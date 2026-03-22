using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertStatusAndMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_alerts_branches_BranchId",
                table: "alerts");

            migrationBuilder.DropForeignKey(
                name: "FK_alerts_users_ResolvedById",
                table: "alerts");

            migrationBuilder.DropForeignKey(
                name: "FK_alerts_warehouses_WarehouseId",
                table: "alerts");

            migrationBuilder.DropIndex(
                name: "IX_alerts_BranchId",
                table: "alerts");

            migrationBuilder.DropIndex(
                name: "IX_alerts_IsRead_IsResolved",
                table: "alerts");

            migrationBuilder.DropIndex(
                name: "IX_alerts_ResolvedById",
                table: "alerts");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "alerts");

            migrationBuilder.DropColumn(
                name: "IsRead",
                table: "alerts");

            migrationBuilder.DropColumn(
                name: "IsResolved",
                table: "alerts");

            migrationBuilder.DropColumn(
                name: "ResolvedAt",
                table: "alerts");

            migrationBuilder.DropColumn(
                name: "ResolvedById",
                table: "alerts");

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "alerts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "alerts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_alerts_Batch_Dedup",
                table: "alerts",
                columns: new[] { "AlertType", "ItemBatchId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_alerts_LowStock_Dedup",
                table: "alerts",
                columns: new[] { "AlertType", "ItemId", "WarehouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_alerts_Status",
                table: "alerts",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_alerts_warehouses_WarehouseId",
                table: "alerts",
                column: "WarehouseId",
                principalTable: "warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_alerts_warehouses_WarehouseId",
                table: "alerts");

            migrationBuilder.DropIndex(
                name: "IX_alerts_Batch_Dedup",
                table: "alerts");

            migrationBuilder.DropIndex(
                name: "IX_alerts_LowStock_Dedup",
                table: "alerts");

            migrationBuilder.DropIndex(
                name: "IX_alerts_Status",
                table: "alerts");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "alerts");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "alerts");

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "alerts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRead",
                table: "alerts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsResolved",
                table: "alerts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedAt",
                table: "alerts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ResolvedById",
                table: "alerts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_alerts_BranchId",
                table: "alerts",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_alerts_IsRead_IsResolved",
                table: "alerts",
                columns: new[] { "IsRead", "IsResolved" });

            migrationBuilder.CreateIndex(
                name: "IX_alerts_ResolvedById",
                table: "alerts",
                column: "ResolvedById");

            migrationBuilder.AddForeignKey(
                name: "FK_alerts_branches_BranchId",
                table: "alerts",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_alerts_users_ResolvedById",
                table: "alerts",
                column: "ResolvedById",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_alerts_warehouses_WarehouseId",
                table: "alerts",
                column: "WarehouseId",
                principalTable: "warehouses",
                principalColumn: "Id");
        }
    }
}
