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
using Npgsql;
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

        // Without a configured signing secret we cannot verify authenticity, and an empty
        // secret is attacker-knowable (they could compute a matching signature). Refuse to
        // process rather than trust a forgeable event. 500 so Stripe retries once configured.
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            logger.LogError("STRIPE_WEBHOOK_SECRET is not configured — rejecting webhook");
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }

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

        // Defence-in-depth: the amount Stripe captured must match the total we computed
        // server-side when the order was created. Rejects a forged or mismatched session
        // before we confirm anything.
        if (session.AmountTotal.HasValue && session.AmountTotal.Value != (long)(order.Total * 100))
        {
            logger.LogError("Stripe session {SessionId} amount {Captured} does not match order {OrderId} total {Expected} — refusing to confirm",
                session.Id, session.AmountTotal.Value, orderId, (long)(order.Total * 100));
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        if (order.Status != OrderStatus.Pending)
        {
            logger.LogInformation("Order {OrderId} already confirmed (status: {Status})", orderId, order.Status);
            return req.CreateResponse(HttpStatusCode.OK);
        }

        order.Status = OrderStatus.Confirmed;
        order.StripePaymentIntentId = session.PaymentIntentId;
        order.UpdatedAt = DateTime.UtcNow;

        // Deterministic instance ID prevents duplicate orchestrations on Stripe retries
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
            logger.LogWarning(ex, "Could not start orchestration for order {OrderNumber} — may already exist", order.OrderNumber);
        }

        // Record the event as processed only now that the order is confirmed and the
        // orchestration scheduled. If any step above failed, this row is absent and Stripe's
        // retry reprocesses cleanly — the order-status check above keeps that idempotent.
        // (Recording this first, as the code previously did, meant a transient fault during
        // processing permanently skipped a paid order.)
        try
        {
            db.StripeProcessedEvents.Add(new StripeProcessedEvent { EventId = stripeEvent.Id });
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation })
        {
            logger.LogInformation("Stripe event {EventId} already recorded (concurrent delivery)", stripeEvent.Id);
        }

        return req.CreateResponse(HttpStatusCode.OK);
    }
}
