using KatiesGarden.Api.Auditing;
using KatiesGarden.Api.Data;
using KatiesGarden.Api.Functions.Orchestration;
using KatiesGarden.Models.Entities;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace KatiesGarden.Api.Services;

public class OrderService(AppDbContext db, IAuditService audit, ILogger<OrderService> logger) : IOrderService
{
    public async Task RecordTransitionAsync(
        Order order,
        OrderStatus previousStatus,
        string newStatus,
        string? note,
        string actor,
        DurableTaskClient durableClient,
        CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(order.OrchestrationInstanceId))
        {
            try
            {
                await durableClient.RaiseEventAsync(
                    order.OrchestrationInstanceId,
                    "StatusChanged",
                    new OrderStatusChangedEvent(newStatus, note, actor),
                    ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not raise StatusChanged event on orchestration {InstanceId}", order.OrchestrationInstanceId);
            }
        }

        await audit.LogAsync(
            "StatusChanged", "Order", order.Id.ToString(),
            actor, actor,
            new { from = previousStatus.ToString(), to = newStatus, note },
            ct);
    }
}
