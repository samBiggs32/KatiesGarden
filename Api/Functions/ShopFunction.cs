using KatiesGarden.Api.Data;
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
        var settings = await db.DeliverySettings.FindAsync(1)
            ?? new DeliverySettings();

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
}
