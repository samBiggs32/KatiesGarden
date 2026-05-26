using KatiesGarden.Api.Auth;
using KatiesGarden.Api.Data;
using KatiesGarden.Models.Shop;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;

namespace KatiesGarden.Api.Functions;

public class PushFunction(AppDbContext db, IConfiguration config, ILogger<PushFunction> logger)
{
    [Function("PushGetVapidPublicKey")]
    public HttpResponseData GetVapidPublicKey(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "push/vapid-public-key")] HttpRequestData req)
    {
        if (!SwaAuth.IsAdmin(req)) return req.CreateResponse(HttpStatusCode.Unauthorized);

        var key = config["VAPID_PUBLIC_KEY"] ?? string.Empty;
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/plain");
        response.WriteString(key);
        return response;
    }

    [Function("PushSubscribe")]
    public async Task<HttpResponseData> Subscribe(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "push/subscribe")] HttpRequestData req)
    {
        if (!SwaAuth.IsAdmin(req)) return req.CreateResponse(HttpStatusCode.Unauthorized);

        var ct = req.FunctionContext.CancellationToken;
        var request = await req.ReadFromJsonAsync<PushSubscribeRequest>();
        if (request is null || string.IsNullOrWhiteSpace(request.Endpoint))
            return await Responses.BadRequest(req, "Endpoint is required.");

        // Upsert by endpoint
        var existing = await db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == request.Endpoint, ct);

        if (existing is not null)
        {
            existing.P256dh = request.P256dh;
            existing.Auth = request.Auth;
        }
        else
        {
            db.PushSubscriptions.Add(new StorePushSubscription
            {
                Endpoint = request.Endpoint,
                P256dh = request.P256dh,
                Auth = request.Auth
            });
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Push subscription saved for {Endpoint}", request.Endpoint[..Math.Min(30, request.Endpoint.Length)]);
        return req.CreateResponse(HttpStatusCode.OK);
    }

    [Function("PushUnsubscribe")]
    public async Task<HttpResponseData> Unsubscribe(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "push/subscribe")] HttpRequestData req)
    {
        if (!SwaAuth.IsAdmin(req)) return req.CreateResponse(HttpStatusCode.Unauthorized);

        var ct = req.FunctionContext.CancellationToken;
        var request = await req.ReadFromJsonAsync<PushSubscribeRequest>();
        if (request is null) return await Responses.BadRequest(req, "Request body is required.");

        var sub = await db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == request.Endpoint, ct);

        if (sub is not null)
        {
            db.PushSubscriptions.Remove(sub);
            await db.SaveChangesAsync(ct);
        }

        return req.CreateResponse(HttpStatusCode.NoContent);
    }
}
