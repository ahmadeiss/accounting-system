namespace Accounting.Core.Entities;

/// <summary>
/// Immutable record of significant system actions.
/// Written on: entity creates/updates/deletes, login events, stock adjustments, invoice confirmations.
/// OldValues/NewValues stored as JSON for full before/after comparison.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;

    /// <summary>JSON-serialized previous state. Null for Create actions.</summary>
    public string? OldValues { get; set; }

    /// <summary>JSON-serialized new state. Null for Delete actions.</summary>
    public string? NewValues { get; set; }

    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Any additional contextual data in JSON format.</summary>
    public string? AdditionalData { get; set; }
}

