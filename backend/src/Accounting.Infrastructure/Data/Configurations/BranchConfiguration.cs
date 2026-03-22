using Accounting.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Data.Configurations;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("branches");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Name).IsRequired().HasMaxLength(200);
        builder.Property(b => b.Code).IsRequired().HasMaxLength(20);
        builder.HasIndex(b => b.Code).IsUnique();
    }
}

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("warehouses");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Name).IsRequired().HasMaxLength(200);
        builder.Property(w => w.Code).IsRequired().HasMaxLength(20);
        builder.HasIndex(w => new { w.BranchId, w.Code }).IsUnique();

        builder.HasOne(w => w.Branch)
            .WithMany(b => b.Warehouses)
            .HasForeignKey(w => w.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

