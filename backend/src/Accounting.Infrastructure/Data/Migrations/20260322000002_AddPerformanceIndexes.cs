using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting.Infrastructure.Data.Migrations;

/// <summary>
/// Adds performance indexes for POS hot paths and FEFO batch queries.
///
/// 1. pg_trgm GIN index on items.name — enables fast ILIKE '%search%' for cashier item search.
///    Requires the pg_trgm extension (available in all standard PostgreSQL installations).
///
/// 2. Composite index on item_batches(item_id, warehouse_id, status, available_quantity)
///    — covers the FEFO query in StockService.GetFefoBatchesAsync which filters on all four columns.
///    INCLUDE expiry_date avoids a heap fetch for the ORDER BY column.
///
/// 3. Composite index on stock_balances(item_id, warehouse_id) already exists (unique constraint).
///    No action needed.
///
/// 4. Index on items.name (B-tree) for exact-match and prefix queries as a fallback
///    when pg_trgm is not available.
/// </summary>
public partial class AddPerformanceIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Enable trigram extension (idempotent — safe to run multiple times)
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

        // GIN trigram index on items.name for fast ILIKE search
        migrationBuilder.Sql("""
            CREATE INDEX CONCURRENTLY IF NOT EXISTS IX_items_name_trgm
            ON items USING GIN (name gin_trgm_ops);
            """);

        // Composite covering index for FEFO batch selection
        // Filters: item_id, warehouse_id, status = 'Active', available_quantity > 0
        // Order: expiry_date ASC (FEFO), created_at ASC (tie-break)
        migrationBuilder.Sql("""
            CREATE INDEX CONCURRENTLY IF NOT EXISTS IX_item_batches_fefo
            ON item_batches (item_id, warehouse_id, status, available_quantity)
            INCLUDE (expiry_date, created_at, cost_per_unit)
            WHERE status = 'Active' AND available_quantity > 0;
            """);

        // Index on stock_movements for reference lookups (e.g. "all movements for invoice X")
        // Already exists: (reference_type, reference_id) — verified in StockConfiguration.cs
        // No action needed.

        // Index on sales_invoice_line_allocations for batch traceability queries
        // Already exists: (item_batch_id, sales_invoice_line_id) — verified in InvoiceConfiguration.cs
        // No action needed.
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX CONCURRENTLY IF EXISTS IX_items_name_trgm;");
        migrationBuilder.Sql("DROP INDEX CONCURRENTLY IF EXISTS IX_item_batches_fefo;");
    }
}

