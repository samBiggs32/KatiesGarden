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
        OrderStatus newStatus,
        string? note,
        string actor,
        DurableTaskClient durableClient,
        CancellationToken ct = default);

    /// <summary>
    /// Erases personal data from an order to satisfy a UK GDPR Art. 17 request, while
    /// retaining the financial record (totals, Stripe IDs, order lines) required for
    /// HMRC's 7-year retention. Replaces name/email/phone/address/notes with placeholders,
    /// unlinks the customer identity, and writes an audit entry. Saves changes.
    /// </summary>
    Task AnonymiseAsync(Order order, string actor, CancellationToken ct = default);
}
