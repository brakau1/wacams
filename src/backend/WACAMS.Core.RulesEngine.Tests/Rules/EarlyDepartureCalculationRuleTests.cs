using WACAMS.Core.RulesEngine.Rules;
using Xunit;

namespace WACAMS.Core.RulesEngine.Tests.Rules;

public class EarlyDepartureCalculationRuleTests
{
    private readonly EarlyDepartureCalculationRule _rule;

    public EarlyDepartureCalculationRuleTests()
    {
        _rule = new EarlyDepartureCalculationRule();
    }

    [Fact]
    public void CanCalculate_WithValidScheduledAndActualEnd_ReturnsTrue()
    {
        // Arrange
        var context = new AttendanceCalculationContext
        {
            ScheduledEnd = TimeSpan.FromHours(17),
            ActualEnd = TimeSpan.FromHours(16).Add(TimeSpan.FromMinutes(30))
        };

        // Act
        var canCalculate = _rule.CanCalculate(context);

        // Assert
        Assert.True(canCalculate);
    }

    [Fact]
    public void Calculate_WhenEmployeeLeavesEarly_ReturnsEarlyMinutes()
    {
        // Arrange
        var context = new AttendanceCalculationContext
        {
            EmployeeId = "EMP001",
            AttendanceDate = DateTime.Today,
            ShiftType = "Morning",
            ScheduledEnd = TimeSpan.FromHours(17),
            ActualEnd = TimeSpan.FromHours(16),
            EarlyGraceMinutes = 5
        };

        // Act
        var result = _rule.Calculate(context);

        // Assert
        Assert.True(result.IsEarlyDeparture);
        Assert.Equal(60, result.EarlyMinutes);
        Assert.True(result.HasException);
        Assert.Equal("EarlyDeparture", result.ExceptionType);
    }

    [Fact]
    public void Calculate_WhenEmployeeLeavesEarlyWithinGracePeriod_ReturnsNotEarly()
    {
        // Arrange
        var context = new AttendanceCalculationContext
        {
            EmployeeId = "EMP001",
            AttendanceDate = DateTime.Today,
            ShiftType = "Morning",
            ScheduledEnd = TimeSpan.FromHours(17),
            ActualEnd = TimeSpan.FromHours(16).Add(TimeSpan.FromMinutes(57)),
            EarlyGraceMinutes = 5
        };

        // Act
        var result = _rule.Calculate(context);

        // Assert
        Assert.False(result.IsEarlyDeparture);
        Assert.Equal(0, result.EarlyMinutes);
        Assert.False(result.HasException);
    }

    [Fact]
    public void Calculate_WhenEmployeeStaysUntilScheduledEnd_ReturnsNotEarly()
    {
        // Arrange
        var context = new AttendanceCalculationContext
        {
            EmployeeId = "EMP001",
            AttendanceDate = DateTime.Today,
            ShiftType = "Morning",
            ScheduledEnd = TimeSpan.FromHours(17),
            ActualEnd = TimeSpan.FromHours(17),
            EarlyGraceMinutes = 5
        };

        // Act
        var result = _rule.Calculate(context);

        // Assert
        Assert.False(result.IsEarlyDeparture);
        Assert.Equal(0, result.EarlyMinutes);
        Assert.False(result.HasException);
    }

    [Fact]
    public void Calculate_WhenEmployeeStaysLate_ReturnsNotEarly()
    {
        // Arrange
        var context = new AttendanceCalculationContext
        {
            EmployeeId = "EMP001",
            AttendanceDate = DateTime.Today,
            ShiftType = "Morning",
            ScheduledEnd = TimeSpan.FromHours(17),
            ActualEnd = TimeSpan.FromHours(17).Add(TimeSpan.FromMinutes(30)),
            EarlyGraceMinutes = 5
        };

        // Act
        var result = _rule.Calculate(context);

        // Assert
        Assert.False(result.IsEarlyDeparture);
        Assert.Equal(0, result.EarlyMinutes);
        Assert.False(result.HasException);
    }
}
