using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WACAMS.Core.Entities;

namespace WACAMS.Infrastructure.Persistence.Configuration;

/// <summary>
/// FluentAPI configuration for the AuditLog entity.
/// Configures EntityType, EntityId, Action, OldValue nvarchar(max), NewValue nvarchar(max).
/// </summary>
public class AuditLogConfig : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        // Primary Key
        builder.HasKey(al => al.Id);

        // Property configurations
        builder.Property(al => al.Id)
            .ValueGeneratedOnAdd();

        builder.Property(al => al.EntityType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(al => al.EntityId)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(al => al.Action)
            .IsRequired()
            .HasMaxLength(20);

        // OldValue and NewValue are stored as nvarchar(max) for large JSON snapshots
        builder.Property(al => al.OldValue)
            .HasColumnType("text")
            .HasColumnName("OldValue");

        builder.Property(al => al.NewValue)
            .HasColumnType("text")
            .HasColumnName("NewValue");

        builder.Property(al => al.PerformedBy)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(al => al.PerformedAt)
            .IsRequired();

        builder.Property(al => al.IpAddress)
            .HasMaxLength(45);

        // Indexes for efficient audit trail queries
        builder.HasIndex(al => al.EntityType)
            .HasDatabaseName("IX_AuditLogs_EntityType");

        builder.HasIndex(al => new { al.EntityType, al.EntityId })
            .HasDatabaseName("IX_AuditLogs_EntityLookup");

        builder.HasIndex(al => al.PerformedAt)
            .HasDatabaseName("IX_AuditLogs_PerformedAt");

        builder.HasIndex(al => al.PerformedBy)
            .HasDatabaseName("IX_AuditLogs_PerformedBy");

        builder.HasIndex(al => al.Action)
            .HasDatabaseName("IX_AuditLogs_Action");
    }
}
