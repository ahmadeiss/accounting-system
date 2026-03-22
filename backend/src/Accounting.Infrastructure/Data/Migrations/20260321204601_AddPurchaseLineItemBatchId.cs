using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseLineItemBatchId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "TaxPercent",
                table: "purchase_invoice_lines",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountPercent",
                table: "purchase_invoice_lines",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<Guid>(
                name: "ItemBatchId",
                table: "purchase_invoice_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_invoice_lines_ItemBatchId",
                table: "purchase_invoice_lines",
                column: "ItemBatchId");

            migrationBuilder.AddForeignKey(
                name: "FK_purchase_invoice_lines_item_batches_ItemBatchId",
                table: "purchase_invoice_lines",
                column: "ItemBatchId",
                principalTable: "item_batches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_purchase_invoice_lines_item_batches_ItemBatchId",
                table: "purchase_invoice_lines");

            migrationBuilder.DropIndex(
                name: "IX_purchase_invoice_lines_ItemBatchId",
                table: "purchase_invoice_lines");

            migrationBuilder.DropColumn(
                name: "ItemBatchId",
                table: "purchase_invoice_lines");

            migrationBuilder.AlterColumn<decimal>(
                name: "TaxPercent",
                table: "purchase_invoice_lines",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountPercent",
                table: "purchase_invoice_lines",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);
        }
    }
}
