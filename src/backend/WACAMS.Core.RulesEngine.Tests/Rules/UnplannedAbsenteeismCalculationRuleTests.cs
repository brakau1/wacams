using WACAMS.Core.RulesEngine.Rules;
using Xunit;

namespace WACAMS.Core.RulesEngine.Tests.Rules;

public class UnplannedAbsenteeismCalculationRuleTests
{
    private readonly UnplannedAbsenteeismCalculationRule _rule;

    public UnplannedAbsenteeismCalculationRuleTests()
    {
        _rule = new UnplannedAbsenteeismCalculationRule();
    }

    [Fact]
    public void Calculate_WhenEmployeeIsAbsent_ReturnsFullShiftLostMinutes()
    {
        // Arrange
        var context = new AttendanceCalculationContext
        {
            EmployeeId = "EMP001",
            AttendanceDate = DateTime.Today,
            ShiftType = "Morning",
            ScheduledStart = TimeSpan.FromHours(8),
            ScheduledEnd = TimeSpan.FromHours(17),
            ActualStart = null,
            ActualEnd = null
        };

        // Act
        var result = _rule.Calculate(context);

        // Assert
        Assert.True(result.IsAbsent);
        Assert.Equal(540, result.LostMinutes); // 9 hours
        Assert.True(result.HasException);
        Assert.Equal("UnplannedAbsenteeism", result.ExceptionType);
    }

    [Fact]
    public void Calculate_WhenEmployeeIsOnLeave_ReturnsNotAbsent()
    {
        // Arrange
        var context = new AttendanceCalculationContext
        {
            EmployeeId = "EMP001",
            AttendanceDate = DateTime.Today,
            ShiftType = "Morning",
            ScheduledStart = TimeSpan.FromHours(8),
            ScheduledEnd = TimeSpan.FromHours(17),
            ActualStart = null,
            ActualEnd = null,
            IsOnLeave = true,
            LeaveType = "Annual"
        };

        // Act
        var result = _rule.Calculate(context);

        // Assert
        Assert.False(result.IsAbsent);
        Assert.Equal(0, result.LostMinutes);
        Assert.False(result.HasException);
    }

    [Fact]
    public void Calculate_WhenShiftIsCancelled_ReturnsNotAbsent()
    {
        // Arrange
        var context = new AttendanceCalculationContext
        {
            EmployeeId = "EMP001",
            AttendanceDate = DateTime.Today,
            ShiftType = "Morning",
            ScheduledStart = TimeSpan.FromHours(8),
            ScheduledEnd = TimeSpan.FromHours(17),
            ActualStart = null,
            ActualEnd = null,
            IsShiftCancelled = true
        };

        // Act
        var result = _rule.Calculate(context);

        // Assert
        Assert.False(result.IsAbsent);
        Assert.Equal(0, result.LostMinutes);
        Assert.False(result.HasException);
    }

    [Fact]
    public void Calculate_WhenEmployeeIsPresent_ReturnsNotAbsent()
    {
        // Arrange
        var context = new AttendanceCalculationContext
        {
            EmployeeId = "EMP001",
            AttendanceDate = DateTime.Today,
            ShiftType = "Morning",
            ScheduledStart = TimeSpan.FromHours(8),
            ScheduledEnd = TimeSpan.FromHours(17),
            ActualStart = TimeSpan.FromHours(8),
            ActualEnd = TimeSpan.FromHours(17)
        };

        // Act
        var result = _rule.Calculate(context);

        // Assert
        Assert.False(result.IsAbsent);
        Assert.Equal(0, result.LostMinutes);
        Assert.False(result.HasException);
    }
}
