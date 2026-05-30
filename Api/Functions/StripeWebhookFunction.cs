using KatiesGarden.Api.Configuration;
using KatiesGarden.Api.Data;
using KatiesGarden.Api.Functions.Orchestration;
using KatiesGarden.Models.Entities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using System.Net;

namespace KatiesGarden.Api.Functions;

public class StripeWebhookFunction(
    AppDbContext db,
    IOptions<StripeOptions> stripeOptions,
    ILogger<StripeWebhookFunction> logger)
{
    [Function("StripeWebhook")]
    public async Task<HttpResponseData> Handle(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "webhooks/stripe")] HttpRequestData req,
        [DurableClient] DurableTaskClient durableClient)
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

        // Start Durable orchestration to handle all post-payment work asynchronously.
        // Instance ID is deterministic per order to prevent duplicate orchestrations on retry.
        var instanceId = $"order-{order.Id}";
        var orchestratorInput = new OrderOrchestratorInput(
            order.Id,
            order.OrderNumber,
            order.CustomerFirstName,
            order.CustomerLastName,
            order.CustomerEmail,
            order.Total,
            order.DeliveryType.ToString(),
            null);

        order.OrchestrationInstanceId = instanceId;
        await db.SaveChangesAsync(ct);

        try
        {
            await durableClient.ScheduleNewOrchestrationInstanceAsync(
                nameof(OrderOrchestrationFunction.OrderLifecycleOrchestrator),
                orchestratorInput,
                new StartOrchestrationOptions { InstanceId = instanceId },
                ct);

            logger.LogInformation("Order {OrderNumber} confirmed — orchestration {InstanceId} started",
                order.OrderNumber, instanceId);
        }
        catch (Exception ex)
        {
            // If the instance already exists (e.g. Stripe retry), log and continue — idempotency is handled above
            logger.LogWarning(ex, "Could not start orchestration for order {OrderNumber} — may already exist", order.OrderNumber);
        }

        return req.CreateResponse(HttpStatusCode.OK);
    }
}
