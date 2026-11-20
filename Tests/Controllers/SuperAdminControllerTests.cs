using Microsoft.AspNetCore.Mvc;
using Moq;
using Wacam.Api.Controllers;
using Wacam.Api.DTOs;
using Wacam.Api.Services;
using Xunit;

namespace Wacam.Api.Tests.Controllers;

public class SuperAdminControllerTests
{
    private readonly Mock<SuperAdminService> _mockService;
    private readonly SuperAdminController _controller;

    public SuperAdminControllerTests()
    {
        _mockService = new Mock<SuperAdminService>(null, null, null);
        _controller = new SuperAdminController(_mockService.Object, null!);
    }

    [Fact]
    public async Task CreateSuperAdmin_WithValidRequest_ReturnsCreatedUser()
    {
        // Arrange
        var request = new SuperAdminCreationRequest
        {
            EmployeeId = "999999",
            DisplayName = "New SuperAdmin",
            Department = "IT",
            Reason = "System expansion"
        };

        var expectedUser = new UserManagementDto
        {
            EmployeeId = "999999",
            DisplayName = "New SuperAdmin",
            Role = "SuperAdmin"
        };

        _mockService
            .Setup(s => s.CreateSuperAdminAsync(It.IsAny<SuperAdminCreationRequest>(), It.IsAny<string>()))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _controller.CreateSuperAdmin(request);

        // Assert
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnedUser = Assert.IsType<UserManagementDto>(createdAtActionResult.Value);
        Assert.Equal("999999", returnedUser.EmployeeId);
        Assert.Equal("SuperAdmin", returnedUser.Role);
    }

    [Fact]
    public async Task CreateSuperAdmin_WithInvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = new SuperAdminCreationRequest
        {
            EmployeeId = "999999",
            DisplayName = "New SuperAdmin",
            Department = "IT",
            Reason = "System expansion"
        };

        _mockService
            .Setup(s => s.CreateSuperAdminAsync(It.IsAny<SuperAdminCreationRequest>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Only Employee ID 803341 can create new SuperAdmin accounts"));

        // Act
        var result = await _controller.CreateSuperAdmin(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var message = Assert.IsType<string>(badRequestResult.Value);
        Assert.Contains("Only Employee ID 803341", message);
    }

    [Fact]
    public async Task GetProtectionStatus_ReturnsProtectionStatus()
    {
        // Arrange
        var expectedStatus = new SuperAdminProtectionDto
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
            .ReturnsAsync(expectedStatus);

        // Act
        var result = await _controller.GetProtectionStatus();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var status = Assert.IsType<SuperAdminProtectionDto>(okResult.Value);
        Assert.Equal("803341", status.ProtectedEmployeeId);
        Assert.True(status.IsProtected);
        Assert.Equal(6, status.ProtectionRules.Count);
    }
}
