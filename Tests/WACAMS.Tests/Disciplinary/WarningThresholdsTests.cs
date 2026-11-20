using FluentAssertions;
using WACAMS.Core.Constants;
using WACAMS.Core.Enums;

namespace WACAMS.Tests.Disciplinary;

/// <summary>
/// Unit tests for WarningThresholds.GetWarningTypeForExceptionCount — pure static function
/// covering all threshold boundaries and edge cases.
/// </summary>
public class WarningThresholdsTests
{
    #region GetWarningTypeForExceptionCount Tests

    [Theory]
    [InlineData(0, null)]
    [InlineData(1, null)]
    [InlineData(2, null)]
    public void GetWarningTypeForExceptionCount_BelowThreshold_ReturnsNull(int exceptionCount, WarningType? expected)
    {
        // Act
        var result = WarningThresholds.GetWarningTypeForExceptionCount(exceptionCount);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetWarningTypeForExceptionCount_ThreeExceptions_ReturnsCounselling()
    {
        // Arrange & Act
        var result = WarningThresholds.GetWarningTypeForExceptionCount(3);

        // Assert
        result.Should().Be(WarningType.Counselling);
    }

    [Fact]
    public void GetWarningTypeForExceptionCount_FourExceptions_ReturnsCounselling()
    {
        // Arrange & Act
        var result = WarningThresholds.GetWarningTypeForExceptionCount(4);

        // Assert
        result.Should().Be(WarningType.Counselling);
    }

    [Fact]
    public void GetWarningTypeForExceptionCount_FiveExceptions_ReturnsVerbalWarning()
    {
        // Arrange & Act
        var result = WarningThresholds.GetWarningTypeForExceptionCount(5);

        // Assert
        result.Should().Be(WarningType.VerbalWarning);
    }

    [Fact]
    public void GetWarningTypeForExceptionCount_SixExceptions_ReturnsVerbalWarning()
    {
        // Arrange & Act
        var result = WarningThresholds.GetWarningTypeForExceptionCount(6);

        // Assert
        result.Should().Be(WarningType.VerbalWarning);
    }

    [Fact]
    public void GetWarningTypeForExceptionCount_SevenExceptions_ReturnsWrittenWarning()
    {
        // Arrange & Act
        var result = WarningThresholds.GetWarningTypeForExceptionCount(7);

        // Assert
        result.Should().Be(WarningType.WrittenWarning);
    }

    [Fact]
    public void GetWarningTypeForExceptionCount_EightExceptions_ReturnsWrittenWarning()
    {
        // Arrange & Act
        var result = WarningThresholds.GetWarningTypeForExceptionCount(8);

        // Assert
        result.Should().Be(WarningType.WrittenWarning);
    }

    [Fact]
    public void GetWarningTypeForExceptionCount_NineExceptions_ReturnsFinalWrittenWarning()
    {
        // Arrange & Act
        var result = WarningThresholds.GetWarningTypeForExceptionCount(9);

        // Assert
        result.Should().Be(WarningType.FinalWrittenWarning);
    }

    [Fact]
    public void GetWarningTypeForExceptionCount_TenExceptions_ReturnsFinalWrittenWarning()
    {
        // Arrange & Act
        var result = WarningThresholds.GetWarningTypeForExceptionCount(10);

        // Assert
        result.Should().Be(WarningType.FinalWrittenWarning);
    }

    [Fact]
    public void GetWarningTypeForExceptionCount_HighCount_ReturnsFinalWrittenWarning()
    {
        // Arrange & Act
        var result = WarningThresholds.GetWarningTypeForExceptionCount(50);

        // Assert
        result.Should().Be(WarningType.FinalWrittenWarning);
    }

    #endregion

    #region Boundary Condition Tests

    [Fact]
    public void GetWarningTypeForExceptionCount_BoundaryTwo_ReturnsNull()
    {
        // 2 is below Counselling threshold (3)
        var result = WarningThresholds.GetWarningTypeForExceptionCount(2);
        result.Should().BeNull();
    }

    [Fact]
    public void GetWarningTypeForExceptionCount_BoundaryThree_CounsellingThreshold()
    {
        // Exactly at Counselling threshold
        var result = WarningThresholds.GetWarningTypeForExceptionCount(3);
        result.Should().Be(WarningType.Counselling);
    }

    [Fact]
    public void GetWarningTypeForExceptionCount_BoundaryFour_CounsellingNotVerbal()
    {
        // 4 is above Counselling (3) but below Verbal (5)
        var result = WarningThresholds.GetWarningTypeForExceptionCount(4);
        result.Should().Be(WarningType.Counselling);
        result.Should().NotBe(WarningType.VerbalWarning);
    }

    [Fact]
    public void GetWarningTypeForExceptionCount_BoundaryFive_VerbalThreshold()
    {
        // Exactly at Verbal threshold
        var result = WarningThresholds.GetWarningTypeForExceptionCount(5);
        result.Should().Be(WarningType.VerbalWarning);
    }

    [Fact]
    public void GetWarningTypeForExceptionCount_BoundarySix_VerbalNotWritten()
    {
        // 6 is above Verbal (5) but below Written (7)
        var result = WarningThresholds.GetWarningTypeForExceptionCount(6);
        result.Should().Be(WarningType.VerbalWarning);
        result.Should().NotBe(WarningType.WrittenWarning);
    }

    [Fact]
    public void GetWarningTypeForExceptionCount_BoundarySeven_WrittenThreshold()
    {
        // Exactly at Written threshold
        var result = WarningThresholds.GetWarningTypeForExceptionCount(7);
        result.Should().Be(WarningType.WrittenWarning);
    }

    [Fact]
    public void GetWarningTypeForExceptionCount_BoundaryEight_WrittenNotFinal()
    {
        // 8 is above Written (7) but below Final (9)
        var result = WarningThresholds.GetWarningTypeForExceptionCount(8);
        result.Should().Be(WarningType.WrittenWarning);
        result.Should().NotBe(WarningType.FinalWrittenWarning);
    }

    [Fact]
    public void GetWarningTypeForExceptionCount_BoundaryNine_FinalThreshold()
    {
        // Exactly at Final Written threshold
        var result = WarningThresholds.GetWarningTypeForExceptionCount(9);
        result.Should().Be(WarningType.FinalWrittenWarning);
    }

    #endregion

    #region Threshold Constant Values Tests

    [Fact]
    public void CounsellingExceptionCount_IsThree()
    {
        WarningThresholds.CounsellingExceptionCount.Should().Be(3);
    }

    [Fact]
    public void VerbalWarningExceptionCount_IsFive()
    {
        WarningThresholds.VerbalWarningExceptionCount.Should().Be(5);
    }

    [Fact]
    public void WrittenWarningExceptionCount_IsSeven()
    {
        WarningThresholds.WrittenWarningExceptionCount.Should().Be(7);
    }

    [Fact]
    public void FinalWrittenWarningExceptionCount_IsNine()
    {
        WarningThresholds.FinalWrittenWarningExceptionCount.Should().Be(9);
    }

    [Fact]
    public void ActiveWarningValidityDays_IsNinety()
    {
        WarningThresholds.ActiveWarningValidityDays.Should().Be(90);
    }

    [Fact]
    public void EscalationActionWindowHours_IsTwentyFour()
    {
        WarningThresholds.EscalationActionWindowHours.Should().Be(24);
    }

    [Fact]
    public void MaxEscalationLevel_IsThree()
    {
        WarningThresholds.MaxEscalationLevel.Should().Be(3);
    }

    #endregion

    #region Progressive Escalation Logic Tests

    [Fact]
    public void GetWarningTypeForExceptionCount_DemonstratesProgressiveLadder()
    {
        // Verify the full progressive ladder: 0→null, 3→Counselling, 5→Verbal, 7→Written, 9→Final
        var results = new Dictionary<int, WarningType?>
        {
            { 0, null },
            { 1, null },
            { 2, null },
            { 3, WarningType.Counselling },
            { 4, WarningType.Counselling },
            { 5, WarningType.VerbalWarning },
            { 6, WarningType.VerbalWarning },
            { 7, WarningType.WrittenWarning },
            { 8, WarningType.WrittenWarning },
            { 9, WarningType.FinalWrittenWarning },
            { 10, WarningType.FinalWrittenWarning },
        };

        foreach (var kvp in results)
        {
            var result = WarningThresholds.GetWarningTypeForExceptionCount(kvp.Key);
            result.Should().Be(kvp.Value, $"Expected {kvp.Value?.ToString() ?? "null"} for {kvp.Key} exceptions");
        }
    }

    [Fact]
    public void GetWarningTypeForExceptionCount_OnlyHighestThresholdMatches()
    {
        // The method should return ONLY the highest matching level, not accumulate lower ones
        var result = WarningThresholds.GetWarningTypeForExceptionCount(9);
        
        // Should be FinalWrittenWarning only, not Counselling + Verbal + Written + Final
        result.Should().Be(WarningType.FinalWrittenWarning);
    }

    #endregion
}
