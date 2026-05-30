using KatiesGarden.Api.Auth;
using KatiesGarden.Api.Data;
using KatiesGarden.Models.Entities;
using KatiesGarden.Models.Shop;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace KatiesGarden.Api.Functions;

public class AdminDeliverySettingsFunction(AppDbContext db)
{
    [Function("AdminGetDeliverySettings")]
    public async Task<HttpResponseData> Get(
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
    public async Task<HttpResponseData> Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "manage/delivery-settings")] HttpRequestData req)
    {
        if (req.RequireAdmin() is { } deny) return deny;

        var ct = req.FunctionContext.CancellationToken;
        var request = await req.ReadFromJsonAsync<DeliverySettingsUpdateRequest>();
        if (request is null) return await Responses.BadRequest(req, "Request body is required.");

        var settings = await db.DeliverySettings.FindAsync([1], ct) ?? new DeliverySettings();

        settings.LocalDeliveryFee = request.LocalDeliveryFee;
        settings.FreeDeliveryThreshold = request.FreeDeliveryThreshold;
        settings.DeliveryAreaDescription = request.DeliveryAreaDescription;
        settings.CollectionAddress = request.CollectionAddress;
        settings.CollectionInstructions = request.CollectionInstructions;

        // FindAsync attaches the entity; SaveChanges persists the tracked changes.
        // No explicit db.Update() call needed.
        await db.SaveChangesAsync(ct);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new DeliverySettingsDto(
            settings.LocalDeliveryFee, settings.FreeDeliveryThreshold,
            settings.DeliveryAreaDescription, settings.CollectionAddress,
            settings.CollectionInstructions));
        return response;
    }
}
