using Accounting.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Data.Configurations;

public class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.ToTable("alerts");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.AlertType).HasConversion<string>().IsRequired();
        builder.Property(a => a.Severity).HasConversion<string>().IsRequired();
        builder.Property(a => a.Status).HasConversion<string>().IsRequired();
        builder.Property(a => a.Message).IsRequired().HasMaxLength(500);
        builder.Property(a => a.Metadata).HasColumnType("text");

        // ── Dedup indexes ────────────────────────────────────────────────────
        // LowStock:   one active alert per (item, warehouse)
        builder.HasIndex(a => new { a.AlertType, a.ItemId, a.WarehouseId, a.Status })
            .HasDatabaseName("IX_alerts_LowStock_Dedup");

        // NearExpiry / Expired: one active alert per batch
        builder.HasIndex(a => new { a.AlertType, a.ItemBatchId, a.Status })
            .HasDatabaseName("IX_alerts_Batch_Dedup");

        // Querying by status (most common dashboard query)
        builder.HasIndex(a => a.Status)
            .HasDatabaseName("IX_alerts_Status");

        // ── Relationships ────────────────────────────────────────────────────
        builder.HasOne(a => a.Item).WithMany(i => i.Alerts)
            .HasForeignKey(a => a.ItemId).OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.ItemBatch).WithMany(b => b.Alerts)
            .HasForeignKey(a => a.ItemBatchId).OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.Warehouse).WithMany()
            .HasForeignKey(a => a.WarehouseId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.EntityName).IsRequired().HasMaxLength(100);
        builder.Property(a => a.EntityId).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Action).IsRequired().HasMaxLength(50);
        builder.Property(a => a.IpAddress).HasMaxLength(50);

        builder.HasIndex(a => new { a.EntityName, a.EntityId });
        builder.HasIndex(a => a.Timestamp);

        builder.HasOne(a => a.User).WithMany()
            .HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class ImportJobConfiguration : IEntityTypeConfiguration<ImportJob>
{
    public void Configure(EntityTypeBuilder<ImportJob> builder)
    {
        builder.ToTable("import_jobs");
        builder.HasKey(j => j.Id);
        builder.Property(j => j.JobType).HasConversion<string>();
        builder.Property(j => j.Status).HasConversion<string>();
        builder.Property(j => j.OriginalFileName).IsRequired().HasMaxLength(300);
        builder.Property(j => j.StoredFilePath).IsRequired().HasMaxLength(500);

        builder.HasOne(j => j.CreatedBy).WithMany()
            .HasForeignKey(j => j.CreatedById).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ImportJobRowConfiguration : IEntityTypeConfiguration<ImportJobRow>
{
    public void Configure(EntityTypeBuilder<ImportJobRow> builder)
    {
        builder.ToTable("import_job_rows");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Status).HasConversion<string>();
        builder.Property(r => r.RawData).IsRequired();

        builder.HasIndex(r => new { r.ImportJobId, r.Status });

        builder.HasOne(r => r.ImportJob).WithMany(j => j.Rows)
            .HasForeignKey(r => r.ImportJobId).OnDelete(DeleteBehavior.Cascade);
    }
}

