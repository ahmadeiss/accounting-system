using System.Text.Json;
using Accounting.Core.Entities;
using Accounting.Core.Interfaces;
using Accounting.Infrastructure.Data;

namespace Accounting.Infrastructure.Services;

/// <summary>
/// Writes AuditLog records to the database.
/// Each call adds an entry to the EF Core ChangeTracker.
/// SaveChanges must be called by the caller (typically at the end of a transaction).
/// </summary>
public class AuditService : IAuditService
{
    private readonly AccountingDbContext _context;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AuditService(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(
        string entityName,
        string entityId,
        string action,
        Guid actorId,
        object? details = null,
        CancellationToken ct = default)
    {
        var entry = new AuditLog
        {
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            UserId = actorId == Guid.Empty ? null : actorId,
            Timestamp = DateTime.UtcNow,
            AdditionalData = details is null
                ? null
                : JsonSerializer.Serialize(details, _jsonOptions)
        };

        await _context.AuditLogs.AddAsync(entry, ct);
        // NOTE: caller is responsible for SaveChanges / CommitTransaction
    }
}

