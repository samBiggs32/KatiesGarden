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

public class AdminCollectionFunction(
    AppDbContext db,
    IValidator<CreateCollectionRequest> createValidator,
    IValidator<UpdateCollectionRequest> updateValidator,
    ILogger<AdminCollectionFunction> logger)
{
    [Function("AdminGetCollections")]
    public async Task<HttpResponseData> GetAll(
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

    [Function("AdminGetCollection")]
    public async Task<HttpResponseData> Get(
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

    [Function("AdminCreateCollection")]
    public async Task<HttpResponseData> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "manage/collections")] HttpRequestData req)
    {
        if (req.RequireAdmin() is { } deny) return deny;

        var ct = req.FunctionContext.CancellationToken;
        CreateCollectionRequest? request;
        try { request = await req.ReadFromJsonAsync<CreateCollectionRequest>(); }
        catch { return await Responses.BadRequest(req, "Invalid request body."); }
        if (request is null) return await Responses.BadRequest(req, "Request body is required.");

        var validation = await createValidator.ValidateAsync(request, ct);
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
    public async Task<HttpResponseData> Update(
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

        var validation = await updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return await Responses.BadRequest(req, validation.Errors.First().ErrorMessage);

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
    public async Task<HttpResponseData> Delete(
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
}
