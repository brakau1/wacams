using FluentAssertions;
using WACAMS.Core.Enums;

namespace WACAMS.Tests.Security;

/// <summary>
/// Unit tests for Role enum values and role-based access control logic.
/// Verifies the correct numeric ordering for privilege escalation checks.
/// </summary>
public class RoleHierarchyTests
{
    #region Role Enum Value Tests

    [Fact]
    public void Role_SuperAdmin_HasValueSix()
    {
        // Assert
        ((int)Role.SuperAdmin).Should().Be(6);
    }

    [Fact]
    public void Role_Admin_HasValueFive()
    {
        // Assert
        ((int)Role.Admin).Should().Be(5);
    }

    [Fact]
    public void Role_Supervisor_HasValueFour()
    {
        // Assert
        ((int)Role.Supervisor).Should().Be(4);
    }

    [Fact]
    public void Role_TeamLeader_HasValueThree()
    {
        // Assert
        ((int)Role.TeamLeader).Should().Be(3);
    }

    [Fact]
    public void Role_HR_HasValueTwo()
    {
        // Assert
        ((int)Role.HR).Should().Be(2);
    }

    [Fact]
    public void Role_Operator_HasValueOne()
    {
        // Assert
        ((int)Role.Operator).Should().Be(1);
    }

    [Fact]
    public void Role_EnumValues_AreSequentialAndAscending()
    {
        // Assert — SuperAdmin > Admin > Supervisor > TeamLeader > HR > Operator
        ((int)Role.SuperAdmin).Should().BeGreaterThan((int)Role.Admin);
        ((int)Role.Admin).Should().BeGreaterThan((int)Role.Supervisor);
        ((int)Role.Supervisor).Should().BeGreaterThan((int)Role.TeamLeader);
        ((int)Role.TeamLeader).Should().BeGreaterThan((int)Role.HR);
        ((int)Role.HR).Should().BeGreaterThan((int)Role.Operator);
    }

    [Fact]
    public void Role_EnumValues_NoGapsBetweenAdjacentLevels()
    {
        // Each adjacent pair should differ by exactly 1
        ((int)Role.SuperAdmin - (int)Role.Admin).Should().Be(1);
        ((int)Role.Admin - (int)Role.Supervisor).Should().Be(1);
        ((int)Role.Supervisor - (int)Role.TeamLeader).Should().Be(1);
        ((int)Role.TeamLeader - (int)Role.HR).Should().Be(1);
        ((int)Role.HR - (int)Role.Operator).Should().Be(1);
    }

    #endregion

    #region Role Hierarchy Logic Tests

    [Fact]
    public void Role_Hierarchy_IsTopDownDescending()
    {
        // Arrange — list roles from highest to lowest privilege
        var orderedRoles = new[]
        {
            Role.SuperAdmin,
            Role.Admin,
            Role.Supervisor,
            Role.TeamLeader,
            Role.HR,
            Role.Operator
        };

        // Assert — each subsequent role must be lower than the previous
        for (int i = 0; i < orderedRoles.Length - 1; i++)
        {
            ((int)orderedRoles[i]).Should().BeGreaterThan((int)orderedRoles[i + 1]);
        }
    }

    [Fact]
    public void Role_CanCompareForPrivilegeCheck_SuperAdminHasHighest()
    {
        // Arrange & Act
        var privileges = Enum.GetValues<Role>();

        // Assert
        privileges.Cast<Role>().Max(r => (int)r).Should().Be(6);
        privileges.Cast<Role>().Min(r => (int)r).Should().Be(1);
    }

    [Fact]
    public void Role_BelowLevel_Checks_WorksCorrectly()
    {
        // Arrange & Act
        var superAdminIsBelowAdmin = (int)Role.SuperAdmin < (int)Role.Admin;
        var operatorIsBelowTeamLeader = (int)Role.Operator < (int)Role.TeamLeader;
        var supervisorIsAboveHR = (int)Role.Supervisor > (int)Role.HR;

        // Assert
        superAdminIsBelowAdmin.Should().BeFalse();
        operatorIsBelowTeamLeader.Should().BeTrue();
        supervisorIsAboveHR.Should().BeTrue();
    }

    [Fact]
    public void Role_EqualityChecks_Work()
    {
        // Assert
        Role.SuperAdmin.Should().Be(Role.SuperAdmin);
        Role.SuperAdmin.Should().NotBe(Role.Admin);
        Role.Operator.Should().Be(Role.Operator);
        Role.Operator.Should().NotBe(Role.HR);
    }

    [Fact]
    public void Role_ToString_ReturnsExpectedNames()
    {
        // Assert
        Role.SuperAdmin.ToString().Should().Be("SuperAdmin");
        Role.Admin.ToString().Should().Be("Admin");
        Role.Supervisor.ToString().Should().Be("Supervisor");
        Role.TeamLeader.ToString().Should().Be("TeamLeader");
        Role.HR.ToString().Should().Be("HR");
        Role.Operator.ToString().Should().Be("Operator");
    }

    [Fact]
    public void Role_ParseValidString_ReturnsCorrectEnum()
    {
        // Arrange & Act
        var parsed = (Role)Enum.Parse(typeof(Role), "SuperAdmin");
        
        // Assert
        parsed.Should().Be(Role.SuperAdmin);
    }

    [Fact]
    public void Role_ParseInvalidString_ThrowsException()
    {
        // Act & Assert
        FluentAssertions.AwaitExtensions.Assert.ThrowsAny<ArgumentException>(() =>
            Enum.Parse<Role>("NonExistentRole"));
    }

    [Fact]
    public void Role_GetValues_ReturnsAllSixRoles()
    {
        // Act
        var allRoles = Enum.GetValues<Role>().Length;

        // Assert
        allRoles.Should().Be(6);
    }

    [Fact]
    public void Role_AllValuesAreUnique()
    {
        // Act
        var values = Enum.GetValues<Role>()
            .Cast<Role>()
            .Select(r => (int)r)
            .Distinct()
            .Count();

        // Assert
        values.Should().Be(6);
    }

    #endregion

    #region Role Privilege Level Utility Methods

    /// <summary>
    /// Helper method used across multiple services to compare role privilege levels.
    /// This mirrors the pattern used in the actual codebase.
    /// </summary>
    private static bool IsRoleAtLeast(this Role role, Role requiredLevel)
    {
        return (int)role >= (int)requiredLevel;
    }

    [Fact]
    public void Role_IsAtLeast_SuperAdminPassesAll()
    {
        // Arrange
        var superAdmin = Role.SuperAdmin;

        // Assert
        superAdmin.IsAtLeast(Role.SuperAdmin).Should().BeTrue();
        superAdmin.IsAtLeast(Role.Admin).Should().BeTrue();
        superAdmin.IsAtLeast(Role.Supervisor).Should().BeTrue();
        superAdmin.IsAtLeast(Role.TeamLeader).Should().BeTrue();
        superAdmin.IsAtLeast(Role.HR).Should().BeTrue();
        superAdmin.IsAtLeast(Role.Operator).Should().BeTrue();
    }

    [Fact]
    public void Role_IsAtLeast_AdminPassesExceptSuperAdmin()
    {
        // Arrange
        var admin = Role.Admin;

        // Assert
        admin.IsAtLeast(Role.SuperAdmin).Should().BeFalse();
        admin.IsAtLeast(Role.Admin).Should().BeTrue();
        admin.IsAtLeast(Role.Supervisor).Should().BeTrue();
        admin.IsAtLeast(Role.TeamLeader).Should().BeTrue();
        admin.IsAtLeast(Role.HR).Should().BeTrue();
        admin.IsAtLeast(Role.Operator).Should().BeTrue();
    }

    [Fact]
    public void Role_IsAtLeast_SupervisorPassesBelowIt()
    {
        // Arrange
        var supervisor = Role.Supervisor;

        // Assert
        supervisor.IsAtLeast(Role.SuperAdmin).Should().BeFalse();
        supervisor.IsAtLeast(Role.Admin).Should().BeFalse();
        supervisor.IsAtLeast(Role.Supervisor).Should().BeTrue();
        supervisor.IsAtLeast(Role.TeamLeader).Should().BeTrue();
        supervisor.IsAtLeast(Role.HR).Should().BeTrue();
        supervisor.IsAtLeast(Role.Operator).Should().BeTrue();
    }

    [Fact]
    public void Role_IsAtLeast_OperatorFailsAllExceptSelf()
    {
        // Arrange
        var operatorRole = Role.Operator;

        // Assert
        operatorRole.IsAtLeast(Role.SuperAdmin).Should().BeFalse();
        operatorRole.IsAtLeast(Role.Admin).Should().BeFalse();
        operatorRole.IsAtLeast(Role.Supervisor).Should().BeFalse();
        operatorRole.IsAtLeast(Role.TeamLeader).Should().BeFalse();
        operatorRole.IsAtLeast(Role.HR).Should().BeFalse();
        operatorRole.IsAtLeast(Role.Operator).Should().BeTrue();
    }

    #endregion

    #region Role-Based Access Control Pattern Tests

    /// <summary>
    /// Simulates a typical RBAC check pattern: "does this user have sufficient privilege?"
    /// Mirrors patterns used throughout the WACAMS codebase.
    /// </summary>
    private static bool HasSufficientPrivilege(Role userRole, Role requiredRole)
    {
        return (int)userRole >= (int)requiredRole;
    }

    [Fact]
    public void RBAC_SuperAdminAccessToEverything_ReturnsTrue()
    {
        // Assert
        HasSufficientPrivilege(Role.SuperAdmin, Role.SuperAdmin).Should().BeTrue();
        HasSufficientPrivilege(Role.SuperAdmin, Role.Admin).Should().BeTrue();
        HasSufficientPrivilege(Role.SuperAdmin, Role.Supervisor).Should().BeTrue();
        HasSufficientPrivilege(Role.SuperAdmin, Role.TeamLeader).Should().BeTrue();
        HasSufficientPrivilege(Role.SuperAdmin, Role.HR).Should().BeTrue();
        HasSufficientPrivilege(Role.SuperAdmin, Role.Operator).Should().BeTrue();
    }

    [Fact]
    public void RBAC_AdminAccessToEverythingExceptSuperAdmin_ReturnsCorrect()
    {
        // Assert
        HasSufficientPrivilege(Role.Admin, Role.SuperAdmin).Should().BeFalse();
        HasSufficientPrivilege(Role.Admin, Role.Admin).Should().BeTrue();
        HasSufficientPrivilege(Role.Admin, Role.Supervisor).Should().BeTrue();
        HasSufficientPrivilege(Role.Admin, Role.TeamLeader).Should().BeTrue();
        HasSufficientPrivilege(Role.Admin, Role.HR).Should().BeTrue();
        HasSufficientPrivilege(Role.Admin, Role.Operator).Should().BeTrue();
    }

    [Fact]
    public void RBAC_SupervisorCantAccessAdminFeatures_ReturnsCorrect()
    {
        // Assert
        HasSufficientPrivilege(Role.Supervisor, Role.SuperAdmin).Should().BeFalse();
        HasSufficientPrivilege(Role.Supervisor, Role.Admin).Should().BeFalse();
        HasSufficientPrivilege(Role.Supervisor, Role.Supervisor).Should().BeTrue();
        HasSufficientPrivilege(Role.Supervisor, Role.TeamLeader).Should().BeTrue();
        HasSufficientPrivilege(Role.Supervisor, Role.HR).Should().BeTrue();
        HasSufficientPrivilege(Role.Supervisor, Role.Operator).Should().BeTrue();
    }

    [Fact]
    public void RBAC_OperatorCanOnlyAccessOwnLevelAndBelow_ReturnsCorrect()
    {
        // Assert
        HasSufficientPrivilege(Role.Operator, Role.SuperAdmin).Should().BeFalse();
        HasSufficientPrivilege(Role.Operator, Role.Admin).Should().BeFalse();
        HasSufficientPrivilege(Role.Operator, Role.Supervisor).Should().BeFalse();
        HasSufficientPrivilege(Role.Operator, Role.TeamLeader).Should().BeFalse();
        HasSufficientPrivilege(Role.Operator, Role.HR).Should().BeFalse();
        HasSufficientPrivilege(Role.Operator, Role.Operator).Should().BeTrue();
    }

    [Fact]
    public void RBAC_TransitivePrivilegeWorks()
    {
        // If user has role X, they implicitly have all permissions below X
        // Test transitivity: if SuperAdmin >= Admin and Admin >= Supervisor
        // then SuperAdmin >= Supervisor (transitive)
        
        var superAdmin = Role.SuperAdmin;
        var admin = Role.Admin;
        var supervisor = Role.Supervisor;

        // Direct checks
        ((int)superAdmin >= (int)admin).Should().BeTrue();
        ((int)admin >= (int)supervisor).Should().BeTrue();

        // Transitive check
        ((int)superAdmin >= (int)supervisor).Should().BeTrue();
    }

    [Fact]
    public void RBAC_MinimumRoleForFeatureTest()
    {
        // Define minimum roles for different features (mimics real codebase patterns)
        var featureMinimumRoles = new Dictionary<string, Role>
        {
            { "view_dashboard", Role.Operator },
            { "capture_production", Role.Operator },
            { "manage_team", Role.TeamLeader },
            { "approve_rework", Role.Supervisor },
            { "manage_users", Role.Admin },
            { "system_config", Role.SuperAdmin },
        };

        foreach (var kvp in featureMinimumRoles)
        {
            // Verify each feature has a valid minimum role defined
            kvp.Value.Should().BeInRange(Role.Operator, Role.SuperAdmin);
        }
    }

    #endregion
}
