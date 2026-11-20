using WACAMS.Core.Disciplinary.Models;
using WACAMS.Core.Disciplinary.Repositories;
using WACAMS.Core.Disciplinary.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace WACAMS.Core.Disciplinary.Tests;

/// <summary>
/// Unit tests for CorrectiveActionEngine
/// </summary>
public class CorrectiveActionEngineTests
{
    private readonly Mock<ICorrectiveActionRepository> _actionRepoMock;
    private readonly Mock<IOffenceCounterRepository> _counterRepoMock;
    private readonly Mock<IValidityPeriodRepository> _validityRepoMock;
    private readonly Mock<ILogger<CorrectiveActionEngine>> _loggerMock;
    private readonly CorrectiveActionEngine _engine;

    public CorrectiveActionEngineTests()
    {
        _actionRepoMock = new Mock<ICorrectiveActionRepository>();
        _counterRepoMock = new Mock<IOffenceCounterRepository>();
        _validityRepoMock = new Mock<IValidityPeriodRepository>();
        _loggerMock = new Mock<ILogger<CorrectiveActionEngine>>();

        _engine = new CorrectiveActionEngine(
            _actionRepoMock.Object,
            _counterRepoMock.Object,
            _validityRepoMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task DetermineActionAsync_FirstOffence_ReturnsCounselling()
    {
        // Arrange
        var employeeId = "EMP001";
        var exceptionId = Guid.NewGuid();
        var issuedBy = "MGR001";
        var reason = "Late arrival";

        _counterRepoMock.Setup(r => r.GetByEmployeeAndTypeAsync(employeeId, OffenceType.LateArrival))
            .ReturnsAsync((OffenceCounter?)null);

        _validityRepoMock.Setup(r => r.GetByLevelAsync(DisciplinaryLevel.Counselling))
            .ReturnsAsync(new ValidityPeriod { Level = DisciplinaryLevel.Counselling, ValidityDays = 90 });

        _actionRepoMock.Setup(r => r.CreateAsync(It.IsAny<CorrectiveAction>()))
            .ReturnsAsync((CorrectiveAction a) => a);

        _counterRepoMock.Setup(r => r.CreateAsync(It.IsAny<OffenceCounter>()))
            .ReturnsAsync((OffenceCounter c) => c);

        // Act
        var result = await _engine.DetermineActionAsync(employeeId, exceptionId, OffenceType.LateArrival, issuedBy, reason);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(DisciplinaryLevel.Counselling, result.DeterminedLevel);
        Assert.Equal(1, result.OffenceNumber);
        Assert.False(result.RequiresHearing);
    }

    [Fact]
    public async Task DetermineActionAsync_FifthOffence_ReturnsDismissalHearing()
    {
        // Arrange
        var employeeId = "EMP001";
        var exceptionId = Guid.NewGuid();
        var issuedBy = "MGR001";
        var reason = "Late arrival";

        _counterRepoMock.Setup(r => r.GetByEmployeeAndTypeAsync(employeeId, OffenceType.LateArrival))
            .ReturnsAsync(new OffenceCounter 
            { 
                EmployeeId = employeeId, 
                OffenceType = OffenceType.LateArrival, 
                ActiveOffences = 4 
            });

        _validityRepoMock.Setup(r => r.GetByLevelAsync(DisciplinaryLevel.DismissalHearing))
            .ReturnsAsync(new ValidityPeriod { Level = DisciplinaryLevel.DismissalHearing, ValidityDays = 365 });

        _actionRepoMock.Setup(r => r.CreateAsync(It.IsAny<CorrectiveAction>()))
            .ReturnsAsync((CorrectiveAction a) => a);

        _counterRepoMock.Setup(r => r.UpdateAsync(It.IsAny<OffenceCounter>()))
            .ReturnsAsync((OffenceCounter c) => c);

        // Act
        var result = await _engine.DetermineActionAsync(employeeId, exceptionId, OffenceType.LateArrival, issuedBy, reason);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(DisciplinaryLevel.DismissalHearing, result.DeterminedLevel);
        Assert.Equal(5, result.OffenceNumber);
        Assert.True(result.RequiresHearing);
    }

    [Fact]
    public async Task AcknowledgeActionAsync_ValidAction_ReturnsTrue()
    {
        // Arrange
        var actionId = Guid.NewGuid();
        var acknowledgedBy = "EMP001";
        var action = new CorrectiveAction
        {
            Id = actionId,
            EmployeeId = "EMP001",
            Status = CorrectiveActionStatus.Issued
        };

        _actionRepoMock.Setup(r => r.GetByIdAsync(actionId))
            .ReturnsAsync(action);

        _actionRepoMock.Setup(r => r.UpdateAsync(It.IsAny<CorrectiveAction>()))
            .ReturnsAsync((CorrectiveAction a) => a);

        // Act
        var result = await _engine.AcknowledgeActionAsync(actionId, acknowledgedBy);

        // Assert
        Assert.True(result);
        _actionRepoMock.Verify(r => r.UpdateAsync(It.Is<CorrectiveAction>(a => 
            a.Status == CorrectiveActionStatus.Acknowledged &&
            a.IsAcknowledged == true &&
            a.AcknowledgedBy == acknowledgedBy)), Times.Once);
    }

    [Fact]
    public async Task DisputeActionAsync_ValidAction_ReturnsTrue()
    {
        // Arrange
        var actionId = Guid.NewGuid();
        var disputedBy = "EMP001";
        var reason = "I was not late";
        var action = new CorrectiveAction
        {
            Id = actionId,
            EmployeeId = "EMP001",
            Status = CorrectiveActionStatus.Issued
        };

        _actionRepoMock.Setup(r => r.GetByIdAsync(actionId))
            .ReturnsAsync(action);

        _actionRepoMock.Setup(r => r.UpdateAsync(It.IsAny<CorrectiveAction>()))
            .ReturnsAsync((CorrectiveAction a) => a);

        // Act
        var result = await _engine.DisputeActionAsync(actionId, disputedBy, reason);

        // Assert
        Assert.True(result);
        _actionRepoMock.Verify(r => r.UpdateAsync(It.Is<CorrectiveAction>(a => 
            a.Status == CorrectiveActionStatus.Disputed &&
            a.ResolutionNotes == reason)), Times.Once);
    }

    [Fact]
    public async Task ScheduleHearingAsync_ActionRequiresHearing_ReturnsTrue()
    {
        // Arrange
        var actionId = Guid.NewGuid();
        var hearingDate = DateTime.UtcNow.AddDays(7);
        var scheduledBy = "MGR001";
        var action = new CorrectiveAction
        {
            Id = actionId,
            EmployeeId = "EMP001",
            RequiresHearing = true,
            Status = CorrectiveActionStatus.Issued
        };

        _actionRepoMock.Setup(r => r.GetByIdAsync(actionId))
            .ReturnsAsync(action);

        _actionRepoMock.Setup(r => r.UpdateAsync(It.IsAny<CorrectiveAction>()))
            .ReturnsAsync((CorrectiveAction a) => a);

        // Act
        var result = await _engine.ScheduleHearingAsync(actionId, hearingDate, scheduledBy);

        // Assert
        Assert.True(result);
        _actionRepoMock.Verify(r => r.UpdateAsync(It.Is<CorrectiveAction>(a => 
            a.Status == CorrectiveActionStatus.UnderHearing &&
            a.HearingScheduledAt == hearingDate)), Times.Once);
    }

    [Fact]
    public async Task ScheduleHearingAsync_ActionDoesNotRequireHearing_ReturnsFalse()
    {
        // Arrange
        var actionId = Guid.NewGuid();
        var hearingDate = DateTime.UtcNow.AddDays(7);
        var scheduledBy = "MGR001";
        var action = new CorrectiveAction
        {
            Id = actionId,
            EmployeeId = "EMP001",
            RequiresHearing = false,
            Status = CorrectiveActionStatus.Issued
        };

        _actionRepoMock.Setup(r => r.GetByIdAsync(actionId))
            .ReturnsAsync(action);

        // Act
        var result = await _engine.ScheduleHearingAsync(actionId, hearingDate, scheduledBy);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RecordHearingOutcome_Upheld_SetsStatusToUpheld()
    {
        // Arrange
        var actionId = Guid.NewGuid();
        var outcome = "Dismissal upheld";
        var recordedBy = "MGR001";
        var action = new CorrectiveAction
        {
            Id = actionId,
            EmployeeId = "EMP001",
            Status = CorrectiveActionStatus.UnderHearing
        };

        _actionRepoMock.Setup(r => r.GetByIdAsync(actionId))
            .ReturnsAsync(action);

        _actionRepoMock.Setup(r => r.UpdateAsync(It.IsAny<CorrectiveAction>()))
            .ReturnsAsync((CorrectiveAction a) => a);

        // Act
        var result = await _engine.RecordHearingOutcomeAsync(actionId, outcome, recordedBy);

        // Assert
        Assert.True(result);
        _actionRepoMock.Verify(r => r.UpdateAsync(It.Is<CorrectiveAction>(a => 
            a.Status == CorrectiveActionStatus.Upheld &&
            a.HearingOutcome == outcome)), Times.Once);
    }

    [Fact]
    public async Task RecordHearingOutcome_Overturned_DecrementsCounter()
    {
        // Arrange
        var actionId = Guid.NewGuid();
        var outcome = "Overturned";
        var recordedBy = "MGR001";
        var action = new CorrectiveAction
        {
            Id = actionId,
            EmployeeId = "EMP001",
            OffenceType = OffenceType.LateArrival,
            Status = CorrectiveActionStatus.UnderHearing
        };

        _actionRepoMock.Setup(r => r.GetByIdAsync(actionId))
            .ReturnsAsync(action);

        _actionRepoMock.Setup(r => r.UpdateAsync(It.IsAny<CorrectiveAction>()))
            .ReturnsAsync((CorrectiveAction a) => a);

        _counterRepoMock.Setup(r => r.GetByEmployeeAndTypeAsync("EMP001", OffenceType.LateArrival))
            .ReturnsAsync(new OffenceCounter 
            { 
                EmployeeId = "EMP001", 
                OffenceType = OffenceType.LateArrival, 
                ActiveOffences = 5 
            });

        _counterRepoMock.Setup(r => r.UpdateAsync(It.IsAny<OffenceCounter>()))
            .ReturnsAsync((OffenceCounter c) => c);

        // Act
        var result = await _engine.RecordHearingOutcomeAsync(actionId, outcome, recordedBy);

        // Assert
        Assert.True(result);
        _counterRepoMock.Verify(r => r.UpdateAsync(It.Is<OffenceCounter>(c => 
            c.ActiveOffences == 4)), Times.Once);
    }
}
