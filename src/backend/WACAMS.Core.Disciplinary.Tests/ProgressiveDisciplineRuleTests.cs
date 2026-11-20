using WACAMS.Core.Disciplinary.Models;
using WACAMS.Core.Disciplinary.Rules;
using Xunit;

namespace WACAMS.Core.Disciplinary.Tests;

/// <summary>
/// Unit tests for ProgressiveDisciplineRule
/// </summary>
public class ProgressiveDisciplineRuleTests
{
    private readonly ProgressiveDisciplineRule _rule;

    public ProgressiveDisciplineRuleTests()
    {
        _rule = new ProgressiveDisciplineRule();
    }

    [Theory]
    [InlineData(1, DisciplinaryLevel.Counselling)]
    [InlineData(2, DisciplinaryLevel.VerbalWarning)]
    [InlineData(3, DisciplinaryLevel.WrittenWarning)]
    [InlineData(4, DisciplinaryLevel.FinalWrittenWarning)]
    [InlineData(5, DisciplinaryLevel.DismissalHearing)]
    [InlineData(6, DisciplinaryLevel.DismissalHearing)]
    public void DetermineLevel_LateArrival_ReturnsCorrectLevel(int offenceCount, DisciplinaryLevel expectedLevel)
    {
        // Act
        var level = _rule.DetermineLevel(OffenceType.LateArrival, offenceCount);

        // Assert
        Assert.Equal(expectedLevel, level);
    }

    [Theory]
    [InlineData(1, DisciplinaryLevel.Counselling)]
    [InlineData(2, DisciplinaryLevel.VerbalWarning)]
    [InlineData(3, DisciplinaryLevel.WrittenWarning)]
    [InlineData(4, DisciplinaryLevel.FinalWrittenWarning)]
    [InlineData(5, DisciplinaryLevel.DismissalHearing)]
    public void DetermineLevel_EarlyDeparture_ReturnsCorrectLevel(int offenceCount, DisciplinaryLevel expectedLevel)
    {
        // Act
        var level = _rule.DetermineLevel(OffenceType.EarlyDeparture, offenceCount);

        // Assert
        Assert.Equal(expectedLevel, level);
    }

    [Fact]
    public void DetermineLevel_InvalidCount_ReturnsNone()
    {
        // Act
        var level = _rule.DetermineLevel(OffenceType.LateArrival, 0);

        // Assert
        Assert.Equal(DisciplinaryLevel.None, level);
    }

    [Theory]
    [InlineData(DisciplinaryLevel.Counselling, false)]
    [InlineData(DisciplinaryLevel.VerbalWarning, false)]
    [InlineData(DisciplinaryLevel.WrittenWarning, false)]
    [InlineData(DisciplinaryLevel.FinalWrittenWarning, false)]
    [InlineData(DisciplinaryLevel.DismissalHearing, true)]
    [InlineData(DisciplinaryLevel.Dismissal, true)]
    public void RequiresHearing_ReturnsCorrectValue(DisciplinaryLevel level, bool expected)
    {
        // Act
        var requiresHearing = _rule.RequiresHearing(level);

        // Assert
        Assert.Equal(expected, requiresHearing);
    }

    [Theory]
    [InlineData(DisciplinaryLevel.Counselling, 1, "Counselling - 1st offence")]
    [InlineData(DisciplinaryLevel.VerbalWarning, 2, "Verbal Warning - 2nd offence")]
    [InlineData(DisciplinaryLevel.WrittenWarning, 3, "Written Warning - 3rd offence")]
    [InlineData(DisciplinaryLevel.FinalWrittenWarning, 4, "Final Written Warning - 4th offence")]
    [InlineData(DisciplinaryLevel.DismissalHearing, 5, "Dismissal Hearing - 5th offence")]
    public void GetDescription_ReturnsCorrectDescription(DisciplinaryLevel level, int offenceCount, string expected)
    {
        // Act
        var description = _rule.GetDescription(level, offenceCount);

        // Assert
        Assert.Equal(expected, description);
    }

    [Theory]
    [InlineData(1, "1st")]
    [InlineData(2, "2nd")]
    [InlineData(3, "3rd")]
    [InlineData(4, "4th")]
    [InlineData(11, "11th")]
    [InlineData(12, "12th")]
    [InlineData(13, "13th")]
    [InlineData(21, "21st")]
    [InlineData(22, "22nd")]
    [InlineData(23, "23rd")]
    public void GetOrdinal_ReturnsCorrectOrdinal(int number, string expected)
    {
        // Act - using reflection to test private method
        var method = typeof(ProgressiveDisciplineRule).GetMethod("GetOrdinal", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = method?.Invoke(_rule, new object[] { number });

        // Assert
        Assert.Equal(expected, result);
    }
}
