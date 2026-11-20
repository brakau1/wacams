using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WACAMS.Core.Entities;

namespace WACAMS.Infrastructure.Data;

/// <summary>
/// EF Core DbContext for the WACAMS PostgreSQL database.
/// Configures all entity mappings, relationships, and indexes via FluentAPI in OnModelCreating.
/// Overrides SaveChanges to automatically capture audit log entries for every Create/Update/Delete.
/// </summary>
public class WacamContext : DbContext
{
    private readonly IConfiguration _configuration;

    public WacamContext(DbContextOptions<WacamContext> options, IConfiguration configuration)
        : base(options)
    {
        _configuration = configuration;
    }

    #region DbSets

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<User> Users => Set<User>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<AttendanceException> AttendanceExceptions => Set<AttendanceException>();
    public DbSet<ShiftSchedule> ShiftSchedules => Set<ShiftSchedule>();
    public DbSet<WarningLetter> WarningLetters => Set<WarningLetter>();
    public DbSet<Escalation> Escalations => Set<Escalation>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Notification entities
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    #endregion

    #region Configuration

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var connectionString = _configuration.GetConnectionString("WacamDb")
                ?? throw new InvalidOperationException(
                    "Connection string 'WacamDb' not found in configuration.");
            optionsBuilder.UseNpgsql(connectionString,
                o => o.MigrationsAssembly(typeof(WacamContext).Assembly.FullName));
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WacamContext).Assembly);

        // Global query filters — soft-delete pattern for Employees
        modelBuilder.Entity<Employee>()
            .HasQueryFilter(e => e.Status != Core.Enums.EmployeeStatus.Terminated);
    }

    #endregion

    #region Audit Logging

    /// <summary>
    /// Override SaveChanges to automatically capture audit log entries for every
    /// Create, Update, and Delete operation across all tracked entities.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Capture audit entries before persisting
        var auditEntries = new List<AuditLog>();

        foreach (var entry in ChangeTracker.Entries())
        {
            var entityType = entry.Entity.GetType().Name;

            switch (entry.State)
            {
                case EntityState.Added:
                    auditEntries.Add(new AuditLog
                    {
                        Id = Guid.NewGuid(),
                        EntityType = entityType,
                        EntityId = GetBusinessKey(entry.Entity),
                        Action = "Create",
                        OldValue = null,
                        NewValue = SerializeEntity(entry.Entity),
                        PerformedBy = "System", // Will be overridden by repository/service layer
                        PerformedAt = DateTime.UtcNow,
                        IpAddress = null
                    });
                    break;

                case EntityState.Modified:
                    var oldValues = entry.Properties.ToDictionary(
                        p => p.Metadata.Name,
                        p => p.OriginalValue?.ToString() ?? "null");
                    var newValues = entry.Properties.ToDictionary(
                        p => p.Metadata.Name,
                        p => p.CurrentValue?.ToString() ?? "null");

                    auditEntries.Add(new AuditLog
                    {
                        Id = Guid.NewGuid(),
                        EntityType = entityType,
                        EntityId = GetBusinessKey(entry.Entity),
                        Action = "Update",
                        OldValue = JsonSerializer.Serialize(oldValues),
                        NewValue = JsonSerializer.Serialize(newValues),
                        PerformedBy = "System",
                        PerformedAt = DateTime.UtcNow,
                        IpAddress = null
                    });
                    break;

                case EntityState.Deleted:
                    auditEntries.Add(new AuditLog
                    {
                        Id = Guid.NewGuid(),
                        EntityType = entityType,
                        EntityId = GetBusinessKey(entry.Entity),
                        Action = "Delete",
                        OldValue = SerializeEntity(entry.Entity),
                        NewValue = null,
                        PerformedBy = "System",
                        PerformedAt = DateTime.UtcNow,
                        IpAddress = null
                    });
                    break;
            }
        }

        // Persist changes first
        var result = await base.SaveChangesAsync(cancellationToken);

        // Then persist audit logs (with performedBy filled in later by services)
        if (auditEntries.Any())
        {
            await AddRangeAsync(auditEntries, cancellationToken);
            await base.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    /// <summary>
    /// Retrieves the business key (EmployeeId or Id as string) from an entity.
    /// Used as the EntityId in audit log entries.
    /// </summary>
    private static string GetBusinessKey(object entity)
    {
        return entity switch
        {
            Employee emp => emp.EmployeeId,
            User user => user.EmployeeId,
            AttendanceRecord ar => $"{ar.EmployeeId}_{ar.Date:yyyy-MM-dd}",
            AttendanceException ae => $"{ae.EmployeeId}_{ae.Date:yyyy-MM-dd}",
            ShiftSchedule ss => $"{ss.EmployeeId}_{ss.Date:yyyy-MM-dd}",
            WarningLetter wl => $"{wl.EmployeeId}_{wl.Id}",
            Escalation esc => esc.Id.ToString(),
            Department dept => dept.Id.ToString(),
            AuditLog log => log.Id.ToString(),
            _ => ((dynamic)entity).Id.ToString()
        };
    }

    /// <summary>
    /// Serializes an entity's property values to a JSON string for audit storage.
    /// Excludes navigation properties to keep payload manageable.
    /// </summary>
    private static string? SerializeEntity(object entity)
    {
        var props = entity.GetType().GetProperties()
            .Where(p => !p.PropertyType.IsClass || p.PropertyType == typeof(string))
            .ToDictionary(
                p => p.Name,
                p => p.GetValue(entity)?.ToString() ?? "null");
        return JsonSerializer.Serialize(props);
    }

    #endregion
}
