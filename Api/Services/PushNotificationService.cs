using KatiesGarden.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WebPush;

namespace KatiesGarden.Api.Services;

public class PushNotificationService(
    AppDbContext db,
    IConfiguration config,
    ILogger<PushNotificationService> logger) : IPushNotificationService
{
    public async Task SendAsync(string title, string body, CancellationToken ct = default)
    {
        var vapidPublicKey = config["VAPID_PUBLIC_KEY"];
        var vapidPrivateKey = config["VAPID_PRIVATE_KEY"];
        var vapidSubject = config["VAPID_SUBJECT"];

        if (string.IsNullOrWhiteSpace(vapidPublicKey) || string.IsNullOrWhiteSpace(vapidPrivateKey))
        {
            logger.LogDebug("VAPID keys not configured — skipping push notifications");
            return;
        }

        var subscriptions = await db.PushSubscriptions.ToListAsync(ct);
        if (subscriptions.Count == 0) return;

        var client = new WebPushClient();
        var vapidDetails = new VapidDetails(vapidSubject ?? "mailto:sales@katiesgarden.uk", vapidPublicKey, vapidPrivateKey);
        var payload = System.Text.Json.JsonSerializer.Serialize(new { title, body });

        var staleEndpoints = new List<string>();

        foreach (var sub in subscriptions)
        {
            try
            {
                var pushSubscription = new PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
                await client.SendNotificationAsync(pushSubscription, payload, vapidDetails);
            }
            catch (WebPushException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Gone or System.Net.HttpStatusCode.NotFound)
            {
                // Subscription expired or unregistered — remove it
                staleEndpoints.Add(sub.Endpoint);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send push notification to {Endpoint}", sub.Endpoint);
            }
        }

        if (staleEndpoints.Count > 0)
        {
            db.PushSubscriptions.RemoveRange(
                db.PushSubscriptions.Where(s => staleEndpoints.Contains(s.Endpoint)));
            await db.SaveChangesAsync(ct);
        }
    }
}
