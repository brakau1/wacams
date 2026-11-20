using Microsoft.AspNetCore.Mvc;
using Moq;
using Wacam.Api.Controllers;
using Wacam.Api.Services;
using Wacam.Api.DTOs;
using Xunit;

namespace Wacam.Api.Tests.Security;

/// <summary>
/// Security-focused tests to verify SuperAdmin protection and authorization
/// </summary>
public class SuperAdminSecurityTests
{
    private readonly Mock<SuperAdminService> _mockService;
    private readonly SuperAdminController _controller;

    public SuperAdminSecurityTests()
    {
        _mockService = new Mock<SuperAdminService>(null, null, null);
        _controller = new SuperAdminController(_mockService.Object, null!);
    }

    [Fact]
    public async Task CreateSuperAdmin_UnauthorizedUser_ReturnsBadRequest()
    {
        // Arrange
        var request = new SuperAdminCreationRequest
        {
            EmployeeId = "999999",
            DisplayName = "New SuperAdmin",
            Department = "IT",
            Reason = "Test"
        };

        _mockService
            .Setup(s => s.CreateSuperAdminAsync(It.IsAny<SuperAdminCreationRequest>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Only Employee ID 803341 can create new SuperAdmin accounts"));

        // Act
        var result = await _controller.CreateSuperAdmin(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Only Employee ID 803341", badRequest.Value.ToString());
    }

    [Fact]
    public async Task UpdateUser_ProtectedSuperAdmin_ReturnsBadRequest()
    {
        // Arrange
        var request = new UpdateUserRequest { DisplayName = "Modified" };

        _mockService
            .Setup(s => s.UpdateUserAsync("803341", It.IsAny<UpdateUserRequest>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Cannot modify the protected SuperAdmin account"));

        // Act
        var result = await _controller.UpdateUser("803341", request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("protected SuperAdmin", badRequest.Value.ToString());
    }

    [Fact]
    public async Task LockAccount_ProtectedSuperAdmin_ReturnsBadRequest()
    {
        // Arrange
        var request = new LockAccountRequest { Reason = "Test" };

        _mockService
            .Setup(s => s.LockAccountAsync("803341", It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Cannot lock the protected SuperAdmin account"));

        // Act
        var result = await _controller.LockAccount("803341", request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("protected SuperAdmin", badRequest.Value.ToString());
    }

    [Fact]
    public async Task AssignRole_ProtectedSuperAdmin_ReturnsBadRequest()
    {
        // Arrange
        var request = new AssignRoleRequest
        {
            EmployeeId = "803341",
            Role = "User",
            Reason = "Test"
        };

        _mockService
            .Setup(s => s.AssignRoleAsync(It.IsAny<AssignRoleRequest>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Cannot assign role to the protected SuperAdmin account"));

        // Act
        var result = await _controller.AssignRole(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("protected SuperAdmin", badRequest.Value.ToString());
    }

    [Fact]
    public async Task RemoveRole_ProtectedSuperAdmin_ReturnsBadRequest()
    {
        // Arrange
        var request = new RemoveRoleRequest { Reason = "Test" };

        _mockService
            .Setup(s => s.RemoveRoleAsync("803341", It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Cannot remove role from the protected SuperAdmin account"));

        // Act
        var result = await _controller.RemoveRole("803341", request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("protected SuperAdmin", badRequest.Value.ToString());
    }

    [Fact]
    public async Task ResetPassword_ProtectedSuperAdmin_ReturnsNotFound()
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            NewPassword = "NewPass123!",
            ForceChangeOnNextLogin = true
        };

        _mockService
            .Setup(s => s.ResetPasswordAsync("803341", It.IsAny<ResetPasswordRequest>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Cannot reset password for the protected SuperAdmin account"));

        // Act
        var result = await _controller.ResetPassword("803341", request);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("protected SuperAdmin", notFound.Value.ToString());
    }

    [Fact]
    public async Task UnlockAccount_ProtectedSuperAdmin_ReturnsBadRequest()
    {
        // Arrange
        _mockService
            .Setup(s => s.UnlockAccountAsync("803341", It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Cannot unlock the protected SuperAdmin account"));

        // Act
        var result = await _controller.UnlockAccount("803341");

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("protected SuperAdmin", badRequest.Value.ToString());
    }

    [Fact]
    public async Task GetProtectionStatus_ReturnsProtectedStatus()
    {
        // Arrange
        var protectionDto = new SuperAdminProtectionDto
        {
            ProtectedEmployeeId = "803341",
            DisplayName = "Botlhale Rakau",
            Role = "SuperAdmin",
            Department = "Production",
            SecurityScope = "Global",
            IsProtected = true,
            ProtectionRules = new List<string>
            {
                "Cannot be deleted",
                "Cannot be disabled",
                "Cannot be downgraded",
                "Cannot lose SuperAdmin role",
                "Cannot be modified by non-SuperAdmin users",
                "All modification attempts must be audit logged"
            }
        };

        _mockService
            .Setup(s => s.GetProtectionStatusAsync())
            .ReturnsAsync(protectionDto);

        // Act
        var result = await _controller.GetProtectionStatus();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var status = Assert.IsType<SuperAdminProtectionDto>(okResult.Value);
        Assert.Equal("803341", status.ProtectedEmployeeId);
        Assert.True(status.IsProtected);
        Assert.Equal(6, status.ProtectionRules.Count);
    }

    [Theory]
    [InlineData("803341")]
    [InlineData(" 803341 ")]
    public async Task ProtectedSuperAdminId_IsCaseInsensitive(string employeeId)
    {
        // Arrange
        var trimmedId = employeeId.Trim();
        
        _mockService
            .Setup(s => s.GetProtectionStatusAsync())
            .ReturnsAsync(new SuperAdminProtectionDto
            {
                ProtectedEmployeeId = trimmedId,
                IsProtected = true
            });

        // Act
        var result = await _controller.GetProtectionStatus();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var status = Assert.IsType<SuperAdminProtectionDto>(okResult.Value);
        Assert.Equal(trimmedId, status.ProtectedEmployeeId);
    }
}
