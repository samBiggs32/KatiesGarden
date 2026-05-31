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
                    OrchestrationEvents.StatusChanged,
                    new OrderStatusChangedEvent(newStatus, note, actor),
                    ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not raise StatusChanged event on orchestration {InstanceId}", order.OrchestrationInstanceId);
            }
        }

        // actorEmail is null because UserDetails is the OAuth display name/username, not an
        // email address. Storing the same value in both columns was misleading audit data.
        await audit.LogAsync(
            "StatusChanged", "Order", order.Id.ToString(),
            actorEmail: null, actorName: actor,
            new { from = previousStatus.ToString(), to = newStatus, note },
            ct);
    }

    private const string Erased = "[erased]";

    public async Task AnonymiseAsync(Order order, string actor, CancellationToken ct = default)
    {
        var emailHash = LogRedaction.Hash(order.CustomerEmail);

        order.CustomerFirstName = Erased;
        order.CustomerLastName = Erased;
        order.CustomerEmail = $"erased-{emailHash}@anonymised.invalid";
        order.CustomerPhone = Erased;
        if (order.DeliveryAddress is not null) order.DeliveryAddress = Erased;
        if (order.DeliveryPostcode is not null) order.DeliveryPostcode = Erased;
        if (order.CustomerNotes is not null) order.CustomerNotes = Erased;
        order.CustomerId = null;
        order.CustomerIdentityProvider = null;
        order.UpdatedAt = DateTime.UtcNow;

        // Write the PII erasure and its audit record in a single transaction so there can be
        // no outcome where customer data is gone but the GDPR evidence row is missing.
        db.AuditLogs.Add(new Models.Entities.AuditLog
        {
            Action = "Anonymised",
            EntityType = "Order",
            EntityId = order.Id.ToString(),
            ActorEmail = null,
            ActorName = actor,
            Details = System.Text.Json.JsonSerializer.Serialize(
                new { emailHash, retained = "financial record + order lines" })
        });
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Order {OrderNumber} anonymised (GDPR erasure) by {Actor}", order.OrderNumber, actor);
    }
}
