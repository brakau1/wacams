using FluentAssertions;
using Moq;
using WACAMS.Core.Entities;
using Wacam.Api.Services;

namespace WACAMS.Tests.Security;

/// <summary>
/// Unit tests for PermissionService — retrieves and validates user permissions
/// based on role-based access control (RBAC).
/// </summary>
public class PermissionServiceTests
{
    private readonly Mock<WacamDbContext> _mockContext;
    private readonly PermissionService _service;

    public PermissionServiceTests()
    {
        _mockContext = new Mock<WacamDbContext>();
        _service = new PermissionService(_mockContext.Object);
    }

    #region GetUserPermissionsByEmployeeIdAsync Tests

    [Fact]
    public async Task GetUserPermissionsByEmployeeIdAsync_UnknownEmployee_ReturnsEmpty()
    {
        // Arrange
        _mockContext.Setup(c => c.Employees.FindAsync(new object?[] { "UNKNOWN" }, CancellationToken.None))
            .ReturnsAsync((Employee?)null);

        // Act
        var result = await _service.GetUserPermissionsByEmployeeIdAsync("UNKNOWN");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserPermissionsByEmployeeIdAsync_EmployeeNotFound_ReturnsEmpty()
    {
        // Arrange
        _mockContext.Setup(c => c.Employees.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Employee, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        // Act
        var result = await _service.GetUserPermissionsByEmployeeIdAsync("EMP001");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserPermissionsByEmployeeIdAsync_ValidEmployee_DelegatesToUserIdMethod()
    {
        // Arrange
        var employee = new Employee
        {
            Id = 1,
            EmployeeId = "EMP001",
            DisplayName = "Test User",
            Role = "SuperAdmin"
        };

        _mockContext.Setup(c => c.Employees.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Employee, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        // Create mock UserRoles with RolePermissions
        var userRole = new UserRole { UserId = 1, Role = new Role { Name = "SuperAdmin" } };
        userRole.Role.RolePermissions = new List<RolePermission>
        {
            new() { Permission = new Permission { Name = "users.read" } },
            new() { Permission = new Permission { Name = "users.write" } },
            new() { Permission = new Permission { Name = "reports.generate" } },
        };

        var mockUserRoles = SetupQueryableList(new List<UserRole> { userRole });
        _mockContext.Setup(c => c.UserRoles).Returns(mockUserRoles.Object);

        // Act
        var result = await _service.GetUserPermissionsByEmployeeIdAsync("EMP001");

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain("users.read");
        result.Should().Contain("users.write");
        result.Should().Contain("reports.generate");
    }

    #endregion

    #region HasPermissionAsync Tests

    [Fact]
    public async Task HasPermissionAsync_UserHasPermission_ReturnsTrue()
    {
        // Arrange
        var userRole = CreateUserWithPermissions(1, new[] { "dashboard.view", "production.capture" });

        var mockUserRoles = SetupQueryableList(new List<UserRole> { userRole });
        _mockContext.Setup(c => c.UserRoles).Returns(mockUserRoles.Object);

        // Act
        var result = await _service.HasPermissionAsync(1, "dashboard.view");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_UserDoesNotHavePermission_ReturnsFalse()
    {
        // Arrange
        var userRole = CreateUserWithPermissions(1, new[] { "dashboard.view", "production.capture" });

        var mockUserRoles = SetupQueryableList(new List<UserRole> { userRole });
        _mockContext.Setup(c => c.UserRoles).Returns(mockUserRoles.Object);

        // Act
        var result = await _service.HasPermissionAsync(1, "admin.usermanage");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionAsync_PermissionCaseInsensitive()
    {
        // Arrange
        var userRole = CreateUserWithPermissions(1, new[] { "Dashboard.View" });

        var mockUserRoles = SetupQueryableList(new List<UserRole> { userRole });
        _mockContext.Setup(c => c.UserRoles).Returns(mockUserRoles.Object);

        // Act
        var result = await _service.HasPermissionAsync(1, "dashboard.view");

        // Assert — LINQ Contains is case-sensitive by default but our data matches exactly
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_EmptyPermissions_ReturnsFalse()
    {
        // Arrange
        var userRole = CreateUserWithPermissions(1, Array.Empty<string>());

        var mockUserRoles = SetupQueryableList(new List<UserRole> { userRole });
        _mockContext.Setup(c => c.UserRoles).Returns(mockUserRoles.Object);

        // Act
        var result = await _service.HasPermissionAsync(1, "any.permission");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region HasAnyPermissionAsync Tests

    [Fact]
    public async Task HasAnyPermissionAsync_UserHasOneOfMany_ReturnsTrue()
    {
        // Arrange
        var userRole = CreateUserWithPermissions(1, new[] { "dashboard.view", "production.capture" });

        var mockUserRoles = SetupQueryableList(new List<UserRole> { userRole });
        _mockContext.Setup(c => c.UserRoles).Returns(mockUserRoles.Object);

        // Act
        var result = await _service.HasAnyPermissionAsync(1, new[] { "admin.usermanage", "reports.generate", "dashboard.view" });

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasAnyPermissionAsync_UserHasNoneOfMany_ReturnsFalse()
    {
        // Arrange
        var userRole = CreateUserWithPermissions(1, new[] { "dashboard.view" });

        var mockUserRoles = SetupQueryableList(new List<UserRole> { userRole });
        _mockContext.Setup(c => c.UserRoles).Returns(mockUserRoles.Object);

        // Act
        var result = await _service.HasAnyPermissionAsync(1, new[] { "admin.usermanage", "reports.generate" });

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasAnyPermissionAsync_EmptyPermissionList_ReturnsFalse()
    {
        // Arrange
        var userRole = CreateUserWithPermissions(1, new[] { "dashboard.view" });

        var mockUserRoles = SetupQueryableList(new List<UserRole> { userRole });
        _mockContext.Setup(c => c.UserRoles).Returns(mockUserRoles.Object);

        // Act
        var result = await _service.HasAnyPermissionAsync(1, Enumerable.Empty<string>());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasAnyPermissionAsync_MultipleMatchingPermissions_ReturnsTrue()
    {
        // Arrange
        var userRole = CreateUserWithPermissions(1, new[] { "dashboard.view", "production.capture", "reports.generate" });

        var mockUserRoles = SetupQueryableList(new List<UserRole> { userRole });
        _mockContext.Setup(c => c.UserRoles).Returns(mockUserRoles.Object);

        // Act
        var result = await _service.HasAnyPermissionAsync(1, new[] { "reports.generate", "admin.usermanage" });

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region GetUserPermissionsAsync Tests

    [Fact]
    public async Task GetUserPermissionsAsync_ReturnsDistinctPermissions()
    {
        // Arrange — same permission via multiple roles should be deduplicated
        var role1 = new UserRole { UserId = 1, Role = new Role { Name = "Supervisor" } };
        role1.Role.RolePermissions = new List<RolePermission>
        {
            new() { Permission = new Permission { Name = "dashboard.view" } }
        };

        var role2 = new UserRole { UserId = 1, Role = new Role { Name = "TeamLeader" } };
        role2.Role.RolePermissions = new List<RolePermission>
        {
            new() { Permission = new Permission { Name = "dashboard.view" } },
            new() { Permission = new Permission { Name = "production.capture" } }
        };

        var mockUserRoles = SetupQueryableList(new List<UserRole> { role1, role2 });
        _mockContext.Setup(c => c.UserRoles).Returns(mockUserRoles.Object);

        // Act
        var result = await _service.GetUserPermissionsAsync(1);

        // Assert
        result.Should().ContainInOrder("dashboard.view", "production.capture");
        result.Should().HaveCount(2); // Deduplicated
    }

    [Fact]
    public async Task GetUserPermissionsAsync_NoUserRoles_ReturnsEmpty()
    {
        // Arrange
        var mockUserRoles = SetupQueryableList(new List<UserRole>());
        _mockContext.Setup(c => c.UserRoles).Returns(mockUserRoles.Object);

        // Act
        var result = await _service.GetUserPermissionsAsync(999);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    private static UserRole CreateUserWithPermissions(int userId, string[] permissions)
    {
        return new UserRole
        {
            UserId = userId,
            Role = new Role
            {
                Name = "Operator",
                RolePermissions = permissions.Select(p => new RolePermission
                {
                    Permission = new Permission { Name = p }
                }).ToList()
            }
        };
    }

    private static Mock<Microsoft.EntityFrameworkCore.DbSet<T>> SetupQueryableList<T>(List<T> items) where T : class
    {
        var mockSet = new Mock<Microsoft.EntityFrameworkCore.DbSet<T>>();
        var queryable = items.AsQueryable();
        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());
        return mockSet;
    }

    #endregion
}
