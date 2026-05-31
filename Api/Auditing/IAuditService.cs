namespace KatiesGarden.Api.Auditing;

public interface IAuditService
{
    Task LogAsync(string action, string entityType, string entityId,
        string? actorEmail = null, string? actorName = null,
        object? details = null, CancellationToken ct = default);
}
