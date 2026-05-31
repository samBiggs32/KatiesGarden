namespace KatiesGarden.Api.Functions.Orchestration;

public record OrderOrchestratorInput(
    Guid OrderId,
    string OrderNumber,
    string CustomerFirstName,
    string CustomerLastName,
    string CustomerEmail,
    decimal Total,
    string DeliveryType,
    string? CollectionAddress);

public record OrderStatusChangedEvent(
    string NewStatus,
    string? Note,
    string? ChangedBy);
