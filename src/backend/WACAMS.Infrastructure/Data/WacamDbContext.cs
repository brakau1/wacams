using Microsoft.EntityFrameworkCore;
using WACAMS.Core.Escalations.Models;
using WACAMS.Core.Exceptions.Models;
using WACAMS.Api.Integration.GIS.Models;

namespace WACAMS.Infrastructure.Data;

/// <summary>
/// EF Core DbContext for WACAMS PostgreSQL database
/// </summary>
public class WacamDbContext : DbContext
{
    public WacamDbContext(DbContextOptions<WacamDbContext> options) : base(options) { }

    #region DbSets

    // Core entities
    public DbSet<AttendanceException> AttendanceExceptions => Set<AttendanceException>();
    
    // Escalation workflow
    public DbSet<Escalation> Escalations => Set<Escalation>();
    public DbSet<EscalationHistory> EscalationHistory => Set<EscalationHistory>();
    
    // GIS iWork Integration
    public DbSet<GISEmployee> GISEmployees => Set<GISEmployee>();
    public DbSet<GISAttendanceRecord> GISAttendanceRecords => Set<GISAttendanceRecord>();
    public DbSet<GISClockTransaction> GISClockTransactions => Set<GISClockTransaction>();
    public DbSet<GISShiftSchedule> GISShiftSchedules => Set<GISShiftSchedule>();
    public DbSet<GISShiftCancellation> GISShiftCancellations => Set<GISShiftCancellation>();
    public DbSet<GISLeaveRecord> GISLeaveRecords => Set<GISLeaveRecord>();
    public DbSet<GISSyncLog> GISSyncLogs => Set<GISSyncLog>();

    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Escalation entity
        modelBuilder.Entity<Escalation>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.EmployeeId)
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.AssignedTo)
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.CurrentLevel)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.DocumentUrl)
                .HasMaxLength(500);
            
            entity.Property(e => e.Notes)
                .HasMaxLength(2000);
            
            entity.Property(e => e.ActionTakenNotes)
                .HasMaxLength(2000);
            
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50);

            // Indexes
            entity.HasIndex(e => e.ExceptionId);
            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.NextEscalationTime);
            entity.HasIndex(e => e.CreatedAt);

            // Relationships
            entity.HasOne(e => e.Exception)
                .WithMany()
                .HasForeignKey(e => e.ExceptionId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasMany(e => e.History)
                .WithOne()
                .HasForeignKey(h => h.EscalationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure EscalationHistory entity
        modelBuilder.Entity<EscalationHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.FromManagerName)
                .HasMaxLength(200);
            
            entity.Property(e => e.ToManagerName)
                .HasMaxLength(200);
            
            entity.Property(e => e.Action)
                .HasMaxLength(100)
                .IsRequired();
            
            entity.Property(e => e.Comments)
                .HasMaxLength(2000);

            // Indexes
            entity.HasIndex(e => e.EscalationId);
            entity.HasIndex(e => e.Timestamp);
        });

        // Configure AttendanceException entity
        modelBuilder.Entity<AttendanceException>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.EmployeeId)
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.ExceptionCategory)
                .HasConversion<string>()
                .HasMaxLength(100)
                .IsRequired();
            
            entity.Property(e => e.ApprovalStatus)
                .HasConversion<string>()
                .HasMaxLength(50);
            
            entity.Property(e => e.ApprovedBy)
                .HasMaxLength(50);
            
            entity.Property(e => e.ApprovalComments)
                .HasMaxLength(2000);

            // Indexes
            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => e.ExceptionDate);
            entity.HasIndex(e => e.ExceptionCategory);
            entity.HasIndex(e => e.ApprovalStatus);
        });

        // Configure GIS entities
        modelBuilder.ConfigureGISEntities();
    }
}
