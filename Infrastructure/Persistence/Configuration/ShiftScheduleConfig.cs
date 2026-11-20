using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WACAMS.Core.Entities;

namespace WACAMS.Infrastructure.Persistence.Configuration;

/// <summary>
/// FluentAPI configuration for the ShiftSchedule entity.
/// Configures composite unique index on (EmployeeId, Date, ShiftCode).
/// </summary>
public class ShiftScheduleConfig : IEntityTypeConfiguration<ShiftSchedule>
{
    public void Configure(EntityTypeBuilder<ShiftSchedule> builder)
    {
        // Primary Key
        builder.HasKey(ss => ss.Id);

        // Composite unique index on (EmployeeId, Date) — one shift assignment per employee per day
        builder.HasIndex(ss => new { ss.EmployeeId, ss.Date })
            .IsUnique()
            .HasDatabaseName("IX_ShiftSchedules_EmployeeId_Date");

        // Property configurations
        builder.Property(ss => ss.Id)
            .ValueGeneratedOnAdd();

        builder.Property(ss => ss.EmployeeId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ss => ss.Date)
            .IsRequired();

        builder.Property(ss => ss.ShiftCode)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(ss => ss.StartTime)
            .IsRequired();

        builder.Property(ss => ss.EndTime)
            .IsRequired();

        builder.Property(ss => ss.Cancelled)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(ss => ss.CancellationReason)
            .HasMaxLength(500);

        // Relationships
        builder.HasOne(ss => ss.Employee)
            .WithMany()
            .HasForeignKey(ss => ss.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_ShiftSchedules_Employees_EmployeeId");

        // Indexes
        builder.HasIndex(ss => ss.Date)
            .HasDatabaseName("IX_ShiftSchedules_Date");

        builder.HasIndex(ss => ss.ShiftCode)
            .HasDatabaseName("IX_ShiftSchedules_ShiftCode");

        builder.HasIndex(ss => ss.Cancelled)
            .HasDatabaseName("IX_ShiftSchedules_Cancelled");
    }
}
