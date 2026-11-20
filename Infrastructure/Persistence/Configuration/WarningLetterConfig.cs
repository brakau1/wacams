using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WACAMS.Core.Entities;

namespace WACAMS.Infrastructure.Persistence.Configuration;

/// <summary>
/// FluentAPI configuration for the WarningLetter entity.
/// Configures Type enum, signed bool default false, nullable signed file fields.
/// </summary>
public class WarningLetterConfig : IEntityTypeConfiguration<WarningLetter>
{
    public void Configure(EntityTypeBuilder<WarningLetter> builder)
    {
        // Primary Key
        builder.HasKey(wl => wl.Id);

        // Index on EmployeeId for fast retrieval of all warnings per employee
        builder.HasIndex(wl => wl.EmployeeId)
            .HasDatabaseName("IX_WarningLetters_EmployeeId");

        // Property configurations
        builder.Property(wl => wl.Id)
            .ValueGeneratedOnAdd();

        builder.Property(wl => wl.EmployeeId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(wl => wl.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(wl => wl.DateIssued)
            .IsRequired();

        builder.Property(wl => wl.Reason)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(wl => wl.IssuedBy)
            .IsRequired()
            .HasMaxLength(50);

        // Signed defaults to false — supervisor must upload signed form later
        builder.Property(wl => wl.Signed)
            .IsRequired()
            .HasDefaultValue(false);

        // SignedFileName is nullable string
        builder.Property(wl => wl.SignedFileName)
            .HasMaxLength(255);

        // SignedFilePath is nullable string (can store full URI/path)
        builder.Property(wl => wl.SignedFilePath)
            .HasMaxLength(1000);

        builder.Property(wl => wl.Acknowledgements)
            .HasMaxLength(1000);

        builder.Property(wl => wl.NextReviewDate)
            .HasColumnName("NextReviewDate");

        // Relationships
        builder.HasOne(wl => wl.Employee)
            .WithMany(e => e.WarningLetters)
            .HasForeignKey(wl => wl.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_WarningLetters_Employees_EmployeeId");

        // Indexes for overdue review queries
        builder.HasIndex(wl => wl.NextReviewDate)
            .HasDatabaseName("IX_WarningLetters_NextReviewDate")
            .IsUnique();

        builder.HasIndex(wl => wl.Type)
            .HasDatabaseName("IX_WarningLetters_Type");

        builder.HasIndex(wl => wl.Signed)
            .HasDatabaseName("IX_WarningLetters_Signed");
    }
}
