namespace KatiesGarden.Api.Functions.Orchestration;

// The event name that couples OrderService.RecordTransitionAsync (RaiseEventAsync caller)
// to WaitForExternalEvent in the orchestrator. A typo in either silently causes the
// orchestrator to wait forever, so the string lives here as a shared constant.
internal static class OrchestrationEvents
{
    internal const string StatusChanged = "StatusChanged";
}

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
