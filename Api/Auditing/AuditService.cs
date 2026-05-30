using KatiesGarden.Api.Data;
using KatiesGarden.Models.Entities;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KatiesGarden.Api.Auditing;

public class AuditService(AppDbContext db, ILogger<AuditService> logger) : IAuditService
{
    public async Task LogAsync(string action, string entityType, string entityId,
        string? actorEmail = null, string? actorName = null,
        object? details = null, CancellationToken ct = default)
    {
        try
        {
            db.AuditLogs.Add(new AuditLog
            {
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                ActorEmail = actorEmail,
                ActorName = actorName,
                Details = details is null ? null : JsonSerializer.Serialize(details)
            });
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write audit log: {Action} on {EntityType}/{EntityId}", action, entityType, entityId);
        }
    }
}
