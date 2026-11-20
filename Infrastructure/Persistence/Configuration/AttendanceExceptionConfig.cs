using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WACAMS.Core.Entities;

namespace WACAMS.Infrastructure.Persistence.Configuration;

/// <summary>
/// FluentAPI configuration for the AttendanceException entity.
/// Configures InvestigationStatus default to Open, nullable InvestigatedBy FK.
/// </summary>
public class AttendanceExceptionConfig : IEntityTypeConfiguration<AttendanceException>
{
    public void Configure(EntityTypeBuilder<AttendanceException> builder)
    {
        // Primary Key
        builder.HasKey(ae => ae.Id);

        // Unique composite index on (EmployeeId, Date, ExceptionType) — prevents duplicates
        builder.HasIndex(ae => new { ae.EmployeeId, ae.Date, ae.ExceptionType })
            .IsUnique()
            .HasDatabaseName("IX_AttendanceExceptions_EmployeeId_Date_Type");

        // Property configurations
        builder.Property(ae => ae.Id)
            .ValueGeneratedOnAdd();

        builder.Property(ae => ae.EmployeeId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ae => ae.Date)
            .IsRequired();

        builder.Property(ae => ae.ExceptionType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(ae => ae.Description)
            .HasMaxLength(2000);

        // InvestigationStatus defaults to Open
        builder.Property(ae => ae.InvestigationStatus)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(Core.Enums.InvestigationStatus.Open);

        // InvestigatedBy is nullable — set only when investigation completes
        builder.Property(ae => ae.InvestigatedBy)
            .HasMaxLength(50);

        builder.Property(ae => ae.InvestigationNotes)
            .HasMaxLength(4000);

        builder.Property(ae => ae.ResolvedAt)
            .HasColumnName("ResolvedAt");

        // Relationships
        builder.HasOne(ae => ae.Employee)
            .WithMany(e => e.Exceptions)
            .HasForeignKey(ae => ae.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_AttendanceExceptions_Employees_EmployeeId");

        // Indexes
        builder.HasIndex(ae => ae.InvestigationStatus)
            .HasDatabaseName("IX_AttendanceExceptions_InvestigationStatus");

        builder.HasIndex(ae => ae.Date)
            .HasDatabaseName("IX_AttendanceExceptions_Date");
    }
}
