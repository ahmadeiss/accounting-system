using Accounting.Application.Import.DTOs;
using Accounting.Application.Import.Services;
using Accounting.Core.Entities;
using Accounting.Core.Enums;
using Accounting.Core.Interfaces;
using Accounting.Infrastructure.Data;
using Accounting.Infrastructure.Excel;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Services;

/// <summary>
/// Orchestrates Excel import jobs.
///
/// Commit strategy (row-level):
///   1. Parse all rows from Excel.
///   2. Validate every row (no DB writes yet).
///   3. For each valid row: persist + SaveChanges atomically.
///      A failure in one row does NOT roll back already-committed rows.
///   4. Persist the ImportJob summary record.
///   5. Return a full ImportResult with per-row status.
///
/// Dry-run strategy:
///   Steps 1-2 only. No DB writes at all. Returns the same ImportResult shape
///   so the caller can display validation errors before committing.
/// </summary>
public sealed class ImportService : IImportService
{
    private readonly AccountingDbContext _context;
    private readonly IAuditService _audit;
    private readonly ItemImportProcessor _itemProcessor;
    private readonly OpeningStockImportProcessor _stockProcessor;

    public ImportService(
        AccountingDbContext context,
        IAuditService audit,
        ItemImportProcessor itemProcessor,
        OpeningStockImportProcessor stockProcessor)
    {
        _context = context;
        _audit = audit;
        _itemProcessor = itemProcessor;
        _stockProcessor = stockProcessor;
    }

    // ─── Item Import ──────────────────────────────────────────────────────────

    public async Task<ImportResult> ImportItemsAsync(
        ImportItemsRequest request,
        CancellationToken ct = default)
    {
        var rows = ExcelParser.Parse(request.FileBytes);
        var rowResults = new List<ImportRowResult>();
        var seenSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Phase 1: validate all rows
        var validationErrors = new Dictionary<int, string?>();
        foreach (var row in rows)
        {
            var error = await _itemProcessor.ValidateAsync(row, seenSkus, ct);
            validationErrors[row.RowNumber] = error;
        }

        if (request.DryRun)
            return BuildDryRunResult(rows, validationErrors);

        // Phase 2: commit valid rows one by one
        Guid jobId = Guid.NewGuid();
        int success = 0, failed = 0, skipped = 0;

        // Reset seenSkus — validation pass already caught duplicates; now we
        // track which SKUs we actually committed so we don't double-insert.
        var committedSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var error = validationErrors[row.RowNumber];
            if (error is not null)
            {
                rowResults.Add(new ImportRowResult(row.RowNumber, ImportRowStatus.Failed, error, row.RawJson));
                failed++;
                continue;
            }

            try
            {
                await _itemProcessor.PersistAsync(row, ct);
                await _context.SaveChangesAsync(ct);
                rowResults.Add(new ImportRowResult(row.RowNumber, ImportRowStatus.Success, null, row.RawJson));
                success++;
            }
            catch (Exception ex)
            {
                rowResults.Add(new ImportRowResult(row.RowNumber, ImportRowStatus.Failed,
                    $"Unexpected error: {ex.Message}", row.RawJson));
                failed++;
                // Detach tracked entities to keep context clean for next row
                _context.ChangeTracker.Clear();
            }
        }

        var status = DetermineStatus(success, failed, rows.Count);
        await PersistJobAsync(jobId, ImportJobType.ItemImport, request.OriginalFileName,
            request.CreatedById, rows.Count, success, failed, skipped, status, rowResults, ct);

        await _audit.LogAsync("ImportJob", jobId.ToString(), "ItemImportCompleted",
            request.CreatedById,
            new { TotalRows = rows.Count, SuccessRows = success, FailedRows = failed, Status = status.ToString() },
            ct);

        return new ImportResult(jobId, false, status, rows.Count, success, failed, skipped,
            failed > 0 ? $"{failed} row(s) failed validation or processing." : null,
            rowResults);
    }

    // ─── Opening Stock Import ─────────────────────────────────────────────────

    public async Task<ImportResult> ImportOpeningStockAsync(
        ImportOpeningStockRequest request,
        CancellationToken ct = default)
    {
        var rows = ExcelParser.Parse(request.FileBytes);
        var rowResults = new List<ImportRowResult>();
        var seenBatchKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Phase 1: validate all rows
        var validationResults = new Dictionary<int, (string? Error, Core.Entities.Item? Item)>();
        foreach (var row in rows)
        {
            var result = await _stockProcessor.ValidateAsync(row, request.WarehouseId, seenBatchKeys, ct);
            validationResults[row.RowNumber] = result;
        }

        if (request.DryRun)
        {
            var dryErrors = validationResults.ToDictionary(kv => kv.Key, kv => kv.Value.Error);
            return BuildDryRunResult(rows, dryErrors);
        }

        // Phase 2: commit valid rows one by one
        Guid jobId = Guid.NewGuid();
        int success = 0, failed = 0, skipped = 0;

        foreach (var row in rows)
        {
            var (error, item) = validationResults[row.RowNumber];
            if (error is not null || item is null)
            {
                rowResults.Add(new ImportRowResult(row.RowNumber, ImportRowStatus.Failed,
                    error ?? "Unknown validation error.", row.RawJson));
                failed++;
                continue;
            }

            try
            {
                await _stockProcessor.PersistAsync(row, item, request.WarehouseId,
                    request.CreatedById, jobId, ct);
                await _context.SaveChangesAsync(ct);
                rowResults.Add(new ImportRowResult(row.RowNumber, ImportRowStatus.Success, null, row.RawJson));
                success++;
            }
            catch (Exception ex)
            {
                rowResults.Add(new ImportRowResult(row.RowNumber, ImportRowStatus.Failed,
                    $"Unexpected error: {ex.Message}", row.RawJson));
                failed++;
                _context.ChangeTracker.Clear();
            }
        }

        var status = DetermineStatus(success, failed, rows.Count);
        await PersistJobAsync(jobId, ImportJobType.OpeningStockImport, request.OriginalFileName,
            request.CreatedById, rows.Count, success, failed, skipped, status, rowResults, ct);

        await _audit.LogAsync("ImportJob", jobId.ToString(), "OpeningStockImportCompleted",
            request.CreatedById,
            new { TotalRows = rows.Count, SuccessRows = success, FailedRows = failed, Status = status.ToString() },
            ct);

        return new ImportResult(jobId, false, status, rows.Count, success, failed, skipped,
            failed > 0 ? $"{failed} row(s) failed validation or processing." : null,
            rowResults);
    }

    // ─── Get Job Result ───────────────────────────────────────────────────────

    public async Task<ImportResult?> GetJobResultAsync(Guid jobId, CancellationToken ct = default)
    {
        var job = await _context.ImportJobs
            .Include(j => j.Rows)
            .FirstOrDefaultAsync(j => j.Id == jobId, ct);

        if (job is null) return null;

        var rowResults = job.Rows
            .OrderBy(r => r.RowNumber)
            .Select(r => new ImportRowResult(r.RowNumber, r.Status, r.ErrorMessage, r.RawData))
            .ToList();

        return new ImportResult(
            job.Id, job.IsDryRun, job.Status,
            job.TotalRows, job.SuccessRows, job.FailedRows, job.SkippedRows,
            job.ErrorSummary, rowResults);
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private static ImportResult BuildDryRunResult(
        IReadOnlyList<ExcelRow> rows,
        Dictionary<int, string?> errors)
    {
        var rowResults = rows.Select(r =>
        {
            var err = errors.TryGetValue(r.RowNumber, out var e) ? e : null;
            var status = err is null ? ImportRowStatus.Success : ImportRowStatus.Failed;
            return new ImportRowResult(r.RowNumber, status, err, r.RawJson);
        }).ToList();

        int success = rowResults.Count(r => r.Status == ImportRowStatus.Success);
        int failed = rowResults.Count(r => r.Status == ImportRowStatus.Failed);
        var jobStatus = failed == 0 ? ImportJobStatus.Completed : ImportJobStatus.PartialSuccess;

        return new ImportResult(null, true, jobStatus, rows.Count, success, failed, 0,
            failed > 0 ? $"{failed} row(s) have validation errors." : null,
            rowResults);
    }

    private static ImportJobStatus DetermineStatus(int success, int failed, int total)
    {
        if (failed == 0) return ImportJobStatus.Completed;
        if (success == 0) return ImportJobStatus.Failed;
        return ImportJobStatus.PartialSuccess;
    }

    private async Task PersistJobAsync(
        Guid jobId,
        ImportJobType jobType,
        string originalFileName,
        Guid createdById,
        int total, int success, int failed, int skipped,
        ImportJobStatus status,
        List<ImportRowResult> rowResults,
        CancellationToken ct)
    {
        var job = new ImportJob
        {
            Id = jobId,
            JobType = jobType,
            Status = status,
            OriginalFileName = originalFileName,
            StoredFilePath = string.Empty, // file storage is deferred
            TotalRows = total,
            SuccessRows = success,
            FailedRows = failed,
            SkippedRows = skipped,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            ErrorSummary = failed > 0 ? $"{failed} row(s) failed." : null,
            IsDryRun = false,
            CreatedById = createdById
        };

        foreach (var r in rowResults)
        {
            job.Rows.Add(new ImportJobRow
            {
                ImportJobId = jobId,
                RowNumber = r.RowNumber,
                Status = r.Status,
                RawData = r.RawData,
                ErrorMessage = r.ErrorMessage
            });
        }

        _context.ImportJobs.Add(job);
        await _context.SaveChangesAsync(ct);
    }
}

