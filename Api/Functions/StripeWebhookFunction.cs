using KatiesGarden.Api.Configuration;
using KatiesGarden.Api.Data;
using KatiesGarden.Api.Email;
using KatiesGarden.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using System.Net;

namespace KatiesGarden.Api.Functions;

public class StripeWebhookFunction(
    AppDbContext db,
    IEmailSender emailSender,
    IPushNotificationService pushService,
    IOptions<SmtpOptions> smtpOptions,
    IOptions<StripeOptions> stripeOptions,
    ILogger<StripeWebhookFunction> logger)
{
    [Function("StripeWebhook")]
    public async Task<HttpResponseData> Handle(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "webhooks/stripe")] HttpRequestData req)
    {
        var ct = req.FunctionContext.CancellationToken;
        var webhookSecret = stripeOptions.Value.WebhookSecret;

        string json;
        using (var reader = new StreamReader(req.Body))
            json = await reader.ReadToEndAsync(ct);

        Event stripeEvent;
        try
        {
            var signature = req.Headers.TryGetValues("Stripe-Signature", out var sigs) ? sigs.First() : string.Empty;
            stripeEvent = EventUtility.ConstructEvent(json, signature, webhookSecret);
        }
        catch (StripeException ex)
        {
            logger.LogWarning(ex, "Invalid Stripe webhook signature");
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error constructing Stripe event");
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }

        if (stripeEvent.Type != EventTypes.CheckoutSessionCompleted)
            return req.CreateResponse(HttpStatusCode.OK);

        var session = (Session)stripeEvent.Data.Object;
        if (!session.Metadata.TryGetValue("orderId", out var orderIdStr) || !Guid.TryParse(orderIdStr, out var orderId))
        {
            logger.LogWarning("Stripe session {SessionId} missing orderId metadata", session.Id);
            return req.CreateResponse(HttpStatusCode.OK);
        }

        var order = await db.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null)
        {
            logger.LogError("Order {OrderId} not found for Stripe session {SessionId}", orderId, session.Id);
            return req.CreateResponse(HttpStatusCode.OK);
        }

        // Idempotency guard
        if (order.Status != OrderStatus.Pending)
        {
            logger.LogInformation("Order {OrderId} already processed (status: {Status})", orderId, order.Status);
            return req.CreateResponse(HttpStatusCode.OK);
        }

        order.Status = OrderStatus.Confirmed;
        order.StripePaymentIntentId = session.PaymentIntentId;
        order.UpdatedAt = DateTime.UtcNow;

        // Decrement stock atomically to prevent oversell under concurrent load
        foreach (var line in order.Lines)
        {
            var rowsUpdated = await db.Products
                .Where(p => p.Id == line.ProductId && p.StockQuantity != null && p.StockQuantity >= line.Quantity)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.StockQuantity, p => p.StockQuantity! - line.Quantity), ct);

            if (rowsUpdated == 0)
            {
                logger.LogWarning(
                    "Stock exhausted for product {ProductId} in order {OrderNumber} — payment taken, requires manual review",
                    line.ProductId, order.OrderNumber);
            }
            else
            {
                await db.Products
                    .Where(p => p.Id == line.ProductId && p.StockQuantity == 0)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsAvailable, false), ct);
            }
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Order {OrderNumber} confirmed (Stripe session {SessionId})", order.OrderNumber, session.Id);

        var smtp = smtpOptions.Value;
        var siteUrl = stripeOptions.Value.SiteUrl;

        // Customer confirmation email
        try
        {
            var settings = await db.DeliverySettings.FindAsync([1], ct);
            var customerEmail = OrderEmailBuilder.BuildCustomerConfirmation(
                order, smtp.EffectiveSenderEmail, "Katie's Garden", settings?.CollectionAddress);
            await emailSender.SendAsync(customerEmail, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send customer confirmation email for order {OrderNumber}", order.OrderNumber);
        }

        // Admin alert email
        try
        {
            var adminEmail = OrderEmailBuilder.BuildAdminAlert(order, smtp.RecipientEmail, siteUrl);
            await emailSender.SendAsync(adminEmail, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send admin alert email for order {OrderNumber}", order.OrderNumber);
        }

        // Push notification to Katie's devices
        try
        {
            await pushService.SendAsync(
                $"New Order {order.OrderNumber}",
                $"£{order.Total:F2} from {order.CustomerFirstName} {order.CustomerLastName}",
                ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send push notification for order {OrderNumber}", order.OrderNumber);
        }

        return req.CreateResponse(HttpStatusCode.OK);
    }
}
