using FluentAssertions;
using Moq;
using WACAMS.Core.Constants;
using WACAMS.Core.Entities;
using WACAMS.Core.Enums;
using WACAMS.Core.Interfaces;
using WACAMS.Infrastructure.Services;

namespace WACAMS.Tests.Escalation;

/// <summary>
/// Unit tests for EscalationService covering escalation creation, state transitions,
/// chain building, max level enforcement, and 24-hour timeout logic.
/// </summary>
public class EscalationServiceTests
{
    private readonly Mock<WacamContext> _mockContext;
    private readonly Mock<IEmployeeRepository> _mockEmployeeRepository;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly EscalationService _service;

    public EscalationServiceTests()
    {
        _mockContext = new Mock<WacamContext>();
        _mockEmployeeRepository = new Mock<IEmployeeRepository>();
        _mockAuditLogService = new Mock<IAuditLogService>();
        _service = new EscalationService(
            _mockContext.Object,
            _mockEmployeeRepository.Object,
            _mockAuditLogService.Object);
    }

    #region CreateEscalation Tests

    [Fact]
    public async Task CreateEscalation_ValidLevel_CreatesSuccessfully()
    {
        // Arrange
        var warningId = Guid.NewGuid();
        var fromManager = "EMP002";
        var toManager = "EMP009";
        var level = 1;
        var actionRequired = "Review disciplinary case with employee.";
        var performedBy = "SYSTEM";

        var savedEscalations = new List<Escalation>();
        var mockSet = new Mock<Microsoft.EntityFrameworkCore.DbSet<Escalation>>();
        
        mockSet.As<IQueryable<Escalation>>().Setup(m => m.Provider).Returns(savedEscalations.AsQueryable().Provider);
        mockSet.As<IQueryable<Escalation>>().Setup(m => m.Expression).Returns(savedEscalations.AsQueryable().Expression);
        mockSet.As<IQueryable<Escalation>>().Setup(m => m.ElementType).Returns(typeof(Escalation));
        mockSet.As<IQueryable<Escalation>>().Setup(m => m.GetEnumerator()).Returns(savedEscalations.GetEnumerator());
        mockSet.Setup(m => m.AddAsync(It.IsAny<Escalation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Escalation e, CancellationToken _) => { savedEscalations.Add(e); return e; });
        mockSet.Setup(m => m.AddRange(It.IsAny<IEnumerable<Escalation>>()))
            .Callback<IEnumerable<Escalation>>(list => savedEscalations.AddRange(list));

        _mockContext.Setup(c => c.Escalations).Returns(mockSet.Object);

        // Act
        var result = await _service.CreateEscalation(warningId, fromManager, toManager, level, actionRequired, performedBy);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.WarningLetterId.Should().Be(warningId);
        result.FromManagerId.Should().Be(fromManager);
        result.ToManagerId.Should().Be(toManager);
        result.Level.Should().Be(level);
        result.Status.Should().Be(EscalationStatus.Pending);
        result.ActionRequired.Should().Be(actionRequired);
        result.CompletedAt.Should().BeNull();

        _mockAuditLogService.Verify(a => a.LogChangeAsync(
            "Escalation",
            result.Id.ToString(),
            "Create",
            null,
            $"Level {level}: {fromManager} → {toManager}",
            performedBy,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateEscalation_LevelTwo_CreatesWithCorrectLevel()
    {
        // Arrange
        var warningId = Guid.NewGuid();
        var level = 2;
        var savedEscalations = new List<Escalation>();
        var mockSet = new Mock<Microsoft.EntityFrameworkCore.DbSet<Escalation>>();
        
        mockSet.As<IQueryable<Escalation>>().Setup(m => m.Provider).Returns(savedEscalations.AsQueryable().Provider);
        mockSet.As<IQueryable<Escalation>>().Setup(m => m.Expression).Returns(savedEscalations.AsQueryable().Expression);
        mockSet.As<IQueryable<Escalation>>().Setup(m => m.ElementType).Returns(typeof(Escalation));
        mockSet.As<IQueryable<Escalation>>().Setup(m => m.GetEnumerator()).Returns(savedEscalations.GetEnumerator());
        mockSet.Setup(m => m.AddAsync(It.IsAny<Escalation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Escalation e, CancellationToken _) => { savedEscalations.Add(e); return e; });

        _mockContext.Setup(c => c.Escalations).Returns(mockSet.Object);

        // Act
        var result = await _service.CreateEscalation(
            warningId, "EMP002", "EMP009", level, "Review case.", "SYSTEM");

        // Assert
        result.Level.Should().Be(2);
        result.Status.Should().Be(EscalationStatus.Pending);
    }

    [Fact]
    public async Task CreateEscalation_MaxLevel_CreatesSuccessfully()
    {
        // Arrange — MaxEscalationLevel is 3
        var warningId = Guid.NewGuid();
        var level = WarningThresholds.MaxEscalationLevel; // 3

        var savedEscalations = new List<Escalation>();
        var mockSet = new Mock<Microsoft.EntityFrameworkCore.DbSet<Escalation>>();
        
        mockSet.As<IQueryable<Escalation>>().Setup(m => m.Provider).Returns(savedEscalations.AsQueryable().Provider);
        mockSet.As<IQueryable<Escalation>>().Setup(m => m.Expression).Returns(savedEscalations.AsQueryable().Expression);
        mockSet.As<IQueryable<Escalation>>().Setup(m => m.ElementType).Returns(typeof(Escalation));
        mockSet.As<IQueryable<Escalation>>().Setup(m => m.GetEnumerator()).Returns(savedEscalations.GetEnumerator());
        mockSet.Setup(m => m.AddAsync(It.IsAny<Escalation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Escalation e, CancellationToken _) => { savedEscalations.Add(e); return e; });

        _mockContext.Setup(c => c.Escalations).Returns(mockSet.Object);

        // Act
        var result = await _service.CreateEscalation(
            warningId, "EMP009", "EMP043", level, "Final escalation.", "SYSTEM");

        // Assert
        result.Should().NotBeNull();
        result.Level.Should().Be(3);
        result.Status.Should().Be(EscalationStatus.Pending);
    }

    [Fact]
    public async Task CreateEscalation_ExceedsMaxLevel_ThrowsInvalidOperationException()
    {
        // Arrange — level 4 exceeds MaxEscalationLevel of 3
        var warningId = Guid.NewGuid();
        var level = WarningThresholds.MaxEscalationLevel + 1; // 4

        // Act & Assert
        await FluentAssertions.AwaitExtensions.Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.CreateEscalation(
                warningId, "EMP002", "EMP009", level, "Should fail.", "SYSTEM"));
    }

    [Fact]
    public async Task CreateEscalation_EmptyFromManagerId_CreatesSuccessfully()
    {
        // Arrange
        var warningId = Guid.NewGuid();
        var savedEscalations = new List<Escalation>();
        var mockSet = new Mock<Microsoft.EntityFrameworkCore.DbSet<Escalation>>();
        
        mockSet.As<IQueryable<Escalation>>().Setup(m => m.Provider).Returns(savedEscalations.AsQueryable().Provider);
        mockSet.As<IQueryable<Escalation>>().Setup(m => m.Expression).Returns(savedEscalations.AsQueryable().Expression);
        mockSet.As<IQueryable<Escalation>>().Setup(m => m.ElementType).Returns(typeof(Escalation));
        mockSet.As<IQueryable<Escalation>>().Setup(m => m.GetEnumerator()).Returns(savedEscalations.GetEnumerator());
        mockSet.Setup(m => m.AddAsync(It.IsAny<Escalation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Escalation e, CancellationToken _) => { savedEscalations.Add(e); return e; });

        _mockContext.Setup(c => c.Escalations).Returns(mockSet.Object);

        // Act
        var result = await _service.CreateEscalation(
            warningId, string.Empty, "EMP009", 1, "Test.", "SYSTEM");

        // Assert
        result.FromManagerId.Should().Be(string.Empty);
    }

    [Fact]
    public async Task CreateEscalation_SetsCompletedAtToNull()
    {
        // Arrange
        var warningId = Guid.NewGuid();
        var savedEscalations = new List<Escalation>();
        var mockSet = new Mock<Microsoft.EntityFrameworkCore.DbSet<Escalation>>();
        
        mockSet.As<IQueryable<Escalation>>().Setup(m => m.Provider).Returns(savedEscalations.AsQueryable().Provider);
        mockSet.As<IQueryable<Escalation>>().Setup(m => m.Expression).Returns(savedEscalations.AsQueryable().Expression);
        mockSet.As<IQueryable<Escalation>>().Setup(m => m.ElementType).Returns(typeof(Escalation));
        mockSet.As<IQueryable<Escalation>>().Setup(m => m.GetEnumerator()).Returns(savedEscalations.GetEnumerator());
        mockSet.Setup(m => m.AddAsync(It.IsAny<Escalation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Escalation e, CancellationToken _) => { savedEscalations.Add(e); return e; });

        _mockContext.Setup(c => c.Escalations).Returns(mockSet.Object);

        // Act
        var result = await _service.CreateEscalation(
            warningId, "EMP002", "EMP009", 1, "Action required.", "SYSTEM");

        // Assert
        result.CompletedAt.Should().BeNull();
    }

    #endregion

    #region ProgressEscalation Tests

    [Fact]
    public async Task ProgressEscalation_PendingToAccepted_TransitionsSuccessfully()
    {
        // Arrange
        var escalationId = Guid.NewGuid();
        var existingEscalation = new Escalation
        {
            Id = escalationId,
            Status = EscalationStatus.Pending,
            Level = 1,
            FromManagerId = "EMP002",
            ToManagerId = "EMP009",
            ActionRequired = "Review case."
        };

        var mockSet = SetupFindableSet(existingEscalation);
        _mockContext.Setup(c => c.Escalations).Returns(mockSet.Object);

        // Act
        var result = await _service.ProgressEscalation(escalationId, EscalationStatus.Accepted, "EMP009");

        // Assert
        result.Status.Should().Be(EscalationStatus.Accepted);
        result.CompletedAt.Should().BeNull();
    }

    [Fact]
    public async Task ProgressEscalation_AcceptedToActionTaken_TransitionsSuccessfully()
    {
        // Arrange
        var escalationId = Guid.NewGuid();
        var existingEscalation = new Escalation
        {
            Id = escalationId,
            Status = EscalationStatus.Accepted,
            Level = 1,
            FromManagerId = "EMP002",
            ToManagerId = "EMP009",
            ActionRequired = "Review case."
        };

        var mockSet = SetupFindableSet(existingEscalation);
        _mockContext.Setup(c => c.Escalations).Returns(mockSet.Object);

        // Act
        var result = await _service.ProgressEscalation(escalationId, EscalationStatus.ActionTaken, "EMP009");

        // Assert
        result.Status.Should().Be(EscalationStatus.ActionTaken);
        result.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ProgressEscalation_ActionTakenToClosed_TransitionsSuccessfully()
    {
        // Arrange
        var escalationId = Guid.NewGuid();
        var existingEscalation = new Escalation
        {
            Id = escalationId,
            Status = EscalationStatus.ActionTaken,
            Level = 1,
            FromManagerId = "EMP002",
            ToManagerId = "EMP009",
            ActionRequired = "Review case.",
            CompletedAt = DateTime.UtcNow
        };

        var mockSet = SetupFindableSet(existingEscalation);
        _mockContext.Setup(c => c.Escalations).Returns(mockSet.Object);

        // Act
        var result = await _service.ProgressEscalation(escalationId, EscalationStatus.Closed, "EMP009");

        // Assert
        result.Status.Should().Be(EscalationStatus.Closed);
    }

    [Fact]
    public async Task ProgressEscalation_PendingToEscalated_TransitionsSuccessfully()
    {
        // Arrange
        var escalationId = Guid.NewGuid();
        var existingEscalation = new Escalation
        {
            Id = escalationId,
            Status = EscalationStatus.Pending,
            Level = 1,
            FromManagerId = "EMP002",
            ToManagerId = "EMP009",
            ActionRequired = "Review case."
        };

        var mockSet = SetupFindableSet(existingEscalation);
        _mockContext.Setup(c => c.Escalations).Returns(mockSet.Object);

        // Act
        var result = await _service.ProgressEscalation(escalationId, EscalationStatus.Escalated, "EMP009");

        // Assert
        result.Status.Should().Be(EscalationStatus.Escalated);
    }

    [Fact]
    public async Task ProgressEscalation_AcceptedToEscalated_TransitionsSuccessfully()
    {
        // Arrange
        var escalationId = Guid.NewGuid();
        var existingEscalation = new Escalation
        {
            Id = escalationId,
            Status = EscalationStatus.Accepted,
            Level = 1,
            FromManagerId = "EMP002",
            ToManagerId = "EMP009",
            ActionRequired = "Review case."
        };

        var mockSet = SetupFindableSet(existingEscalation);
        _mockContext.Setup(c => c.Escalations).Returns(mockSet.Object);

        // Act
        var result = await _service.ProgressEscalation(escalationId, EscalationStatus.Escalated, "EMP009");

        // Assert
        result.Status.Should().Be(EscalationStatus.Escalated);
    }

    #endregion

    #region Invalid Transition Tests

    [Fact]
    public async Task ProgressEscalation_PendingToClosed_ThrowsInvalidOperationException()
    {
        // Arrange
        var escalationId = Guid.NewGuid();
        var existingEscalation = new Escalation
        {
            Id = escalationId,
            Status = EscalationStatus.Pending,
            Level = 1,
            FromManagerId = "EMP002",
            ToManagerId = "EMP009"
        };

        var mockSet = SetupFindableSet(existingEscalation);
        _mockContext.Setup(c => c.Escalations).Returns(mockSet.Object);

        // Act & Assert
        await FluentAssertions.AwaitExtensions.Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.ProgressEscalation(escalationId, EscalationStatus.Closed, "EMP009"));
    }

    [Fact]
    public async Task ProgressEscalation_PendingToActionTaken_ThrowsInvalidOperationException()
    {
        // Arrange
        var escalationId = Guid.NewGuid();
        var existingEscalation = new Escalation
        {
            Id = escalationId,
            Status = EscalationStatus.Pending,
            Level = 1,
            FromManagerId = "EMP002",
            ToManagerId = "EMP009"
        };

        var mockSet = SetupFindableSet(existingEscalation);
        _mockContext.Setup(c => c.Escalations).Returns(mockSet.Object);

        // Act & Assert
        await FluentAssertions.AwaitExtensions.Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.ProgressEscalation(escalationId, EscalationStatus.ActionTaken, "EMP009"));
    }

    [Fact]
    public async Task ProgressEscalation_AcceptedToClosed_ThrowsInvalidOperationException()
    {
        // Arrange
        var escalationId = Guid.NewGuid();
        var existingEscalation = new Escalation
        {
            Id = escalationId,
            Status = EscalationStatus.Accepted,
            Level = 1,
            FromManagerId = "EMP002",
            ToManagerId = "EMP009"
        };

        var mockSet = SetupFindableSet(existingEscalation);
        _mockContext.Setup(c => c.Escalations).Returns(mockSet.Object);

        // Act & Assert
        await FluentAssertions.AwaitExtensions.Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.ProgressEscalation(escalationId, EscalationStatus.Closed, "EMP009"));
    }

    [Fact]
    public async Task ProgressEscalation_AcceptedToPending_ThrowsInvalidOperationException()
    {
        // Arrange
        var escalationId = Guid.NewGuid();
        var existingEscalation = new Escalation
        {
            Id = escalationId,
            Status = EscalationStatus.Accepted,
            Level = 1,
            FromManagerId = "EMP002",
            ToManagerId = "EMP009"
        };

        var mockSet = SetupFindableSet(existingEscalation);
        _mockContext.Setup(c => c.Escalations).Returns(mockSet.Object);

        // Act & Assert
        await FluentAssertions.AwaitExtensions.Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.ProgressEscalation(escalationId, EscalationStatus.Pending, "EMP009"));
    }

    [Fact]
    public async Task ProgressEscalation_ActionTakenToPending_ThrowsInvalidOperationException()
    {
        // Arrange
        var escalationId = Guid.NewGuid();
        var existingEscalation = new Escalation
        {
            Id = escalationId,
            Status = EscalationStatus.ActionTaken,
            Level = 1,
            FromManagerId = "EMP002",
            ToManagerId = "EMP009"
        };

        var mockSet = SetupFindableSet(existingEscalation);
        _mockContext.Setup(c => c.Escalations).Returns(mockSet.Object);

        // Act & Assert
        await FluentAssertions.AwaitExtensions.Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.ProgressEscalation(escalationId, EscalationStatus.Pending, "EMP009"));
    }

    [Fact]
    public async Task ProgressEscalation_ActionTakenToAccepted_ThrowsInvalidOperationException()
    {
        // Arrange
        var escalationId = Guid.NewGuid();
        var existingEscalation = new Escalation
        {
            Id = escalationId,
            Status = EscalationStatus.ActionTaken,
            Level = 1,
            FromManagerId = "EMP002",
            ToManagerId = "EMP009"
        };

        var mockSet = SetupFindableSet(existingEscalation);
        _mockContext.Setup(c => c.Escalations).Returns(mockSet.Object);

        // Act & Assert
        await FluentAssertions.AwaitExtensions.Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.ProgressEscalation(escalationId, EscalationStatus.Accepted, "EMP009"));
    }

    [Fact]
    public async Task ProgressEscalation_EscalatedToAny_ThrowsInvalidOperationException()
    {
        // Arrange
        var escalationId = Guid.NewGuid();
        var existingEscalation = new Escalation
        {
            Id = escalationId,
            Status = EscalationStatus.Escalated,
            Level = 2,
            FromManagerId = "EMP002",
            ToManagerId = "EMP009"
        };

        var mockSet = SetupFindableSet(existingEscalation);
        _mockContext.Setup(c => c.Escalations).Returns(mockSet.Object);

        // Act & Assert — any transition from Escalated should throw
        foreach (var target in new[]
        {
            EscalationStatus.Pending,
            EscalationStatus.Accepted,
            EscalationStatus.ActionTaken,
            EscalationStatus.Escalated,
            EscalationStatus.Closed
        })
        {
            await FluentAssertions.AwaitExtensions.Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _service.ProgressEscalation(escalationId, target, "EMP009"));
        }
    }

    [Fact]
    public async Task ProgressEscalation_ClosedToAny_ThrowsInvalidOperationException()
    {
        // Arrange
        var escalationId = Guid.NewGuid();
        var existingEscalation = new Escalation
        {
            Id = escalationId,
            Status = EscalationStatus.Closed,
            Level = 3,
            FromManagerId = "EMP002",
            ToManagerId = "EMP043"
        };

        var mockSet = SetupFindableSet(existingEscalation);
        _mockContext.Setup(c => c.Escalations).Returns(mockSet.Object);

        // Act & Assert
        await FluentAssertions.AwaitExtensions.Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.ProgressEscalation(escalationId, EscalationStatus.Pending, "EMP009"));
    }

    [Fact]
    public async Task ProgressEscalation_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var mockSet = new Mock<Microsoft.EntityFrameworkCore.DbSet<Escalation>>();
        _mockContext.Setup(c => c.Escalations).Returns(mockSet.Object);

        // Act & Assert
        await FluentAssertions.AwaitExtensions.Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await _service.ProgressEscalation(nonExistentId, EscalationStatus.Accepted, "EMP009"));
    }

    #endregion

    #region GetEscalationChain Tests

    [Fact]
    public async Task GetEscalationChain_BuildsCompleteHierarchy()
    {
        // Arrange — EMP005 → EMP003 → EMP002 → EMP001 → null
        _mockEmployeeRepository.Setup(r => r.GetByEmployeeIdAsync("EMP005", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Core.Entities.Employee { EmployeeId = "EMP005", ManagerId = "EMP003" });
        _mockEmployeeRepository.Setup(r => r.GetByEmployeeIdAsync("EMP003", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Core.Entities.Employee { EmployeeId = "EMP003", ManagerId = "EMP002" });
        _mockEmployeeRepository.Setup(r => r.GetByEmployeeIdAsync("EMP002", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Core.Entities.Employee { EmployeeId = "EMP002", ManagerId = "EMP001" });
        _mockEmployeeRepository.Setup(r => r.GetByEmployeeIdAsync("EMP001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Core.Entities.Employee { EmployeeId = "EMP001", ManagerId = null! });

        // Act
        var chain = await _service.GetEscalationChain("EMP005");

        // Assert
        chain.Should().HaveCount(3);
        chain[0].Should().Be("EMP003");
        chain[1].Should().Be("EMP002");
        chain[2].Should().Be("EMP001");
    }

    [Fact]
    public async Task GetEscalationChain_NoManager_ReturnsEmpty()
    {
        // Arrange — top-level manager has no ManagerId
        _mockEmployeeRepository.Setup(r => r.GetByEmployeeIdAsync("EMP001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Core.Entities.Employee { EmployeeId = "EMP001", ManagerId = null! });

        // Act
        var chain = await _service.GetEscalationChain("EMP001");

        // Assert
        chain.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEscalationChain_NullEmployee_ReturnsEmpty()
    {
        // Arrange — employee not found
        _mockEmployeeRepository.Setup(r => r.GetByEmployeeIdAsync("UNKNOWN", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Core.Entities.Employee?)null);

        // Act
        var chain = await _service.GetEscalationChain("UNKNOWN");

        // Assert
        chain.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEscalationChain_SingleLevel_ReturnsOne()
    {
        // Arrange — EMP005 reports to EMP002 who has no manager
        _mockEmployeeRepository.Setup(r => r.GetByEmployeeIdAsync("EMP005", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Core.Entities.Employee { EmployeeId = "EMP005", ManagerId = "EMP002" });
        _mockEmployeeRepository.Setup(r => r.GetByEmployeeIdAsync("EMP002", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Core.Entities.Employee { EmployeeId = "EMP002", ManagerId = null! });

        // Act
        var chain = await _service.GetEscalationChain("EMP005");

        // Assert
        chain.Should().HaveCount(1);
        chain[0].Should().Be("EMP002");
    }

    [Fact]
    public async Task GetEscalationChain_DeepHierarchy()
    {
        // Arrange — 5 levels deep
        _mockEmployeeRepository.SetupSequence(r => r.GetByEmployeeIdAsync("EMP005", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Core.Entities.Employee { EmployeeId = "EMP005", ManagerId = "EMP004" })
            .ReturnsAsync(new Core.Entities.Employee { EmployeeId = "EMP004", ManagerId = "EMP003" })
            .ReturnsAsync(new Core.Entities.Employee { EmployeeId = "EMP003", ManagerId = "EMP002" })
            .ReturnsAsync(new Core.Entities.Employee { EmployeeId = "EMP002", ManagerId = "EMP001" })
            .ReturnsAsync((Core.Entities.Employee?)null);

        // Act
        var chain = await _service.GetEscalationChain("EMP005");

        // Assert
        chain.Should().HaveCount(4);
        chain[0].Should().Be("EMP004");
        chain[1].Should().Be("EMP003");
        chain[2].Should().Be("EMP002");
        chain[3].Should().Be("EMP001");
    }

    #endregion

    #region Max Escalation Level Tests

    [Fact]
    public void MaxEscalationLevelConstant_IsThree()
    {
        // Assert
        WarningThresholds.MaxEscalationLevel.Should().Be(3);
    }

    [Fact]
    public void EscalationActionWindowHoursConstant_IsTwentyFour()
    {
        // Assert
        WarningThresholds.EscalationActionWindowHours.Should().Be(24);
    }

    [Fact]
    public void ActiveWarningValidityDaysConstant_IsNinety()
    {
        // Assert
        WarningThresholds.ActiveWarningValidityDays.Should().Be(90);
    }

    #endregion

    #region Helper Methods

    private static Mock<Microsoft.EntityFrameworkCore.DbSet<T>> SetupFindableSet<T>(T entity) where T : class
    {
        var mockSet = new Mock<Microsoft.EntityFrameworkCore.DbSet<T>>();
        var entities = new List<T> { entity }.AsQueryable();
        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(entities.Provider);
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(entities.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(entities.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(entities.GetEnumerator());
        mockSet.Setup(m => m.FindAsync(It.IsAny<object[]>())).ReturnsAsync(entity);
        return mockSet;
    }

    #endregion
}
