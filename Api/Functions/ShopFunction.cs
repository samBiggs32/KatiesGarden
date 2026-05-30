using KatiesGarden.Api.Auth;
using KatiesGarden.Api.Data;
using KatiesGarden.Models.Entities;
using KatiesGarden.Models.Shop;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;

namespace KatiesGarden.Api.Functions;

public class ShopFunction(AppDbContext db, ILogger<ShopFunction> logger)
{
    [Function("GetCollections")]
    public async Task<HttpResponseData> GetCollections(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "shop/collections")] HttpRequestData req)
    {
        var collections = await db.Collections
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder).ThenByDescending(c => c.StartDate)
            .Select(c => new CollectionSummaryDto(
                c.Id, c.Title, c.Slug, c.Description, c.CoverImageUrl,
                c.StartDate, c.EndDate,
                c.Products.Count(p => p.IsAvailable),
                c.DisplayOrder, c.IsActive))
            .ToListAsync(req.FunctionContext.CancellationToken);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(collections);
        return response;
    }

    [Function("GetCollection")]
    public async Task<HttpResponseData> GetCollection(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "shop/collections/{slug}")] HttpRequestData req,
        string slug)
    {
        var collection = await db.Collections
            .Include(c => c.Products.Where(p => p.IsAvailable).OrderBy(p => p.DisplayOrder))
            .FirstOrDefaultAsync(c => c.Slug == slug && c.IsActive, req.FunctionContext.CancellationToken);

        if (collection is null)
        {
            logger.LogInformation("Collection {Slug} not found", slug);
            return req.CreateResponse(HttpStatusCode.NotFound);
        }

        var dto = new CollectionDetailDto
        {
            Id = collection.Id,
            Title = collection.Title,
            Slug = collection.Slug,
            Description = collection.Description,
            CoverImageUrl = collection.CoverImageUrl,
            IsActive = collection.IsActive,
            DisplayOrder = collection.DisplayOrder,
            StartDate = collection.StartDate,
            EndDate = collection.EndDate,
            Products = collection.Products.Select(p => new ProductSummaryDto(
                p.Id, p.Name, p.Slug, p.Description, p.Price, p.StockQuantity,
                p.IsAvailable, p.CanLocalDeliver,
                p.ImageUrls.FirstOrDefault(), p.DisplayOrder)).ToList()
        };

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(dto);
        return response;
    }

    [Function("GetProduct")]
    public async Task<HttpResponseData> GetProduct(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "shop/products/{slug}")] HttpRequestData req,
        string slug)
    {
        var product = await db.Products
            .Include(p => p.Collection)
            .FirstOrDefaultAsync(p => p.Slug == slug, req.FunctionContext.CancellationToken);

        if (product is null)
        {
            logger.LogInformation("Product {Slug} not found", slug);
            return req.CreateResponse(HttpStatusCode.NotFound);
        }

        var dto = new ProductDetailDto
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            IsAvailable = product.IsAvailable,
            CanLocalDeliver = product.CanLocalDeliver,
            ImageUrls = product.ImageUrls,
            HowToBuyNote = product.HowToBuyNote,
            CollectionId = product.CollectionId,
            CollectionTitle = product.Collection?.Title,
            CollectionSlug = product.Collection?.Slug,
            DisplayOrder = product.DisplayOrder
        };

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(dto);
        return response;
    }

    [Function("GetDeliverySettings")]
    public async Task<HttpResponseData> GetDeliverySettings(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "shop/delivery-settings")] HttpRequestData req)
    {
        var ct = req.FunctionContext.CancellationToken;
        var settings = await db.DeliverySettings.FindAsync([1], ct) ?? new DeliverySettings();

        var dto = new DeliverySettingsDto(
            settings.LocalDeliveryFee,
            settings.FreeDeliveryThreshold,
            settings.DeliveryAreaDescription,
            settings.CollectionAddress,
            settings.CollectionInstructions);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(dto);
        return response;
    }

    // Returns a minimal order summary keyed by Stripe session ID so the order success
    // page can show the customer their order number without requiring authentication.
    // The session ID acts as an opaque token — only Stripe and the redirected customer
    // possess it, so it is safe to use as the sole auth mechanism here.
    [Function("GetOrderBySession")]
    public async Task<HttpResponseData> GetOrderBySession(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "shop/order-lookup")] HttpRequestData req)
    {
        var ct = req.FunctionContext.CancellationToken;
        var sessionId = req.GetQueryParam("sessionId");

        if (string.IsNullOrWhiteSpace(sessionId))
            return req.CreateResponse(HttpStatusCode.BadRequest);

        var order = await db.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.StripeSessionId == sessionId, ct);

        if (order is null)
            return req.CreateResponse(HttpStatusCode.NotFound);

        var dto = new OrderLookupDto(
            order.OrderNumber,
            order.Total,
            order.DeliveryFee,
            order.DeliveryType.ToString(),
            order.CreatedAt,
            order.Lines.Select(l => new OrderLookupLineDto(l.ProductName, l.Quantity, l.LineTotal)).ToList());

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(dto);
        return response;
    }

    // Full-text search across available products. Sort options: featured (default),
    // price_asc, price_desc, name. Returns at most 50 results.
    [Function("SearchProducts")]
    public async Task<HttpResponseData> SearchProducts(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "shop/search")] HttpRequestData req)
    {
        var ct = req.FunctionContext.CancellationToken;
        var q = (req.GetQueryParam("q") ?? string.Empty).Trim();
        var sort = req.GetQueryParam("sort") ?? "featured";

        var productsQuery = db.Products
            .Include(p => p.Collection)
            .Where(p => p.IsAvailable);

        if (!string.IsNullOrWhiteSpace(q))
        {
            // Escape LIKE metacharacters so user input is treated as literal text,
            // then use Postgres-native ILIKE for efficient case-insensitive matching.
            var pattern = $"%{q.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
            productsQuery = productsQuery.Where(p =>
                EF.Functions.ILike(p.Name, pattern, "\\") ||
                (p.Description != null && EF.Functions.ILike(p.Description, pattern, "\\")));
        }

        productsQuery = sort switch
        {
            "price_asc"  => productsQuery.OrderBy(p => p.Price).ThenBy(p => p.DisplayOrder),
            "price_desc" => productsQuery.OrderByDescending(p => p.Price).ThenBy(p => p.DisplayOrder),
            "name"       => productsQuery.OrderBy(p => p.Name),
            _            => productsQuery.OrderBy(p => p.DisplayOrder).ThenBy(p => p.Name),
        };

        var results = await productsQuery
            .Take(50)
            .Select(p => new ProductSearchResultDto(
                p.Id, p.Name, p.Slug, p.Description, p.Price, p.StockQuantity,
                p.IsAvailable, p.CanLocalDeliver, p.ImageUrls.FirstOrDefault(), p.DisplayOrder,
                p.Collection != null ? p.Collection.Title : null,
                p.Collection != null ? p.Collection.Slug : null))
            .ToListAsync(ct);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(results);
        return response;
    }
}
