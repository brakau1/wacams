using Microsoft.EntityFrameworkCore;
using WACAMS.Core.Entities;

namespace Wacam.Api.Data;

/// <summary>
/// EF Core DbContext for PostgreSQL database.
/// </summary>
public class WacamDbContext : DbContext
{
    public WacamDbContext(DbContextOptions<WacamDbContext> options) : base(options) { }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<AttendanceException> AttendanceExceptions => Set<AttendanceException>();
    public DbSet<WarningLetter> WarningLetters => Set<WarningLetter>();
    public DbSet<Escalation> Escalations => Set<Escalation>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SystemSettings> SystemSettings => Set<SystemSettings>();

    // Notification entities
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    // RBAC entities
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    // Admin Users (full access regardless of role)
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    
    // User accounts for authentication
    public DbSet<User> Users => Set<User>();
    
    // Employee access overrides
    public DbSet<EmployeeAccessOverride> EmployeeAccessOverrides => Set<EmployeeAccessOverride>();

    // SuperAdmin Enhancement Pack entities
    public DbSet<PermissionTemplate> PermissionTemplates => Set<PermissionTemplate>();
    public DbSet<PermissionTemplateVersion> PermissionTemplateVersions => Set<PermissionTemplateVersion>();
    public DbSet<PermissionTemplateAssignment> PermissionTemplateAssignments => Set<PermissionTemplateAssignment>();
    public DbSet<SecurityEvent> SecurityEvents => Set<SecurityEvent>();
    public DbSet<SecurityLockdown> SecurityLockdowns => Set<SecurityLockdown>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<AccessReviewSnapshot> AccessReviewSnapshots => Set<AccessReviewSnapshot>();
    public DbSet<BulkImportHistory> BulkImportHistory => Set<BulkImportHistory>();

    // GIS Integration entities (Phase 4A)
    public DbSet<GISIntegrationSettings> GISIntegrationSettings => Set<GISIntegrationSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Employee — unique index on EmployeeId
        modelBuilder.Entity<Employee>()
            .HasIndex(e => e.EmployeeId)
            .IsUnique();

        modelBuilder.Entity<Employee>()
            .HasOne(e => e.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // AttendanceException
        modelBuilder.Entity<AttendanceException>()
            .HasOne(ae => ae.Employee)
            .WithMany(e => e.Exceptions)
            .HasForeignKey(ae => ae.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AttendanceException>()
            .HasIndex(ae => new { ae.EmployeeId, ae.Date, ae.ExceptionType })
            .IsUnique();

        // WarningLetter
        modelBuilder.Entity<WarningLetter>()
            .HasOne(wl => wl.Employee)
            .WithMany(e => e.WarningLetters)
            .HasForeignKey(wl => wl.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WarningLetter>()
            .HasOne(wl => wl.Exception)
            .WithMany(ae => ae.WarningLetters)
            .HasForeignKey(wl => wl.ExceptionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Escalation
        modelBuilder.Entity<Escalation>()
            .HasOne(e => e.Employee)
            .WithMany(e => e.Escalations)
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Escalation>()
            .HasOne(e => e.WarningLetter)
            .WithMany(wl => wl.Escalations)
            .HasForeignKey(e => e.WarningLetterId)
            .OnDelete(DeleteBehavior.Cascade);

        // Department hierarchy
        modelBuilder.Entity<Department>()
            .HasOne(d => d.Parent)
            .WithMany(p => p.Children)
            .HasForeignKey(d => d.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // RBAC — Role
        modelBuilder.Entity<Role>()
            .HasIndex(r => r.Name)
            .IsUnique();

        // RBAC — Permission
        modelBuilder.Entity<Permission>()
            .HasIndex(p => p.Name)
            .IsUnique();

        // RBAC — UserRole junction
        modelBuilder.Entity<UserRole>()
            .HasIndex(ur => new { ur.UserId, ur.RoleId })
            .IsUnique();

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany()
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // RBAC — RolePermission junction
        modelBuilder.Entity<RolePermission>()
            .HasIndex(rp => new { rp.RoleId, rp.PermissionId })
            .IsUnique();

        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // SystemSettings — unique key per Key
        modelBuilder.Entity<SystemSettings>()
            .HasIndex(s => s.Key)
            .IsUnique();

        // Seed default settings
        modelBuilder.Entity<SystemSettings>().HasData(
            new SystemSettings { Id = Guid.NewGuid(), Key = "LateGracePeriodMinutes", Value = "5", Description = "Grace period in minutes before late is recorded" },
            new SystemSettings { Id = Guid.NewGuid(), Key = "EarlyDepartureThresholdMinutes", Value = "10", Description = "Minutes early before early departure exception is created" },
            new SystemSettings { Id = Guid.NewGuid(), Key = "MissingClockInThresholdMinutes", Value = "30", Description = "Minutes after shift start before missing clock-in is flagged" },
            new SystemSettings { Id = Guid.NewGuid(), Key = "AWOLThresholdDays", Value = "3", Description = "Consecutive days without attendance before AWOL" },
            new SystemSettings { Id = Guid.NewGuid(), Key = "EscalationTimeoutHours", Value = "24", Description = "Hours a manager has to act before escalation" },
            new SystemSettings { Id = Guid.NewGuid(), Key = "ShiftMorningStart", Value = "06:00", Description = "Morning shift start time" },
            new SystemSettings { Id = Guid.NewGuid(), Key = "ShiftMorningEnd", Value = "14:00", Description = "Morning shift end time" },
            new SystemSettings { Id = Guid.NewGuid(), Key = "ShiftAfternoonStart", Value = "14:00", Description = "Afternoon shift start time" },
            new SystemSettings { Id = Guid.NewGuid(), Key = "ShiftAfternoonEnd", Value = "22:00", Description = "Afternoon shift end time" },
            new SystemSettings { Id = Guid.NewGuid(), Key = "ShiftNightStart", Value = "22:00", Description = "Night shift start time" },
            new SystemSettings { Id = Guid.NewGuid(), Key = "ShiftNightEnd", Value = "06:00", Description = "Night shift end time" },
            new SystemSettings { Id = Guid.NewGuid(), Key = "DefaultTargetAttendancePercent", Value = "95.0", Description = "Default target attendance percentage for departments" },
            new SystemSettings { Id = Guid.NewGuid(), Key = "WarningProgressionEnabled", Value = "true", Description = "Enable progressive warning discipline path" },
            new SystemSettings { Id = Guid.NewGuid(), Key = "AutoEscalateFinalWarning", Value = "true", Description = "Automatically escalate when FinalWrittenWarning is generated" },
            new SystemSettings { Id = Guid.NewGuid(), Key = "RefreshTokenExpiryDays", Value = "7", Description = "Refresh token validity in days" }
        );

        // SuperAdmin Enhancement Pack entity configurations
        modelBuilder.Entity<PermissionTemplate>()
            .HasIndex(pt => pt.Name)
            .IsUnique();

        modelBuilder.Entity<PermissionTemplateVersion>()
            .HasOne(ptv => ptv.Template)
            .WithMany(pt => pt.Versions)
            .HasForeignKey(ptv => ptv.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PermissionTemplateAssignment>()
            .HasIndex(pta => new { pta.TemplateId, pta.EmployeeId })
            .IsUnique();

        modelBuilder.Entity<PermissionTemplateAssignment>()
            .HasOne(pta => pta.Template)
            .WithMany(pt => pt.Assignments)
            .HasForeignKey(pta => pta.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PermissionTemplateAssignment>()
            .HasOne(pta => pta.Employee)
            .WithMany()
            .HasForeignKey(pta => pta.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SecurityEvent>()
            .HasIndex(se => se.Timestamp);

        modelBuilder.Entity<SecurityEvent>()
            .HasIndex(se => se.EventType);

        modelBuilder.Entity<SecurityEvent>()
            .HasOne(se => se.Employee)
            .WithMany()
            .HasForeignKey(se => se.EmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<SecurityLockdown>()
            .HasOne(sl => sl.Department)
            .WithMany()
            .HasForeignKey(sl => sl.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<UserSession>()
            .HasIndex(us => us.EmployeeId);

        modelBuilder.Entity<UserSession>()
            .HasIndex(us => us.IsActive);

        modelBuilder.Entity<UserSession>()
            .HasOne(us => us.Employee)
            .WithMany()
            .HasForeignKey(us => us.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AccessReviewSnapshot>()
            .HasIndex(ars => ars.EmployeeId);

        modelBuilder.Entity<AccessReviewSnapshot>()
            .HasIndex(ars => ars.SnapshotDate);

        modelBuilder.Entity<AccessReviewSnapshot>()
            .HasOne(ars => ars.Employee)
            .WithMany()
            .HasForeignKey(ars => ars.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BulkImportHistory>()
            .HasIndex(bih => bih.ImportDate);

        modelBuilder.Entity<BulkImportHistory>()
            .HasIndex(bih => bih.Status);
    }
}
