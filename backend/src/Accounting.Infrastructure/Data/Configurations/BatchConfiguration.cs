using Accounting.Core.Entities;
using Accounting.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Data.Configurations;

public class ItemBatchConfiguration : IEntityTypeConfiguration<ItemBatch>
{
    public void Configure(EntityTypeBuilder<ItemBatch> builder)
    {
        builder.ToTable("item_batches");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.BatchNumber).IsRequired().HasMaxLength(100);
        builder.Property(b => b.ReceivedQuantity).HasPrecision(18, 4);
        builder.Property(b => b.AvailableQuantity).HasPrecision(18, 4);
        builder.Property(b => b.CostPerUnit).HasPrecision(18, 4);
        builder.Property(b => b.Status).HasConversion<string>();

        // Unique batch number per item per warehouse
        builder.HasIndex(b => new { b.ItemId, b.WarehouseId, b.BatchNumber }).IsUnique();

        // Critical index for FEFO queries: item + warehouse + expiry date
        builder.HasIndex(b => new { b.ItemId, b.WarehouseId, b.ExpiryDate, b.Status });

        builder.HasOne(b => b.Item)
            .WithMany(i => i.Batches)
            .HasForeignKey(b => b.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Warehouse)
            .WithMany(w => w.ItemBatches)
            .HasForeignKey(b => b.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

