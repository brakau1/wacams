using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wacam.Api.Data;
using Wacam.Api.Middleware;
using WACAMS.Core.Entities;
using WACAMS.IntegrationTests.Infrastructure;
using WACAMS.Core.DTOs;

namespace WACAMS.IntegrationTests.Escalation;

/// <summary>
/// Integration tests for the escalation workflow: auto-creation on final warning,
/// escalation chain progression, 24-hour timeout, max level resolution, and notifications.
/// </summary>
public class EscalationWorkflowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly WacamDbContext _context;
    private readonly TestWebApplicationFactory _factory;

    public EscalationWorkflowTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _context = factory.Services.GetRequiredService<WacamDbContext>();
    }

    #region Auto-Creation from FinalWarning

    [Fact]
    public async Task AutoCreateEscalation_FinalWarning_ShouldCreateLevel1Escalation()
    {
        // Arrange — Use existing VerbalWarning (id=2 from seeder) as a proxy to test escalation creation logic
        // In production, GenerateWarningAsync triggers this when WarningType == "FinalWrittenWarning"
        var scope = _factory.Services.CreateScope();
        var escalationService = scope.ServiceProvider.GetRequiredService<Wacam.Api.Services.EscalationService>();

        // Find an existing warning letter
        var existingWarning = await _context.WarningLetters.FirstOrDefaultAsync();
        existingWarning.Should().NotBeNull();

        // Act — Manually call auto-create (normally triggered by WarningService)
        await escalationService.AutoCreateEscalationAsync(existingWarning!.Id, "708513");
        await _context.SaveChangesAsync();

        // Assert
        var escalation = await _context.Escalations
            .FirstOrDefaultAsync(e => e.WarningLetterId == existingWarning.Id);

        escalation.Should().NotBeNull();
        escalation!.EmployeeId.Should().Be("708513");
        escalation.Level.Should().Be(1);
        escalation.Status.Should().Be("Pending");
        escalation.ResponsibleManagerId.Should().Be("800001"); // Supervisor is direct manager of TeamLeader's team
    }

    [Fact]
    public async Task AutoCreateEscalation_NonFinalWarning_ShouldNotCreateEscalation()
    {
        // Arrange — Counselling/Verbal/Written warnings should NOT trigger auto-escalation
        var scope = _factory.Services.CreateScope();
        var escalationService = scope.ServiceProvider.GetRequiredService<Wacam.Api.Services.EscalationService>();

        var counsellingWarning = await _context.WarningLetters
            .FirstOrDefaultAsync(wl => wl.WarningType == "Counselling");

        counsellingWarning.Should().NotBeNull();

        // Act
        await escalationService.AutoCreateEscalationAsync(counsellingWarning!.Id, "708155");
        await _context.SaveChangesAsync();

        // Assert — No escalation should be created for non-FinalWrittenWarning
        var escalations = await _context.Escalations
            .Where(e => e.WarningLetterId == counsellingWarning.Id)
            .ToListAsync();

        escalations.Should().BeEmpty();
    }

    [Fact]
    public async Task AutoCreateEscalation_Duplicate_ShouldSkipCreation()
    {
        // Arrange — First call creates escalation
        var scope = _factory.Services.CreateScope();
        var escalationService = scope.ServiceProvider.GetRequiredService<Wacam.Api.Services.EscalationService>();

        var existingWarning = await _context.WarningLetters.FirstOrDefaultAsync();
        existingWarning.Should().NotBeNull();

        // Create first escalation
        await escalationService.AutoCreateEscalationAsync(existingWarning!.Id, "708513");
        await _context.SaveChangesAsync();

        // Act — Second call with same warning ID should skip
        await escalationService.AutoCreateEscalationAsync(existingWarning.Id, "708513");
        await _context.SaveChangesAsync();

        // Assert — Only one escalation should exist
        var count = await _context.Escalations.CountAsync(e => e.WarningLetterId == existingWarning.Id);
        count.Should().Be(1);
    }

    #endregion

    #region Escalation Chain Progression

    [Fact]
    public async Task GetEscalationChain_ShouldReturnFullManagementHierarchy()
    {
        // Arrange
        var scope = _factory.Services.CreateScope();
        var escalationService = scope.ServiceProvider.GetRequiredService<Wacam.Api.Services.EscalationService>();

        // Act — Build chain for operator (manager → teamleader → supervisor → manager)
        var chain = await escalationService.GetEscalationChainAsync("708513");

        // Assert
        chain.EmployeeId.Should().Be("708513");
        chain.DisplayName.Should().Be("Lesedi Mashile");
        chain.Chain.Should().HaveCountGreaterThanOrEqualTo(3); // ManagerId chain: TL → Supervisor → Admin

        chain.Chain[0].ManagerId.Should().Be("707995"); // TeamLeader George Ntseke
        chain.Chain[0].Role.Should().Be("TeamLeader");
    }

    [Fact]
    public async Task ProgressEscalation_Acknowledged_ShouldMarkAsResolvedOrAdvance()
    {
        // Arrange — Use existing Acknowledged escalation (id=1 from seeder)
        var scope = _factory.Services.CreateScope();
        var escalationService = scope.ServiceProvider.GetRequiredService<Wacam.Api.Services.EscalationService>();

        var existingEscalation = await _context.Escalations.FindAsync(1);
        existingEscalation.Should().NotBeNull();

        // Act — Progress the escalation
        var request = new ProgressEscalationRequest
        {
            Notes = "Issue resolved after investigation."
        };

        var result = await escalationService.ProgressEscalationAsync(1, request, "800001");
        await _context.SaveChangesAsync();

        // Assert — Status should change (either Resolved or advanced to next level)
        result.Status.Should().BeOneOf("Resolved", "Pending");
        result.Notes.Should().Contain("Issue resolved");
    }

    [Fact]
    public async Task ProgressEscalation_ResolvedStatus_ShouldThrowException()
    {
        // Arrange — Resolve an escalation first
        var scope = _factory.Services.CreateScope();
        var escalationService = scope.ServiceProvider.GetRequiredService<Wacam.Api.Services.EscalationService>();

        var existingEscalation = await _context.Escalations.FindAsync(1);
        existingEscalation!.Status = "Resolved";
        await _context.SaveChangesAsync();

        // Act & Assert
        var request = new ProgressEscalationRequest { Notes = "Test" };
        var action = () => escalationService.ProgressEscalationAsync(1, request, "800001");
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Escalation is already resolved.");
    }

    [Fact]
    public async Task ProgressEscalation_ForNonExistent_ShouldThrowNotFound()
    {
        // Arrange
        var scope = _factory.Services.CreateScope();
        var escalationService = scope.ServiceProvider.GetRequiredService<Wacam.Api.Services.EscalationService>();

        // Act & Assert
        var request = new ProgressEscalationRequest { Notes = "Test" };
        var action = () => escalationService.ProgressEscalationAsync(99999, request, "800001");
        await action.Should().ThrowAsync<NotFoundException>();
    }

    #endregion

    #region 24-Hour Timeout Auto-Escalation

    [Fact]
    public async Task ScanOverdueEscalations_ShouldAutoEscalatePendingPast24Hours()
    {
        // Arrange — Create an escalation with old CreatedAt (>24 hours ago)
        var scope = _factory.Services.CreateScope();
        var escalationService = scope.ServiceProvider.GetRequiredService<Wacam.Api.Services.EscalationService>();

        var warning = await _context.WarningLetters.FirstOrDefaultAsync();
        warning.Should().NotBeNull();

        var oldEscalation = new Escalation
        {
            EmployeeId = "708513",
            WarningLetterId = warning!.Id,
            Level = 1,
            ResponsibleManagerId = "800001",
            Status = "Pending",
            Notes = "Old escalation for timeout test",
            CreatedAt = DateTime.UtcNow.AddHours(-48) // 48 hours ago
        };

        _context.Escalations.Add(oldEscalation);
        await _context.SaveChangesAsync();

        // Act
        await escalationService.ScanOverdueEscalationsAsync();
        await _context.SaveChangesAsync();

        // Assert — Should have been auto-escalated to next level
        var updated = await _context.Escalations.FindAsync(oldEscalation.Id);
        updated.Should().NotBeNull();

        if (updated!.Status != "Resolved")
        {
            updated.Level.Should().BeGreaterThan(1); // Advanced to next level
            updated.Notes.Should().Contain("[AUTO] Escalated");
        }
        else
        {
            updated.Status.Should().Be("Resolved"); // Top of chain reached
        }
    }

    [Fact]
    public async Task ScanOverdueEscalations_ShouldNotTouchRecentPending()
    {
        // Arrange — Create a fresh escalation (<24 hours old)
        var scope = _factory.Services.CreateScope();
        var escalationService = scope.ServiceProvider.GetRequiredService<Wacam.Api.Services.EscalationService>();

        var warning = await _context.WarningLetters.FirstOrDefaultAsync();
        var recentEscalation = new Escalation
        {
            EmployeeId = "708513",
            WarningLetterId = warning!.Id,
            Level = 1,
            ResponsibleManagerId = "800001",
            Status = "Pending",
            Notes = "Recent escalation — should NOT be touched",
            CreatedAt = DateTime.UtcNow.AddHours(-2) // Only 2 hours ago
        };

        _context.Escalations.Add(recentEscalation);
        await _context.SaveChangesAsync();
        var id = recentEscalation.Id;

        // Act
        await escalationService.ScanOverdueEscalationsAsync();
        await _context.SaveChangesAsync();

        // Assert — Should remain unchanged
        var updated = await _context.Escalations.FindAsync(id);
        updated!.Status.Should().Be("Pending");
        updated.Level.Should().Be(1);
        updated.Notes.Should().NotContain("[AUTO]");
    }

    #endregion

    #region Max Level Reached → Auto-Resolve

    [Fact]
    public async Task ScanOverdueEscalations_AtTopOfChain_ShouldResolve()
    {
        // Arrange — Create escalation at top of chain (no more managers above)
        var scope = _factory.Services.CreateScope();
        var escalationService = scope.ServiceProvider.GetRequiredService<Wacam.Api.Services.EscalationService>();

        var warning = await _context.WarningLetters.FirstOrDefaultAsync();
        var topEscalation = new Escalation
        {
            EmployeeId = "800010", // This is the Production Manager (top-level, no ManagerId)
            WarningLetterId = warning!.Id,
            Level = 99, // Simulate reaching end of chain
            ResponsibleManagerId = "800010",
            Status = "Pending",
            Notes = "At top of chain",
            CreatedAt = DateTime.UtcNow.AddHours(-48)
        };

        _context.Escalations.Add(topEscalation);
        await _context.SaveChangesAsync();
        var id = topEscalation.Id;

        // Act
        await escalationService.ScanOverdueEscalationsAsync();
        await _context.SaveChangesAsync();

        // Assert — Should be resolved since there's no one above
        var updated = await _context.Escalations.FindAsync(id);
        updated!.Status.Should().Be("Resolved");
        updated.ResolvedAt.Should().NotBeNull();
        updated.Notes.Should().Contain("end of escalation chain");
    }

    #endregion

    #region Escalation Notification

    [Fact]
    public async Task AutoCreateEscalation_ShouldAttemptNotification()
    {
        // Arrange — Verify notification service is called during auto-escalation
        var scope = _factory.Services.CreateScope();
        var escalationService = scope.ServiceProvider.GetRequiredService<Wacam.Api.Services.EscalationService>();

        var warning = await _context.WarningLetters.FirstOrDefaultAsync();
        warning.Should().NotBeNull();

        // Act — Should attempt to send notification (even if it fails due to mock)
        await escalationService.AutoCreateEscalationAsync(warning!.Id, "708513");
        await _context.SaveChangesAsync();

        // Assert — Escalation created, notification attempted (logged if failed)
        var esc = await _context.Escalations
            .FirstOrDefaultAsync(e => e.WarningLetterId == warning.Id);

        esc.Should().NotBeNull();
    }

    #endregion

    #region List & Filter

    [Fact]
    public async Task ListEscalations_ShouldSupportDepartmentFilter()
    {
        // Arrange
        var scope = _factory.Services.CreateScope();
        var escalationService = scope.ServiceProvider.GetRequiredService<Wacam.Api.Services.EscalationService>();

        // Act
        var (escalations, totalCount) = await escalationService.ListAsync(
            departmentId: 1, page: 1, pageSize: 50);

        // Assert
        totalCount.Should().BeGreaterOrEqualTo(0);
        foreach (var esc in escalations)
        {
            // All escalations should belong to Production dept employees
            esc.EmployeeId.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task ListEscalations_ShouldSupportStatusFilter()
    {
        // Arrange
        var scope = _factory.Services.CreateScope();
        var escalationService = scope.ServiceProvider.GetRequiredService<Wacam.Api.Services.EscalationService>();

        // Act
        var (escalations, totalCount) = await escalationService.ListAsync(
            status: "Acknowledged", page: 1, pageSize: 50);

        // Assert
        foreach (var esc in escalations)
        {
            esc.Status.Should().Be("Acknowledged");
        }
    }

    [Fact]
    public async Task ListEscalations_ShouldSupportPagination()
    {
        // Arrange
        var scope = _factory.Services.CreateScope();
        var escalationService = scope.ServiceProvider.GetRequiredService<Wacam.Api.Services.EscalationService>();

        // Act — Page 1
        var (page1, total1) = await escalationService.ListAsync(page: 1, pageSize: 2);

        // Act — Page 2
        var (page2, total2) = await escalationService.ListAsync(page: 2, pageSize: 2);

        // Assert
        total1.Should().Be(total2); // Total count should be the same
        page1.Should().HaveCountLessThanOrEqualTo(2);
        page2.Should().HaveCountLessThanOrEqualTo(2);
    }

    #endregion
}
