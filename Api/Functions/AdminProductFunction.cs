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

namespace KatiesGarden.Api.Functions;

public class AdminProductFunction(
    AppDbContext db,
    IValidator<CreateProductRequest> createProductValidator,
    IValidator<UpdateProductRequest> updateProductValidator,
    IValidator<CreateCollectionRequest> createCollectionValidator,
    IValidator<UpdateCollectionRequest> updateCollectionValidator,
    ILogger<AdminProductFunction> logger)
{
    // ── Products ────────────────────────────────────────────────────────────

    [Function("AdminCreateProduct")]
    public async Task<HttpResponseData> CreateProduct(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "manage/products")] HttpRequestData req)
    {
        if (req.RequireAdmin() is { } deny) return deny;

        var ct = req.FunctionContext.CancellationToken;
        CreateProductRequest? request;
        try { request = await req.ReadFromJsonAsync<CreateProductRequest>(); }
        catch { return await Responses.BadRequest(req, "Invalid request body."); }
        if (request is null) return await Responses.BadRequest(req, "Request body is required.");

        var validation = await createProductValidator.ValidateAsync(request, ct);
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
    public async Task<HttpResponseData> UpdateProduct(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "manage/products/{id:guid}")] HttpRequestData req,
        Guid id)
    {
        if (req.RequireAdmin() is { } deny) return deny;

        var ct = req.FunctionContext.CancellationToken;
        var product = await db.Products.FindAsync([id], ct);
        if (product is null) return req.CreateResponse(HttpStatusCode.NotFound);

        UpdateProductRequest? request;
        try { request = await req.ReadFromJsonAsync<UpdateProductRequest>(); }
        catch { return await Responses.BadRequest(req, "Invalid request body."); }
        if (request is null) return await Responses.BadRequest(req, "Request body is required.");

        var validation = await updateProductValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return await Responses.BadRequest(req, string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));

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
    public async Task<HttpResponseData> DeleteProduct(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "manage/products/{id:guid}")] HttpRequestData req,
        Guid id)
    {
        if (req.RequireAdmin() is { } deny) return deny;

        var ct = req.FunctionContext.CancellationToken;
        var product = await db.Products.FindAsync([id], ct);
        if (product is null) return req.CreateResponse(HttpStatusCode.NotFound);

        db.Products.Remove(product);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Deleted product {Id}", id);
        return req.CreateResponse(HttpStatusCode.NoContent);
    }

    // ── Collections ─────────────────────────────────────────────────────────

    [Function("AdminGetProduct")]
    public async Task<HttpResponseData> GetProduct(
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

    [Function("AdminGetCollection")]
    public async Task<HttpResponseData> GetCollection(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "manage/collections/{id:guid}")] HttpRequestData req,
        Guid id)
    {
        if (req.RequireAdmin() is { } deny) return deny;

        var ct = req.FunctionContext.CancellationToken;
        var collection = await db.Collections
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (collection is null) return req.CreateResponse(HttpStatusCode.NotFound);

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
            Products = collection.Products
                .OrderBy(p => p.DisplayOrder).ThenBy(p => p.Name)
                .Select(p => new ProductSummaryDto(
                    p.Id, p.Name, p.Slug, p.Description, p.Price, p.StockQuantity,
                    p.IsAvailable, p.CanLocalDeliver, p.ImageUrls.FirstOrDefault(), p.DisplayOrder,
                    p.ImageUrls.Length))
                .ToList()
        };

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(dto);
        return response;
    }

    [Function("AdminGetCollections")]
    public async Task<HttpResponseData> GetAllCollections(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "manage/collections")] HttpRequestData req)
    {
        if (req.RequireAdmin() is { } deny) return deny;

        var ct = req.FunctionContext.CancellationToken;
        var collections = await db.Collections
            .OrderBy(c => c.DisplayOrder).ThenByDescending(c => c.StartDate)
            .Select(c => new CollectionSummaryDto(
                c.Id, c.Title, c.Slug, c.Description, c.CoverImageUrl,
                c.StartDate, c.EndDate, c.Products.Count, c.DisplayOrder, c.IsActive))
            .ToListAsync(ct);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(collections);
        return response;
    }

    [Function("AdminGetProducts")]
    public async Task<HttpResponseData> GetAllProducts(
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

    [Function("AdminCreateCollection")]
    public async Task<HttpResponseData> CreateCollection(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "manage/collections")] HttpRequestData req)
    {
        if (req.RequireAdmin() is { } deny) return deny;

        var ct = req.FunctionContext.CancellationToken;
        CreateCollectionRequest? request;
        try { request = await req.ReadFromJsonAsync<CreateCollectionRequest>(); }
        catch { return await Responses.BadRequest(req, "Invalid request body."); }
        if (request is null) return await Responses.BadRequest(req, "Request body is required.");

        var validation = await createCollectionValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return await Responses.BadRequest(req, validation.Errors.First().ErrorMessage);

        var slug = SlugHelper.Generate(request.Title);
        if (await db.Collections.AnyAsync(c => c.Slug == slug, ct))
            slug = $"{slug}-{Guid.NewGuid().ToString()[..8]}";

        var collection = new Collection
        {
            Title = request.Title,
            Slug = slug,
            Description = request.Description,
            CoverImageUrl = request.CoverImageUrl,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            DisplayOrder = request.DisplayOrder
        };

        db.Collections.Add(collection);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Created collection {Title} ({Id})", collection.Title, collection.Id);

        var response = req.CreateResponse(HttpStatusCode.Created);
        await response.WriteAsJsonAsync(new CollectionSummaryDto(
            collection.Id, collection.Title, collection.Slug, collection.Description,
            collection.CoverImageUrl, collection.StartDate, collection.EndDate, 0,
            collection.DisplayOrder, collection.IsActive));
        return response;
    }

    [Function("AdminUpdateCollection")]
    public async Task<HttpResponseData> UpdateCollection(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "manage/collections/{id:guid}")] HttpRequestData req,
        Guid id)
    {
        if (req.RequireAdmin() is { } deny) return deny;

        var ct = req.FunctionContext.CancellationToken;
        var collection = await db.Collections.FindAsync([id], ct);
        if (collection is null) return req.CreateResponse(HttpStatusCode.NotFound);

        UpdateCollectionRequest? request;
        try { request = await req.ReadFromJsonAsync<UpdateCollectionRequest>(); }
        catch { return await Responses.BadRequest(req, "Invalid request body."); }
        if (request is null) return await Responses.BadRequest(req, "Request body is required.");

        var validation = await updateCollectionValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return await Responses.BadRequest(req, string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));

        collection.Title = request.Title;
        collection.Description = request.Description;
        collection.CoverImageUrl = request.CoverImageUrl;
        collection.IsActive = request.IsActive;
        collection.StartDate = request.StartDate;
        collection.EndDate = request.EndDate;
        collection.DisplayOrder = request.DisplayOrder;

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Updated collection {Id}", id);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new CollectionSummaryDto(
            collection.Id, collection.Title, collection.Slug, collection.Description,
            collection.CoverImageUrl, collection.StartDate, collection.EndDate,
            0, collection.DisplayOrder, collection.IsActive));
        return response;
    }

    [Function("AdminDeleteCollection")]
    public async Task<HttpResponseData> DeleteCollection(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "manage/collections/{id:guid}")] HttpRequestData req,
        Guid id)
    {
        if (req.RequireAdmin() is { } deny) return deny;

        var ct = req.FunctionContext.CancellationToken;
        var collection = await db.Collections.FindAsync([id], ct);
        if (collection is null) return req.CreateResponse(HttpStatusCode.NotFound);

        collection.IsActive = false;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Deactivated collection {Id}", id);
        return req.CreateResponse(HttpStatusCode.NoContent);
    }

    // ── Delivery Settings ───────────────────────────────────────────────────

    [Function("AdminGetDeliverySettings")]
    public async Task<HttpResponseData> GetDeliverySettings(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "manage/delivery-settings")] HttpRequestData req)
    {
        if (req.RequireAdmin() is { } deny) return deny;

        var ct = req.FunctionContext.CancellationToken;
        var settings = await db.DeliverySettings.FindAsync([1], ct) ?? new DeliverySettings();

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new DeliverySettingsDto(
            settings.LocalDeliveryFee, settings.FreeDeliveryThreshold,
            settings.DeliveryAreaDescription, settings.CollectionAddress,
            settings.CollectionInstructions));
        return response;
    }

    [Function("AdminUpdateDeliverySettings")]
    public async Task<HttpResponseData> UpdateDeliverySettings(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "manage/delivery-settings")] HttpRequestData req)
    {
        if (req.RequireAdmin() is { } deny) return deny;

        var ct = req.FunctionContext.CancellationToken;
        var request = await req.ReadFromJsonAsync<DeliverySettingsUpdateRequest>();
        if (request is null) return await Responses.BadRequest(req, "Request body is required.");

        var settings = await db.DeliverySettings.FindAsync([1], ct)
            ?? new DeliverySettings();

        settings.LocalDeliveryFee = request.LocalDeliveryFee;
        settings.FreeDeliveryThreshold = request.FreeDeliveryThreshold;
        settings.DeliveryAreaDescription = request.DeliveryAreaDescription;
        settings.CollectionAddress = request.CollectionAddress;
        settings.CollectionInstructions = request.CollectionInstructions;

        db.DeliverySettings.Update(settings);
        await db.SaveChangesAsync(ct);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new DeliverySettingsDto(
            settings.LocalDeliveryFee, settings.FreeDeliveryThreshold,
            settings.DeliveryAreaDescription, settings.CollectionAddress,
            settings.CollectionInstructions));
        return response;
    }

    private static ProductSummaryDto ToSummary(Product p) => new(
        p.Id, p.Name, p.Slug, p.Description, p.Price, p.StockQuantity,
        p.IsAvailable, p.CanLocalDeliver, p.ImageUrls.FirstOrDefault(), p.DisplayOrder,
        p.ImageUrls.Length);
}

internal record DeliverySettingsUpdateRequest(
    decimal LocalDeliveryFee,
    decimal? FreeDeliveryThreshold,
    string DeliveryAreaDescription,
    string CollectionAddress,
    string CollectionInstructions);
