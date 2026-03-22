using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesInvoiceLineAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "TaxPercent",
                table: "sales_invoice_lines",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountPercent",
                table: "sales_invoice_lines",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.CreateTable(
                name: "sales_invoice_line_allocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesInvoiceLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ExpiryDateSnapshot = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_invoice_line_allocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sales_invoice_line_allocations_item_batches_ItemBatchId",
                        column: x => x.ItemBatchId,
                        principalTable: "item_batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_invoice_line_allocations_sales_invoice_lines_SalesInv~",
                        column: x => x.SalesInvoiceLineId,
                        principalTable: "sales_invoice_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sales_invoice_line_allocations_ItemBatchId_SalesInvoiceLine~",
                table: "sales_invoice_line_allocations",
                columns: new[] { "ItemBatchId", "SalesInvoiceLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_invoice_line_allocations_SalesInvoiceLineId",
                table: "sales_invoice_line_allocations",
                column: "SalesInvoiceLineId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sales_invoice_line_allocations");

            migrationBuilder.AlterColumn<decimal>(
                name: "TaxPercent",
                table: "sales_invoice_lines",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountPercent",
                table: "sales_invoice_lines",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);
        }
    }
}
