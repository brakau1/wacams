using WACAMS.Core.RulesEngine.Rules;
using Xunit;

namespace WACAMS.Core.RulesEngine.Tests.Rules;

public class LateArrivalCalculationRuleTests
{
    private readonly LateArrivalCalculationRule _rule;

    public LateArrivalCalculationRuleTests()
    {
        _rule = new LateArrivalCalculationRule();
    }

    [Fact]
    public void CanCalculate_WithValidScheduledAndActualStart_ReturnsTrue()
    {
        // Arrange
        var context = new AttendanceCalculationContext
        {
            ScheduledStart = TimeSpan.FromHours(8),
            ActualStart = TimeSpan.FromHours(8).Add(TimeSpan.FromMinutes(15))
        };

        // Act
        var canCalculate = _rule.CanCalculate(context);

        // Assert
        Assert.True(canCalculate);
    }

    [Fact]
    public void CanCalculate_WithMissingScheduledStart_ReturnsFalse()
    {
        // Arrange
        var context = new AttendanceCalculationContext
        {
            ActualStart = TimeSpan.FromHours(8)
        };

        // Act
        var canCalculate = _rule.CanCalculate(context);

        // Assert
        Assert.False(canCalculate);
    }

    [Fact]
    public void CanCalculate_WithMissingActualStart_ReturnsFalse()
    {
        // Arrange
        var context = new AttendanceCalculationContext
        {
            ScheduledStart = TimeSpan.FromHours(8)
        };

        // Act
        var canCalculate = _rule.CanCalculate(context);

        // Assert
        Assert.False(canCalculate);
    }

    [Fact]
    public void Calculate_WhenEmployeeIsLate_ReturnsLateMinutes()
    {
        // Arrange
        var context = new AttendanceCalculationContext
        {
            EmployeeId = "EMP001",
            AttendanceDate = DateTime.Today,
            ShiftType = "Morning",
            ScheduledStart = TimeSpan.FromHours(8),
            ActualStart = TimeSpan.FromHours(8).Add(TimeSpan.FromMinutes(15)),
            LateGraceMinutes = 5
        };

        // Act
        var result = _rule.Calculate(context);

        // Assert
        Assert.True(result.IsLate);
        Assert.Equal(15, result.LateMinutes);
        Assert.True(result.HasException);
        Assert.Equal("LateArrival", result.ExceptionType);
    }

    [Fact]
    public void Calculate_WhenEmployeeIsLateWithinGracePeriod_ReturnsNotLate()
    {
        // Arrange
        var context = new AttendanceCalculationContext
        {
            EmployeeId = "EMP001",
            AttendanceDate = DateTime.Today,
            ShiftType = "Morning",
            ScheduledStart = TimeSpan.FromHours(8),
            ActualStart = TimeSpan.FromHours(8).Add(TimeSpan.FromMinutes(3)),
            LateGraceMinutes = 5
        };

        // Act
        var result = _rule.Calculate(context);

        // Assert
        Assert.False(result.IsLate);
        Assert.Equal(0, result.LateMinutes);
        Assert.False(result.HasException);
    }

    [Fact]
    public void Calculate_WhenEmployeeIsOnTime_ReturnsNotLate()
    {
        // Arrange
        var context = new AttendanceCalculationContext
        {
            EmployeeId = "EMP001",
            AttendanceDate = DateTime.Today,
            ShiftType = "Morning",
            ScheduledStart = TimeSpan.FromHours(8),
            ActualStart = TimeSpan.FromHours(8),
            LateGraceMinutes = 5
        };

        // Act
        var result = _rule.Calculate(context);

        // Assert
        Assert.False(result.IsLate);
        Assert.Equal(0, result.LateMinutes);
        Assert.False(result.HasException);
    }

    [Fact]
    public void Calculate_WhenEmployeeIsEarly_ReturnsNotLate()
    {
        // Arrange
        var context = new AttendanceCalculationContext
        {
            EmployeeId = "EMP001",
            AttendanceDate = DateTime.Today,
            ShiftType = "Morning",
            ScheduledStart = TimeSpan.FromHours(8),
            ActualStart = TimeSpan.FromHours(7).Add(TimeSpan.FromMinutes(45)),
            LateGraceMinutes = 5
        };

        // Act
        var result = _rule.Calculate(context);

        // Assert
        Assert.False(result.IsLate);
        Assert.Equal(0, result.LateMinutes);
        Assert.False(result.HasException);
    }

    [Fact]
    public void Calculate_WhenCannotCalculate_ReturnsEmptyResult()
    {
        // Arrange
        var context = new AttendanceCalculationContext
        {
            EmployeeId = "EMP001",
            AttendanceDate = DateTime.Today,
            ShiftType = "Morning"
        };

        // Act
        var result = _rule.Calculate(context);

        // Assert
        Assert.False(result.IsLate);
        Assert.Equal(0, result.LateMinutes);
        Assert.False(result.HasException);
    }
}
