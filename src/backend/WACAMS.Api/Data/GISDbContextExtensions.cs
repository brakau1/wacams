using Microsoft.EntityFrameworkCore;
using WACAMS.Api.Integration.GIS.Models;

namespace WACAMS.Api.Data;

/// <summary>
/// Extension methods for configuring GIS iWork integration entities in DbContext
/// </summary>
public static class GISDbContextExtensions
{
    /// <summary>
    /// Configure GIS iWork integration entities
    /// </summary>
    public static void ConfigureGISEntities(this ModelBuilder modelBuilder)
    {
        // GISEmployee
        modelBuilder.Entity<GISEmployee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId);
            entity.Property(e => e.EmployeeId).HasMaxLength(20);
            entity.Property(e => e.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Gender).HasMaxLength(10);
            entity.Property(e => e.Nationality).HasMaxLength(100);
            entity.Property(e => e.DepartmentCode).HasMaxLength(20);
            entity.Property(e => e.DepartmentName).HasMaxLength(200);
            entity.Property(e => e.EmploymentStatus).HasMaxLength(20);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.ManagerId).HasMaxLength(20);

            entity.HasIndex(e => e.DepartmentCode);
            entity.HasIndex(e => e.EmploymentStatus);
            entity.HasIndex(e => e.LastSyncedAt);
        });

        // GISAttendanceRecord
        modelBuilder.Entity<GISAttendanceRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EmployeeId).HasMaxLength(20).IsRequired();
            entity.Property(e => e.ShiftType).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Source).HasMaxLength(50).HasDefaultValue("GIS_iWork");

            entity.HasIndex(e => new { e.EmployeeId, e.AttendanceDate, e.ShiftType }).IsUnique();
            entity.HasIndex(e => e.AttendanceDate);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.SyncedAt);
        });

        // GISClockTransaction
        modelBuilder.Entity<GISClockTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EmployeeId).HasMaxLength(20).IsRequired();
            entity.Property(e => e.TransactionType).HasMaxLength(20).IsRequired();
            entity.Property(e => e.TerminalId).HasMaxLength(50);
            entity.Property(e => e.TerminalLocation).HasMaxLength(200);
            entity.Property(e => e.BiometricData).HasMaxLength(500);
            entity.Property(e => e.ValidationMessage).HasMaxLength(500);

            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => e.TransactionDateTime);
            entity.HasIndex(e => e.TransactionType);
            entity.HasIndex(e => e.SyncedAt);
        });

        // GISShiftSchedule
        modelBuilder.Entity<GISShiftSchedule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EmployeeId).HasMaxLength(20).IsRequired();
            entity.Property(e => e.ShiftType).HasMaxLength(20).IsRequired();
            entity.Property(e => e.ShiftCode).HasMaxLength(50);

            entity.HasIndex(e => new { e.EmployeeId, e.ScheduleDate }).IsUnique();
            entity.HasIndex(e => e.ScheduleDate);
            entity.HasIndex(e => e.ShiftType);
            entity.HasIndex(e => e.SyncedAt);
        });

        // GISShiftCancellation
        modelBuilder.Entity<GISShiftCancellation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EmployeeId).HasMaxLength(20).IsRequired();
            entity.Property(e => e.OriginalShiftType).HasMaxLength(20).IsRequired();
            entity.Property(e => e.CancellationReason).HasMaxLength(500).IsRequired();
            entity.Property(e => e.CancelledBy).HasMaxLength(50).IsRequired();

            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => e.CancelledDate);
            entity.HasIndex(e => e.SyncedAt);
        });

        // GISLeaveRecord
        modelBuilder.Entity<GISLeaveRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EmployeeId).HasMaxLength(20).IsRequired();
            entity.Property(e => e.LeaveType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.LeaveReason).HasMaxLength(1000);
            entity.Property(e => e.ApprovedBy).HasMaxLength(50);
            entity.Property(e => e.SupportingDocument).HasMaxLength(500);

            entity.HasIndex(e => new { e.EmployeeId, e.LeaveDate }).IsUnique();
            entity.HasIndex(e => e.LeaveDate);
            entity.HasIndex(e => e.LeaveType);
            entity.HasIndex(e => e.IsApproved);
            entity.HasIndex(e => e.SyncedAt);
        });

        // GISSyncLog
        modelBuilder.Entity<GISSyncLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SyncType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);

            entity.HasIndex(e => e.SyncType);
            entity.HasIndex(e => e.SyncedAt);
            entity.HasIndex(e => e.Success);
        });
    }
}
