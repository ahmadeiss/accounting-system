using Accounting.Core.Entities;
using Accounting.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Data.Configurations;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movements");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Quantity).HasPrecision(18, 4);
        builder.Property(m => m.UnitCost).HasPrecision(18, 4);
        builder.Property(m => m.MovementType).HasConversion<string>();
        builder.Property(m => m.ReferenceType).HasMaxLength(100);

        // Indexes for reporting queries
        builder.HasIndex(m => new { m.ItemId, m.WarehouseId, m.MovementDate });
        builder.HasIndex(m => new { m.ReferenceType, m.ReferenceId });

        builder.HasOne(m => m.Item)
            .WithMany(i => i.StockMovements)
            .HasForeignKey(m => m.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Warehouse)
            .WithMany(w => w.StockMovements)
            .HasForeignKey(m => m.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.ItemBatch)
            .WithMany(b => b.StockMovements)
            .HasForeignKey(m => m.ItemBatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.CreatedBy)
            .WithMany()
            .HasForeignKey(m => m.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class StockBalanceConfiguration : IEntityTypeConfiguration<StockBalance>
{
    public void Configure(EntityTypeBuilder<StockBalance> builder)
    {
        builder.ToTable("stock_balances");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.QuantityOnHand).HasPrecision(18, 4);

        // Unique constraint: one balance row per item per warehouse
        builder.HasIndex(b => new { b.ItemId, b.WarehouseId }).IsUnique();

        builder.HasOne(b => b.Item)
            .WithMany(i => i.StockBalances)
            .HasForeignKey(b => b.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Warehouse)
            .WithMany(w => w.StockBalances)
            .HasForeignKey(b => b.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

