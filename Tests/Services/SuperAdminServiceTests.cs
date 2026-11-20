using Microsoft.EntityFrameworkCore;
using Wacam.Api.Data;
using Wacam.Api.Services;
using Wacam.Api.DTOs;
using WACAMS.Core.Entities;
using Xunit;

namespace Wacam.Api.Tests.Services;

public class SuperAdminServiceTests
{
    private readonly WacamDbContext _context;
    private readonly SuperAdminService _service;

    public SuperAdminServiceTests()
    {
        var options = new DbContextOptionsBuilder<WacamDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new WacamDbContext(options);
        _service = new SuperAdminService(_context, null!, null!);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Add protected SuperAdmin
        _context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            EmployeeId = "803341",
            DisplayName = "Botlhale Rakau",
            Email = "803341@wacams.local",
            PasswordHash = "hashed_password",
            Role = Role.SuperAdmin,
            IsActive = true,
            IsLocked = false,
            CreatedAt = DateTime.UtcNow
        });

        // Add test user
        _context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            EmployeeId = "123456",
            DisplayName = "Test User",
            Email = "123456@wacams.local",
            PasswordHash = "hashed_password",
            Role = Role.User,
            IsActive = true,
            IsLocked = false,
            CreatedAt = DateTime.UtcNow
        });

        _context.SaveChanges();
    }

    [Fact]
    public async Task UpdateUser_ProtectedSuperAdmin_ThrowsException()
    {
        // Arrange
        var request = new UpdateUserRequest
        {
            DisplayName = "Modified Name"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateUserAsync("803341", request, "803341"));

        Assert.Equal("Cannot modify the protected SuperAdmin account (Employee ID: 803341)", exception.Message);
    }

    [Fact]
    public async Task ResetPassword_ProtectedSuperAdmin_ThrowsException()
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            NewPassword = "NewPassword123!",
            ForceChangeOnNextLogin = true
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ResetPasswordAsync("803341", request, "803341"));

        Assert.Equal("Cannot reset password for the protected SuperAdmin account (Employee ID: 803341)", exception.Message);
    }

    [Fact]
    public async Task LockAccount_ProtectedSuperAdmin_ThrowsException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.LockAccountAsync("803341", "Test reason", "803341"));

        Assert.Equal("Cannot lock the protected SuperAdmin account (Employee ID: 803341)", exception.Message);
    }

    [Fact]
    public async Task UnlockAccount_ProtectedSuperAdmin_ThrowsException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UnlockAccountAsync("803341", "803341"));

        Assert.Equal("Cannot unlock the protected SuperAdmin account (Employee ID: 803341)", exception.Message);
    }

    [Fact]
    public async Task AssignRole_ProtectedSuperAdmin_ThrowsException()
    {
        // Arrange
        var request = new AssignRoleRequest
        {
            EmployeeId = "803341",
            Role = "User",
            Reason = "Test"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AssignRoleAsync(request, "803341"));

        Assert.Equal("Cannot assign role to the protected SuperAdmin account (Employee ID: 803341)", exception.Message);
    }

    [Fact]
    public async Task RemoveRole_ProtectedSuperAdmin_ThrowsException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RemoveRoleAsync("803341", "Test", "803341"));

        Assert.Equal("Cannot remove role from the protected SuperAdmin account (Employee ID: 803341)", exception.Message);
    }

    [Fact]
    public async Task CreateSuperAdmin_NonProtectedSuperAdmin_ThrowsException()
    {
        // Arrange
        var request = new SuperAdminCreationRequest
        {
            EmployeeId = "999999",
            DisplayName = "New SuperAdmin",
            Department = "IT",
            Reason = "System expansion"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateSuperAdminAsync(request, "123456"));

        Assert.Equal("Only Employee ID 803341 can create new SuperAdmin accounts", exception.Message);
    }

    [Fact]
    public async Task CreateSuperAdmin_ProtectedSuperAdmin_Success()
    {
        // Arrange
        var request = new SuperAdminCreationRequest
        {
            EmployeeId = "999999",
            DisplayName = "New SuperAdmin",
            Department = "IT",
            Reason = "System expansion"
        };

        // Act
        var result = await _service.CreateSuperAdminAsync(request, "803341");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("999999", result.EmployeeId);
        Assert.Equal("SuperAdmin", result.Role);
        Assert.True(result.MustChangePassword);
    }

    [Fact]
    public async Task GetProtectionStatus_ReturnsCorrectStatus()
    {
        // Act
        var result = await _service.GetProtectionStatusAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("803341", result.ProtectedEmployeeId);
        Assert.Equal("Botlhale Rakau", result.DisplayName);
        Assert.Equal("SuperAdmin", result.Role);
        Assert.Equal("Production", result.Department);
        Assert.Equal("Global", result.SecurityScope);
        Assert.True(result.IsProtected);
        Assert.Equal(6, result.ProtectionRules.Count);
    }

    [Fact]
    public async Task UpdateUser_RegularUser_Success()
    {
        // Arrange
        var request = new UpdateUserRequest
        {
            DisplayName = "Modified Name"
        };

        // Act
        var result = await _service.UpdateUserAsync("123456", request, "803341");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Modified Name", result.DisplayName);
    }

    [Fact]
    public async Task LockAccount_RegularUser_Success()
    {
        // Act
        await _service.LockAccountAsync("123456", "Test reason", "803341");

        // Assert
        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == "123456");
        Assert.NotNull(user);
        Assert.True(user.IsLocked);
    }
}
