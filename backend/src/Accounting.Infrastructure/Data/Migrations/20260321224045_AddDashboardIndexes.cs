using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_sales_invoices_Status_SaleDate",
                table: "sales_invoices",
                columns: new[] { "Status", "SaleDate" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_invoices_Status_InvoiceDate",
                table: "purchase_invoices",
                columns: new[] { "Status", "InvoiceDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_sales_invoices_Status_SaleDate",
                table: "sales_invoices");

            migrationBuilder.DropIndex(
                name: "IX_purchase_invoices_Status_InvoiceDate",
                table: "purchase_invoices");
        }
    }
}
