using KatiesGarden.Models.Entities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace KatiesGarden.Api.Functions.Orchestration;

public class OrderOrchestrationFunction
{
    private static readonly HashSet<string> TerminalStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(OrderStatus.Delivered),
        nameof(OrderStatus.Cancelled),
        nameof(OrderStatus.Refunded)
    };

    private static readonly HashSet<string> EmailNotifyStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(OrderStatus.Confirmed),
        nameof(OrderStatus.ReadyForCollection),
        nameof(OrderStatus.Dispatched),
        nameof(OrderStatus.Delivered),
        nameof(OrderStatus.Cancelled),
        nameof(OrderStatus.Refunded)
    };

    [Function(nameof(OrderLifecycleOrchestrator))]
    public async Task OrderLifecycleOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var logger = context.CreateReplaySafeLogger(nameof(OrderOrchestrationFunction));
        var input = context.GetInput<OrderOrchestratorInput>()!;

        // Record initial Confirmed status in history
        await context.CallActivityAsync(nameof(OrderActivities.RecordStatusHistoryActivity),
            new RecordStatusHistoryInput(input.OrderId, nameof(OrderStatus.Confirmed), "Payment confirmed via Stripe", "System"));

        // Fan-out: run post-payment activities in parallel (email, push, stock)
        var fanOut = new List<Task>
        {
            context.CallActivityAsync(nameof(OrderActivities.SendCustomerConfirmationEmailActivity), input.OrderId),
            context.CallActivityAsync(nameof(OrderActivities.SendAdminAlertEmailActivity), input.OrderId),
            context.CallActivityAsync(nameof(OrderActivities.SendPushNotificationActivity), input),
            context.CallActivityAsync(nameof(OrderActivities.DecrementStockActivity), input.OrderId)
        };

        try { await Task.WhenAll(fanOut); }
        catch (Exception ex)
        {
            logger.LogError(ex, "One or more post-payment activities failed for order {OrderNumber}", input.OrderNumber);
        }

        // Wait for status change events, looping until a terminal state is reached.
        // Uses a 2-year timeout so Durable state doesn't accumulate indefinitely.
        var timeout = context.CurrentUtcDateTime.AddDays(730);

        while (context.CurrentUtcDateTime < timeout)
        {
            using var cts = new CancellationTokenSource();
            var statusChanged = context.WaitForExternalEvent<OrderStatusChangedEvent>(OrchestrationEvents.StatusChanged, cts.Token);
            var deadline = context.CreateTimer(timeout, cts.Token);

            var winner = await Task.WhenAny(statusChanged, deadline);
            cts.Cancel();

            if (winner == deadline) break;

            var evt = await statusChanged;

            // Record the status change in history
            await context.CallActivityAsync(nameof(OrderActivities.RecordStatusHistoryActivity),
                new RecordStatusHistoryInput(input.OrderId, evt.NewStatus, evt.Note, evt.ChangedBy));

            // Send status update email for significant transitions
            if (EmailNotifyStatuses.Contains(evt.NewStatus))
            {
                try
                {
                    await context.CallActivityAsync(
                        nameof(OrderActivities.SendStatusUpdateEmailActivity), input.OrderId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send status update email for order {OrderNumber}", input.OrderNumber);
                }
            }

            if (TerminalStatuses.Contains(evt.NewStatus))
            {
                logger.LogInformation("Order {OrderNumber} reached terminal status {Status} — orchestration complete",
                    input.OrderNumber, evt.NewStatus);
                return;
            }
        }

        logger.LogInformation("Order {OrderNumber} orchestration timed out — completing", input.OrderNumber);
    }
}
