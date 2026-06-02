using FluentValidation;
using KatiesGarden.Api.Auth;
using KatiesGarden.Api.Data;
using KatiesGarden.Models.Entities;
using KatiesGarden.Models.Helpers;
using KatiesGarden.Models.Shop;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace KatiesGarden.Api.Functions;

public class AdminProductFunction(
    AppDbContext db,
    IValidator<ProductRequest> validator,
    ILogger<AdminProductFunction> logger)
{
    [Function("AdminGetProducts")]
    public async Task<HttpResponseData> GetAll(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "manage/products")] HttpRequestData req)
    {
        if (req.RequireAdmin() is { } deny) return deny;

        var ct = req.FunctionContext.CancellationToken;
        var products = await db.Products
            .OrderBy(p => p.DisplayOrder).ThenBy(p => p.Name)
            .Select(p => new ProductSummaryDto(
                p.Id, p.Name, p.Slug, p.Description, p.Price, p.StockQuantity,
                p.IsAvailable, p.CanLocalDeliver, p.ImageUrls.FirstOrDefault(), p.DisplayOrder,
                p.ImageUrls.Length))
            .ToListAsync(ct);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(products);
        return response;
    }

    [Function("AdminGetProduct")]
    public async Task<HttpResponseData> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "manage/products/{id:guid}")] HttpRequestData req,
        Guid id)
    {
        if (req.RequireAdmin() is { } deny) return deny;

        var ct = req.FunctionContext.CancellationToken;
        var product = await db.Products
            .Include(p => p.Collection)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (product is null) return req.CreateResponse(HttpStatusCode.NotFound);

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

    [Function("AdminCreateProduct")]
    public async Task<HttpResponseData> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "manage/products")] HttpRequestData req)
    {
        if (req.RequireAdmin() is { } deny) return deny;

        var ct = req.FunctionContext.CancellationToken;
        ProductRequest? request;
        try { request = await req.ReadFromJsonAsync<ProductRequest>(); }
        catch (JsonException) { return await Responses.BadRequest(req, "Invalid request body."); }
        if (request is null) return await Responses.BadRequest(req, "Request body is required.");

        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return await Responses.BadRequest(req, validation.Errors.First().ErrorMessage);

        var slug = SlugHelper.Generate(request.Name);
        if (await db.Products.AnyAsync(p => p.Slug == slug, ct))
            slug = $"{slug}-{Guid.NewGuid().ToString()[..8]}";

        var product = new Product
        {
            Name = request.Name,
            Slug = slug,
            Description = request.Description,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            IsAvailable = request.IsAvailable,
            CanLocalDeliver = request.CanLocalDeliver,
            ImageUrls = request.ImageUrls,
            CollectionId = request.CollectionId,
            HowToBuyNote = request.HowToBuyNote,
            DisplayOrder = request.DisplayOrder
        };

        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Created product {Name} ({Id})", product.Name, product.Id);

        var response = req.CreateResponse(HttpStatusCode.Created);
        await response.WriteAsJsonAsync(ToSummary(product));
        return response;
    }

    [Function("AdminUpdateProduct")]
    public async Task<HttpResponseData> Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "manage/products/{id:guid}")] HttpRequestData req,
        Guid id)
    {
        if (req.RequireAdmin() is { } deny) return deny;

        var ct = req.FunctionContext.CancellationToken;
        var product = await db.Products.FindAsync([id], ct);
        if (product is null) return req.CreateResponse(HttpStatusCode.NotFound);

        ProductRequest? request;
        try { request = await req.ReadFromJsonAsync<ProductRequest>(); }
        catch (JsonException) { return await Responses.BadRequest(req, "Invalid request body."); }
        if (request is null) return await Responses.BadRequest(req, "Request body is required.");

        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return await Responses.BadRequest(req, validation.Errors.First().ErrorMessage);

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.StockQuantity = request.StockQuantity;
        product.IsAvailable = request.IsAvailable;
        product.CanLocalDeliver = request.CanLocalDeliver;
        product.ImageUrls = request.ImageUrls;
        product.CollectionId = request.CollectionId;
        product.HowToBuyNote = request.HowToBuyNote;
        product.DisplayOrder = request.DisplayOrder;

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Updated product {Id}", id);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(ToSummary(product));
        return response;
    }

    [Function("AdminDeleteProduct")]
    public async Task<HttpResponseData> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "manage/products/{id:guid}")] HttpRequestData req,
        Guid id)
    {
        if (req.RequireAdmin() is { } deny) return deny;

        var ct = req.FunctionContext.CancellationToken;
        var product = await db.Products.FindAsync([id], ct);
        if (product is null) return req.CreateResponse(HttpStatusCode.NotFound);

        if (await db.OrderLines.AnyAsync(l => l.ProductId == id, ct))
            return await Responses.BadRequest(req, "Cannot delete a product that has been ordered. Deactivate it instead.");

        db.Products.Remove(product);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Deleted product {Id}", id);
        return req.CreateResponse(HttpStatusCode.NoContent);
    }

    private static ProductSummaryDto ToSummary(Product p) => new(
        p.Id, p.Name, p.Slug, p.Description, p.Price, p.StockQuantity,
        p.IsAvailable, p.CanLocalDeliver, p.ImageUrls.FirstOrDefault(), p.DisplayOrder,
        p.ImageUrls.Length);
}
