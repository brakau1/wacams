using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WACAMS.Core.Entities;

namespace WACAMS.Infrastructure.Persistence.Configuration;

/// <summary>
/// FluentAPI configuration for the AttendanceRecord entity.
/// Configures composite index on (EmployeeId, Date) for fast daily lookups.
/// </summary>
public class AttendanceRecordConfig : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        // Primary Key
        builder.HasKey(ar => ar.Id);

        // Unique composite index on (EmployeeId, Date) — one record per employee per day
        builder.HasIndex(ar => new { ar.EmployeeId, ar.Date })
            .IsUnique()
            .HasDatabaseName("IX_AttendanceRecords_EmployeeId_Date");

        // Property configurations
        builder.Property(ar => ar.Id)
            .ValueGeneratedOnAdd();

        builder.Property(ar => ar.EmployeeId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ar => ar.Date)
            .IsRequired();

        builder.Property(ar => ar.ClockInTime)
            .HasColumnName("ClockInTime");

        builder.Property(ar => ar.ClockOutTime)
            .HasColumnName("ClockOutTime");

        builder.Property(ar => ar.ExpectedClockIn)
            .IsRequired();

        builder.Property(ar => ar.ExpectedClockOut)
            .IsRequired();

        builder.Property(ar => ar.LateMinutes)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(ar => ar.EarlyMinutes)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(ar => ar.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(ar => ar.SourceSystem)
            .HasMaxLength(50);

        // Relationships
        builder.HasOne(ar => ar.Employee)
            .WithMany(e => e.AttendanceRecords)
            .HasForeignKey(ar => ar.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_AttendanceRecords_Employees_EmployeeId");

        // ShiftSchedule may optionally reference this AttendanceRecord (one-to-zero-or-one)
        // The FK lives on ShiftSchedule as a shared-primary-key relationship

        // Indexes for date range queries
        builder.HasIndex(ar => ar.Date)
            .HasDatabaseName("IX_AttendanceRecords_Date");

        builder.HasIndex(ar => ar.Status)
            .HasDatabaseName("IX_AttendanceRecords_Status");
    }
}
