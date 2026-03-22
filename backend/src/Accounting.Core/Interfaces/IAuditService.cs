namespace Accounting.Core.Interfaces;

/// <summary>
/// Writes immutable audit log entries.
/// Called from application/domain services — never bypassed.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Records a significant domain action.
    /// <paramref name="details"/> is serialized to JSON and stored in AuditLog.AdditionalData.
    /// </summary>
    Task LogAsync(
        string entityName,
        string entityId,
        string action,
        Guid actorId,
        object? details = null,
        CancellationToken ct = default);
}

