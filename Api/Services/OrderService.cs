using KatiesGarden.Api.Auditing;
using KatiesGarden.Api.Data;
using KatiesGarden.Api.Functions.Orchestration;
using KatiesGarden.Api.Helpers;
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

    private const string Erased = "[erased]";

    public async Task AnonymiseAsync(Order order, string actor, CancellationToken ct = default)
    {
        // Keep a one-way hash of the original email so the erasure can be evidenced in the
        // audit log without storing the address itself.
        var emailHash = LogRedaction.Hash(order.CustomerEmail);

        order.CustomerFirstName = Erased;
        order.CustomerLastName = Erased;
        order.CustomerEmail = $"erased-{emailHash}@anonymised.invalid";
        order.CustomerPhone = Erased;
        if (order.DeliveryAddress is not null) order.DeliveryAddress = Erased;
        if (order.DeliveryPostcode is not null) order.DeliveryPostcode = Erased;
        if (order.CustomerNotes is not null) order.CustomerNotes = Erased;
        // Unlink the OAuth identity so the order no longer surfaces in "My Orders".
        order.CustomerId = null;
        order.CustomerIdentityProvider = null;
        order.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        await audit.LogAsync(
            "Anonymised", "Order", order.Id.ToString(),
            actor, actor,
            new { emailHash, retained = "financial record + order lines" },
            ct);

        logger.LogInformation("Order {OrderNumber} anonymised (GDPR erasure) by {Actor}", order.OrderNumber, actor);
    }
}
