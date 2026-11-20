using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WACAMS.Core.Entities;

namespace WACAMS.Infrastructure.Persistence.Configuration;

/// <summary>
/// FluentAPI configuration for the Escalation entity.
/// Configures WarningLetterId FK, FromManagerId FK, ToManagerId FK, Level int, Status enum.
/// </summary>
public class EscalationConfig : IEntityTypeConfiguration<Escalation>
{
    public void Configure(EntityTypeBuilder<Escalation> builder)
    {
        // Primary Key
        builder.HasKey(e => e.Id);

        // Foreign key to WarningLetter
        builder.HasIndex(e => e.WarningLetterId)
            .HasDatabaseName("IX_Escalations_WarningLetterId");

        // Property configurations
        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd();

        builder.Property(e => e.WarningLetterId)
            .IsRequired();

        builder.Property(e => e.FromManagerId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.ToManagerId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Level)
            .IsRequired();

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.ActionRequired)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(e => e.CompletedAt)
            .HasColumnName("CompletedAt");

        // Relationships
        builder.HasOne(e => e.WarningLetter)
            .WithMany(wl => wl.Escalations)
            .HasForeignKey(e => e.WarningLetterId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_Escalations_WarningLetters_WarningLetterId");

        // Indexes
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_Escalations_Status");

        builder.HasIndex(e => new { e.FromManagerId, e.ToManagerId })
            .HasDatabaseName("IX_Escalations_ManagerChain");

        builder.HasIndex(e => e.Level)
            .HasDatabaseName("IX_Escalations_Level");
    }
}
