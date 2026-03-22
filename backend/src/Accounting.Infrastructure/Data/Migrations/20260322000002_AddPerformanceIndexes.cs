using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting.Infrastructure.Data.Migrations;

/// <summary>
/// Adds performance indexes for POS hot paths and FEFO batch queries.
/// All raw SQL uses fully-quoted PascalCase identifiers to match EF Core's generated schema.
/// CREATE INDEX CONCURRENTLY is intentionally avoided — EF Core runs migrations inside a
/// transaction block and PostgreSQL forbids CONCURRENTLY within a transaction.
/// </summary>
public partial class AddPerformanceIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS "IX_items_Barcode"
            ON "items" ("Barcode");
            """);

        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS "IX_items_SKU"
            ON "items" ("SKU");
            """);

        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS "IX_items_Name_trgm"
            ON "items" USING GIN ("Name" gin_trgm_ops);
            """);

        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS "IX_sales_invoices_Status_SaleDate"
            ON "sales_invoices" ("Status", "SaleDate");
            """);

        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS "IX_purchase_invoices_Status_InvoiceDate"
            ON "purchase_invoices" ("Status", "InvoiceDate");
            """);

        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS "IX_item_batches_ItemId_ExpiryDate"
            ON "item_batches" ("ItemId", "ExpiryDate");
            """);

        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS "IX_item_batches_ItemId_WarehouseId_ExpiryDate"
            ON "item_batches" ("ItemId", "WarehouseId", "ExpiryDate");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_items_Barcode";""");
        migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_items_SKU";""");
        migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_items_Name_trgm";""");
        migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_sales_invoices_Status_SaleDate";""");
        migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_purchase_invoices_Status_InvoiceDate";""");
        migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_item_batches_ItemId_ExpiryDate";""");
        migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_item_batches_ItemId_WarehouseId_ExpiryDate";""");
    }
}

