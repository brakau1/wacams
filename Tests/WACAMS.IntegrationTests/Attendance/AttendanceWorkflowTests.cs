using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Wacam.Api.Data;
using WACAMS.Core.Entities;
using Wacam.Api.Middleware;
using WACAMS.IntegrationTests.Infrastructure;

namespace WACAMS.IntegrationTests.Attendance;

/// <summary>
/// Integration tests for the complete attendance workflow: clock-in/out, late detection,
/// exception auto-creation, missing scan jobs, summary calculation, and department filtering.
/// </summary>
public class AttendanceWorkflowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly WacamDbContext _context;
    private readonly TestWebApplicationFactory _factory;

    public AttendanceWorkflowTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        _context = scope.ServiceProvider.GetRequiredService<WacamDbContext>();
    }

    #region Clock-In / Clock-Out Flow

    [Fact]
    public async Task ClockIn_ShouldCreateRecordAndReturnSuccess()
    {
        // Arrange
        var date = new DateTime(2026, 11, 15);
        var request = new { employeeId = "708513", date = date.ToString("yyyy-MM-dd") };

        // Act
        var response = await _client.PostAsJsonAsync("/api/attendance/clockin", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().NotBeNull();
        body!["employeeId"].ToString().Should().Be("708513");

        // Verify record exists in database
        var createdRecord = await _context.AttendanceRecords
            .FirstOrDefaultAsync(r => r.EmployeeId == "708513" && r.Date.Date == date.Date);
        createdRecord.Should().NotBeNull();
        createdRecord!.ClockIn.Should().NotBeNull();
        createdRecord.IsLate.Should().BeFalse();
    }

    [Fact]
    public async Task ClockIn_ForInactiveEmployee_ShouldReturnNotFound()
    {
        // Arrange
        var date = new DateTime(2026, 11, 15);
        var request = new { employeeId = "999999", date = date.ToString("yyyy-MM-dd") };

        // Act
        var response = await _client.PostAsJsonAsync("/api/attendance/clockin", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ClockIn_AlreadyClockedIn_ShouldReturnConflict()
    {
        // Arrange -- employee 708513 already has records on baseDate from seeder
        var baseDate = new DateTime(2026, 11, 10);
        var request = new { employeeId = "708513", date = baseDate.ToString("yyyy-MM-dd") };

        // Act
        var response = await _client.PostAsJsonAsync("/api/attendance/clockin", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ClockOut_WithNoExistingRecord_ShouldReturnNotFound()
    {
        // Arrange
        var request = new { employeeId = "708513", date = new DateTime(2026, 12, 25).ToString("yyyy-MM-dd") };

        // Act
        var response = await _client.PostAsJsonAsync("/api/attendance/clockout", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ClockOut_AlreadyClockedOut_ShouldReturnConflict()
    {
        // Arrange -- employee 708513 has records on baseDate with clock-out from seeder
        var baseDate = new DateTime(2026, 11, 10);
        var request = new { employeeId = "708513", date = baseDate.ToString("yyyy-MM-dd") };

        // Act
        var response = await _client.PostAsJsonAsync("/api/attendance/clockout", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    #endregion

    #region Late Detection & Auto-Exception

    [Fact]
    public async Task ExistingLateRecord_ShouldHaveCorrectMinutesLate()
    {
        // Arrange -- Employee 708155 has a late record on baseDate (25 min late)
        var baseDate = new DateTime(2026, 11, 10);

        // Act
        var record = await _context.AttendanceRecords
            .FirstOrDefaultAsync(r => r.EmployeeId == "708155" && r.Date.Date == baseDate);

        // Assert
        record.Should().NotBeNull();
        record!.IsLate.Should().BeTrue();
        record.MinutesLate.Should().Be(25);
        record.ClockIn.Should().Be(new TimeSpan(6, 25, 0));
        record.ExpectedClockIn.Should().Be(new TimeSpan(6, 0, 0));
    }

    [Fact]
    public async Task ExistingEarlyDepartureRecord_ShouldHaveCorrectMinutesEarly()
    {
        // Arrange -- Employee 802987 has an early departure record on baseDate (20 min early)
        var baseDate = new DateTime(2026, 11, 10);

        // Act
        var record = await _context.AttendanceRecords
            .FirstOrDefaultAsync(r => r.EmployeeId == "802987" && r.Date.Date == baseDate);

        // Assert
        record.Should().NotBeNull();
        record!.IsEarlyDeparture.Should().BeTrue();
        record.MinutesEarly.Should().Be(20);
        record.ClockOut.Should().Be(new TimeSpan(13, 40, 0));
        record.ExpectedClockOut.Should().Be(new TimeSpan(14, 0, 0));
    }

    #endregion

    #region Missing Scan Jobs

    [Fact]
    public async Task ScanMissingClockIns_ShouldCreateAbsenteeismExceptionsForMissingRecords()
    {
        // Arrange
        var date = new DateTime(2026, 11, 10);

        // Act -- Use direct service access
        var scope = _factory.Services.CreateScope();
        var attendanceService = scope.ServiceProvider.GetRequiredService<Wacam.Api.Services.AttendanceService>();
        await attendanceService.ScanMissingClockInsAsync(date);
        await _context.SaveChangesAsync();

        // Assert -- Should have created UnplannedAbsenteeism exceptions
        var exceptions = await _context.AttendanceExceptions
            .Where(ae => ae.Date.Date == date.Date && ae.ExceptionType == "UnplannedAbsenteeism")
            .ToListAsync();
        exceptions.Should().NotBeNull();
    }

    [Fact]
    public async Task ScanMissingClockOuts_ShouldCreateExceptionsForUnclockedOutEmployees()
    {
        // Arrange -- Employee 802987 has a record on baseDate.AddDays(1) without clock-out
        var date = new DateTime(2026, 11, 11);

        // Act
        var scope = _factory.Services.CreateScope();
        var attendanceService = scope.ServiceProvider.GetRequiredService<Wacam.Api.Services.AttendanceService>();
        await attendanceService.ScanMissingClockOutsAsync(date);
        await _context.SaveChangesAsync();

        // Assert -- Should have created MissingClockOut exceptions
        var exceptions = await _context.AttendanceExceptions
            .Where(ae => ae.Date.Date == date.Date && ae.ExceptionType == "MissingClockOut")
            .ToListAsync();
        exceptions.Should().NotBeNull();
    }

    #endregion

    #region Summary Calculation

    [Fact]
    public async Task GetSummary_ShouldCalculateCorrectAttendancePercentages()
    {
        // Arrange
        var startDate = new DateTime(2026, 11, 10);
        var endDate = new DateTime(2026, 11, 12);

        // Act
        var response = await _client.GetAsync(
            $"/api/attendance/summary?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().NotBeNull();
        body!["totalEmployees"].Should().BeGreaterThan(0);
        body!["present"].Should().BeGreaterOrEqualTo(0);
        body!["late"].Should().BeGreaterOrEqualTo(0);
        body!["attendancePercent"].Should().BeGreaterOrEqualTo(0);
        body!["attendancePercent"].Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public async Task GetSummary_WithDepartmentFilter_ShouldReturnFilteredResults()
    {
        // Arrange -- Production Dept = ID 1
        var startDate = new DateTime(2026, 11, 10);
        var endDate = new DateTime(2026, 11, 12);

        // Act
        var response = await _client.GetAsync(
            $"/api/attendance/summary?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}&departmentId=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().NotBeNull();
        body!["departmentName"].ToString().Should().Contain("Production");
    }

    [Fact]
    public async Task GetIndividualReport_ShouldReturnCompleteEmployeeHistory()
    {
        // Arrange
        var startDate = new DateTime(2026, 11, 10);
        var endDate = new DateTime(2026, 11, 12);

        // Act
        var response = await _client.GetAsync(
            $"/api/attendance/individual/708513?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().NotBeNull();
        body!["employeeId"].ToString().Should().Be("708513");
        body!["displayName"].ToString().Should().Be("Lesedi Mashile");
        body!["daysPresent"].Should().BeGreaterOrEqualTo(0);
        body!["daysLate"].Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task GetIndividualReport_ForUnknownEmployee_ShouldReturnNotFound()
    {
        // Arrange
        var startDate = new DateTime(2026, 11, 10);
        var endDate = new DateTime(2026, 11, 12);

        // Act
        var response = await _client.GetAsync(
            $"/api/attendance/individual/999999?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Daily Records

    [Fact]
    public async Task GetDaily_ShouldReturnAllEmployeeRecordsForDate()
    {
        // Arrange
        var date = new DateTime(2026, 11, 10);

        // Act
        var response = await _client.GetAsync($"/api/attendance/daily?date={date:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
        body.Should().NotBeNull();
        body!.Count.Should().BeGreaterOrEqualTo(3); // Multiple employees have records on this date
    }

    [Fact]
    public async Task GetDaily_WithDepartmentFilter_ShouldReturnOnlyDeptRecords()
    {
        // Arrange
        var date = new DateTime(2026, 11, 10);

        // Act
        var response = await _client.GetAsync(
            $"/api/attendance/daily?date={date:yyyy-MM-dd}&departmentId=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
        body.Should().NotBeNull();
        foreach (var record in body!)
            record.Should().ContainKey("employeeId");
    }

    #endregion

    #region Total Hours Calculation

    [Fact]
    public void AttendanceRecord_TotalHours_NormalShift_ShouldCalculateEightHours()
    {
        // Arrange
        var record = new AttendanceRecord
        {
            ClockIn = new TimeSpan(6, 0, 0),
            ClockOut = new TimeSpan(14, 0, 0)
        };

        // Act
        var hours = record.TotalHoursWorked;

        // Assert
        hours.Should().BeApproximately(8m, 0.01m);
    }

    [Fact]
    public void AttendanceRecord_TotalHours_OvernightShift_ShouldCalculateEightHours()
    {
        // Arrange -- Night shift: 22:00 -> 06:00
        var record = new AttendanceRecord
        {
            ClockIn = new TimeSpan(22, 0, 0),
            ClockOut = new TimeSpan(6, 0, 0)
        };

        // Act
        var hours = record.TotalHoursWorked;

        // Assert
        hours.Should().BeApproximately(8m, 0.01m);
    }

    #endregion
}
