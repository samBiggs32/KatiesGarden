using KatiesGarden.Models.Entities;
using Microsoft.DurableTask.Client;

namespace KatiesGarden.Api.Services;

public interface IOrderService
{
    /// <summary>
    /// Raises the StatusChanged external event on the Durable orchestration and
    /// writes an audit log entry. The caller is responsible for updating
    /// <see cref="Order.Status"/> and calling <c>db.SaveChangesAsync()</c> first.
    /// </summary>
    Task RecordTransitionAsync(
        Order order,
        OrderStatus previousStatus,
        string newStatus,
        string? note,
        string actor,
        DurableTaskClient durableClient,
        CancellationToken ct = default);
}
