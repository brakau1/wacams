using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WACAMS.Core.Entities;

namespace WACAMS.Infrastructure.Persistence.Configuration;

/// <summary>
/// FluentAPI configuration for the User entity.
/// Configures Id PK, EmployeeId unique FK, Username unique, PasswordHash, Role enum, IsActive bool.
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Primary Key
        builder.HasKey(u => u.Id);

        // Unique index on EmployeeId (one-to-one with Employee)
        builder.HasIndex(u => u.EmployeeId)
            .IsUnique()
            .HasDatabaseName("IX_Users_EmployeeId");

        // Unique index on Username
        builder.HasIndex(u => u.Username)
            .IsUnique()
            .HasDatabaseName("IX_Users_Username");

        // Property configurations
        builder.Property(u => u.Id)
            .ValueGeneratedOnAdd();

        builder.Property(u => u.EmployeeId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.PasswordHash)
            .IsRequired();

        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<int>();

        // IsActive defaults to true
        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(u => u.LastLoginAt)
            .HasColumnName("LastLoginAt");

        // Relationships — one-to-one with Employee
        builder.HasOne(u => u.Employee)
            .WithOne(e => e.User)
            .HasForeignKey<User>(u => u.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_Users_Employees_EmployeeId");

        // Indexes for auth lookups
        builder.HasIndex(u => u.IsActive)
            .HasDatabaseName("IX_Users_IsActive");
    }
}
