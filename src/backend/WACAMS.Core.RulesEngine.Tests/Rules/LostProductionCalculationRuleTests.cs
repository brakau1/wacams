using WACAMS.Core.RulesEngine.Rules;
using Xunit;

namespace WACAMS.Core.RulesEngine.Tests.Rules;

public class LostProductionCalculationRuleTests
{
    private readonly LostProductionCalculationRule _rule;

    public LostProductionCalculationRuleTests()
    {
        _rule = new LostProductionCalculationRule();
    }

    [Fact]
    public void Calculate_WhenEmployeeIsLate_CalculatesLostMinutes()
    {
        // Arrange
        var context = new AttendanceCalculationContext
        {
            EmployeeId = "EMP001",
            AttendanceDate = DateTime.Today,
            ShiftType = "Morning",
            ScheduledStart = TimeSpan.FromHours(8),
            ActualStart = TimeSpan.FromHours(8).Add(TimeSpan.FromMinutes(30)),
            ScheduledEnd = TimeSpan.FromHours(17),
            ActualEnd = TimeSpan.FromHours(17),
            LateGraceMinutes = 5,
            EarlyGraceMinutes = 5
        };

        // Act
        var result = _rule.Calculate(context);

        // Assert
        Assert.Equal(30, result.LostMinutes);
        Assert.True(result.IsLate);
        Assert.Equal(30, result.LateMinutes);
    }

    [Fact]
    public void Calculate_WhenEmployeeLeavesEarly_CalculatesLostMinutes()
    {
        // Arrange
        var context = new AttendanceCalculationContext
        {
            EmployeeId = "EMP001",
            AttendanceDate = DateTime.Today,
            ShiftType = "Morning",
            ScheduledStart = TimeSpan.FromHours(8),
            ActualStart = TimeSpan.FromHours(8),
            ScheduledEnd = TimeSpan.FromHours(17),
            ActualEnd = TimeSpan.FromHours(16),
            LateGraceMinutes = 5,
            EarlyGraceMinutes = 5
        };

        // Act
        var result = _rule.Calculate(context);

        // Assert
        Assert.Equal(60, result.LostMinutes);
        Assert.True(result.IsEarlyDeparture);
        Assert.Equal(60, result.EarlyMinutes);
    }

    [Fact]
    public void Calculate_WhenEmployeeIsLateAndLeavesEarly_CalculatesTotalLostMinutes()
    {
        // Arrange
        var context = new AttendanceCalculationContext
        {
            EmployeeId = "EMP001",
            AttendanceDate = DateTime.Today,
            ShiftType = "Morning",
            ScheduledStart = TimeSpan.FromHours(8),
            ActualStart = TimeSpan.FromHours(8).Add(TimeSpan.FromMinutes(30)),
            ScheduledEnd = TimeSpan.FromHours(17),
            ActualEnd = TimeSpan.FromHours(16),
            LateGraceMinutes = 5,
            EarlyGraceMinutes = 5
        };

        // Act
        var result = _rule.Calculate(context);

        // Assert
        Assert.Equal(90, result.LostMinutes); // 30 late + 60 early
        Assert.True(result.IsLate);
        Assert.True(result.IsEarlyDeparture);
    }

    [Fact]
    public void Calculate_WhenMissingClockIn_CalculatesFullShiftLostMinutes()
    {
        // Arrange
        var context = new AttendanceCalculationContext
        {
            EmployeeId = "EMP001",
            AttendanceDate = DateTime.Today,
            ShiftType = "Morning",
            ScheduledStart = TimeSpan.FromHours(8),
            ActualStart = null,
            ScheduledEnd = TimeSpan.FromHours(17),
            ActualEnd = null,
            LateGraceMinutes = 5,
            EarlyGraceMinutes = 5
        };

        // Act
        var result = _rule.Calculate(context);

        // Assert
        Assert.Equal(540, result.LostMinutes); // 9 hours * 60 minutes
        Assert.True(result.HasException);
        Assert.Equal("MissingClockIn", result.ExceptionType);
    }

    [Fact]
    public void Calculate_WhenMissingClockOut_CalculatesPartialShiftLostMinutes()
    {
        // Arrange
        var context = new AttendanceCalculationContext
        {
            EmployeeId = "EMP001",
            AttendanceDate = DateTime.Today,
            ShiftType = "Morning",
            ScheduledStart = TimeSpan.FromHours(8),
            ActualStart = TimeSpan.FromHours(8),
            ScheduledEnd = TimeSpan.FromHours(17),
            ActualEnd = null,
            LateGraceMinutes = 5,
            EarlyGraceMinutes = 5
        };

        // Act
        var result = _rule.Calculate(context);

        // Assert
        Assert.Equal(540, result.LostMinutes); // From 8:00 to 17:00 = 9 hours
        Assert.True(result.HasException);
        Assert.Equal("MissingClockOut", result.ExceptionType);
    }

    [Fact]
    public void Calculate_WhenNoExceptions_ReturnsZeroLostMinutes()
    {
        // Arrange
        var context = new AttendanceCalculationContext
        {
            EmployeeId = "EMP001",
            AttendanceDate = DateTime.Today,
            ShiftType = "Morning",
            ScheduledStart = TimeSpan.FromHours(8),
            ActualStart = TimeSpan.FromHours(8),
            ScheduledEnd = TimeSpan.FromHours(17),
            ActualEnd = TimeSpan.FromHours(17),
            LateGraceMinutes = 5,
            EarlyGraceMinutes = 5
        };

        // Act
        var result = _rule.Calculate(context);

        // Assert
        Assert.Equal(0, result.LostMinutes);
        Assert.False(result.HasException);
    }

    [Fact]
    public void LostHours_ConvertsMinutesToHours()
    {
        // Arrange
        var context = new AttendanceCalculationContext
        {
            EmployeeId = "EMP001",
            AttendanceDate = DateTime.Today,
            ShiftType = "Morning",
            ScheduledStart = TimeSpan.FromHours(8),
            ActualStart = TimeSpan.FromHours(8).Add(TimeSpan.FromMinutes(90)),
            ScheduledEnd = TimeSpan.FromHours(17),
            ActualEnd = TimeSpan.FromHours(17),
            LateGraceMinutes = 5,
            EarlyGraceMinutes = 5
        };

        // Act
        var result = _rule.Calculate(context);

        // Assert
        Assert.Equal(90, result.LostMinutes);
        Assert.Equal(1.5m, result.LostHours);
    }
}
