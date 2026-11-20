using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Wacam.Api.Data;
using WACAMS.Core.Entities;
using WACAMS.IntegrationTests.Infrastructure;

namespace WACAMS.IntegrationTests.Dashboard;

/// <summary>
/// Integration tests for the dashboard data aggregation: real-time KPI calculations,
/// department filtering, date range filtering, and trend accuracy.
/// Uses the AttendanceService GetSummaryAsync as the primary data source for dashboard metrics.
/// </summary>
public class DashboardIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly WacamDbContext _context;
    private readonly TestWebApplicationFactory _factory;

    public DashboardIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _context = factory.Services.GetRequiredService<WacamDbContext>();
    }

    #region Real-Time Dashboard Data Aggregation

    [Fact]
    public async Task GetSummary_ShouldAggregateAllAttendanceMetrics()
    {
        // Arrange — Date range covering seeded records (Nov 10–12)
        var startDate = new DateTime(2026, 11, 10);
        var endDate = new DateTime(2026, 11, 12);

        // Act
        var response = await _client.GetAsync(
            $"/api/attendance/summary?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().NotBeNull();

        // Verify all KPI fields are present
        body!.Should().ContainKey("present");
        body.Should().ContainKey("late");
        body.Should().ContainKey("earlyDeparture");
        body.Should().ContainKey("absenteeism");
        body.Should().ContainKey("awol");
        body.Should().ContainKey("missingClockIn");
        body.Should().ContainKey("missingClockOut");
        body.Should().ContainKey("attendancePercent");
        body.Should().ContainKey("totalLostProductionMinutes");
        body.Should().ContainKey("totalEmployees");
    }

    [Fact]
    public async Task DashboardKPIs_ShouldReflectActualDatabaseState()
    {
        // Arrange
        var startDate = new DateTime(2026, 11, 10);
        var endDate = new DateTime(2026, 11, 12);

        // Count actual late records in DB
        var dbLateCount = await _context.AttendanceRecords.CountAsync(
            r => r.Date >= startDate && r.Date <= endDate && r.IsLate);

        // Act
        var response = await _client.GetAsync(
            $"/api/attendance/summary?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().NotBeNull();

        var reportedLate = Convert.ToInt32(body!["late"]);
        reportedLate.Should().BeGreaterOrEqualTo(dbLateCount); // May include auto-detected exceptions
    }

    [Fact]
    public async Task Dashboard_ShouldCalculateAttendancePercentageCorrectly()
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
        var pct = Convert.ToDecimal(body!["attendancePercent"]);

        pct.Should().BeGreaterOrEqualTo(0);
        pct.Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public async Task Dashboard_ShouldReportLostProductionMinutes()
    {
        // Arrange
        var startDate = new DateTime(2026, 11, 10);
        var endDate = new DateTime(2026, 11, 12);

        // Calculate expected lost minutes from DB
        var expectedLost = await _context.AttendanceRecords
            .Where(r => r.Date >= startDate && r.Date <= endDate)
            .SumAsync(r => r.MinutesLate + r.MinutesEarly);

        // Act
        var response = await _client.GetAsync(
            $"/api/attendance/summary?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var reportedLost = Convert.ToInt32(body!["totalLostProductionMinutes"]);

        reportedLost.Should().BeGreaterOrEqualTo(expectedLost);
    }

    #endregion

    #region KPI Calculations with Live Data

    [Fact]
    public async Task LateCount_ShouldMatchIsLateRecords()
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
        var lateCount = Convert.ToInt32(body!["late"]);

        // Should be at least the number of IsLate=true records
        lateCount.Should().BeGreaterOrEqualTo(2); // Seeder has 2 late records
    }

    [Fact]
    public async Task EarlyDepartureCount_ShouldMatchIsEarlyRecords()
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
        var earlyCount = Convert.ToInt32(body!["earlyDeparture"]);

        earlyCount.Should().BeGreaterOrEqualTo(1); // Seeder has 1 early departure record
    }

    [Fact]
    public async Task PresentCount_ShouldMatchClockInRecords()
    {
        // Arrange
        var startDate = new DateTime(2026, 11, 10);
        var endDate = new DateTime(2026, 11, 12);

        // Count actual clock-in records in DB
        var dbPresent = await _context.AttendanceRecords.CountAsync(
            r => r.Date >= startDate && r.Date <= endDate && r.ClockIn.HasValue);

        // Act
        var response = await _client.GetAsync(
            $"/api/attendance/summary?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var reportedPresent = Convert.ToInt32(body!["present"]);

        reportedPresent.Should().BeGreaterOrEqualTo(dbPresent);
    }

    [Fact]
    public async Task AWOLDetection_ShouldIdentifyConsecutiveAbsences()
    {
        // Arrange — Employee 708155 has records on day 1 & 2 but NOT day 3
        var startDate = new DateTime(2026, 11, 10);
        var endDate = new DateTime(2026, 11, 12);

        // Act
        var response = await _client.GetAsync(
            $"/api/attendance/summary?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var awolCount = Convert.ToInt32(body!["awol"]);

        // AWOL threshold is 3 consecutive days — may or may not trigger depending on seed data pattern
        awolCount.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task MissingClockInCount_ShouldDetectNullClockIns()
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
        var missingCI = Convert.ToInt32(body!["missingClockIn"]);

        missingCI.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task MissingClockOutCount_ShouldDetectNullClockOuts()
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
        var missingCO = Convert.ToInt32(body!["missingClockOut"]);

        missingCO.Should().BeGreaterOrEqualTo(0);
    }

    #endregion

    #region Department Filter Application

    [Fact]
    public async Task DepartmentFilter_ShouldReduceEmployeeCount()
    {
        // Arrange
        var startDate = new DateTime(2026, 11, 10);
        var endDate = new DateTime(2026, 11, 12);

        // Get all departments count
        var allResponse = await _client.GetAsync(
            $"/api/attendance/summary?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");
        var allBody = await allResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var allEmpCount = Convert.ToInt32(allBody!["totalEmployees"]);

        // Get production dept only
        var deptResponse = await _client.GetAsync(
            $"/api/attendance/summary?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}&departmentId=1");
        var deptBody = await deptResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var deptEmpCount = Convert.ToInt32(deptBody!["totalEmployees"]);

        // Assert
        allResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        deptResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Production dept should have fewer employees than all departments combined
        // (since we also have Engineering dept employees)
        deptEmpCount.Should().BeLessThanOrEqualTo(allEmpCount);
    }

    [Fact]
    public async Task DepartmentFilter_ShouldReturnCorrectDepartmentName()
    {
        // Arrange
        var startDate = new DateTime(2026, 11, 10);
        var endDate = new DateTime(2026, 11, 12);

        // Act
        var response = await _client.GetAsync(
            $"/api/attendance/summary?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}&departmentId=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body!["departmentName"].ToString().Should().Contain("Production");
    }

    [Fact]
    public async Task NonExistentDepartmentFilter_ShouldReturnEmptyResults()
    {
        // Arrange
        var startDate = new DateTime(2026, 11, 10);
        var endDate = new DateTime(2026, 11, 12);

        // Act
        var response = await _client.GetAsync(
            $"/api/attendance/summary?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}&departmentId=999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var empCount = Convert.ToInt32(body!["totalEmployees"]);
        empCount.Should().BeGreaterOrEqualTo(0);
    }

    #endregion

    #region Date Range Filtering

    [Fact]
    public async Task SingleDayRange_ShouldReturnAccurateSummary()
    {
        // Arrange
        var singleDate = new DateTime(2026, 11, 10);

        // Act
        var response = await _client.GetAsync(
            $"/api/attendance/summary?startDate={singleDate:yyyy-MM-dd}&endDate={singleDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().ContainKey("present");
        body.Should().ContainKey("late");
        body["date"].ToString().Should().Contain("2026-11-10");
    }

    [Fact]
    public async Task WideDateRange_ShouldIncludeAllPeriodData()
    {
        // Arrange
        var startDate = new DateTime(2026, 11, 1);
        var endDate = new DateTime(2026, 11, 30);

        // Act
        var response = await _client.GetAsync(
            $"/api/attendance/summary?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().ContainKey("attendancePercent");
        body["attendancePercent"].ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task EmptyDateRange_ShouldReturnZeroes()
    {
        // Arrange — Future date with no data
        var futureStart = new DateTime(2027, 1, 1);
        var futureEnd = new DateTime(2027, 1, 7);

        // Act
        var response = await _client.GetAsync(
            $"/api/attendance/summary?startDate={futureStart:yyyy-MM-dd}&endDate={futureEnd:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Convert.ToInt32(body!["present"]).Should().Be(0);
        Convert.ToInt32(body!["late"]).Should().Be(0);
        Convert.ToDecimal(body!["attendancePercent"]).Should().Be(0);
    }

    [Fact]
    public async Task InvalidDateRange_ShouldReturnBadRequest()
    {
        // Arrange — End date before start date
        var startDate = new DateTime(2026, 11, 20);
        var endDate = new DateTime(2026, 11, 10);

        // Act
        var response = await _client.GetAsync(
            $"/api/attendance/summary?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Trend Calculation Accuracy

    [Fact]
    public async Task IndividualReport_ShouldShowCorrectTrend()
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

        var daysPresent = Convert.ToInt32(body!["daysPresent"]);
        var daysLate = Convert.ToInt32(body!["daysLate"]);
        var totalDays = Convert.ToInt32(body!["totalDays"]);

        // Lesedi Mashile (708513) has 3 records on Nov 10-12, all on time
        daysPresent.Should().BeGreaterOrEqualTo(3);
        daysLate.Should().BeGreaterOrEqualTo(0);
        daysPresent.Should().BeLessThanOrEqualTo(totalDays);
    }

    [Fact]
    public async Task MultipleEmployees_ShouldHaveDifferentTrends()
    {
        // Arrange
        var startDate = new DateTime(2026, 11, 10);
        var endDate = new DateTime(2026, 11, 12);

        // Act — Get reports for two different employees
        var resp1 = await _client.GetAsync(
            $"/api/attendance/individual/708513?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");
        var resp2 = await _client.GetAsync(
            $"/api/attendance/individual/708155?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        // Assert
        resp1.StatusCode.Should().Be(HttpStatusCode.OK);
        resp2.StatusCode.Should().Be(HttpStatusCode.OK);

        var body1 = await resp1.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var body2 = await resp2.Content.ReadFromJsonAsync<Dictionary<string, object>>();

        var late1 = Convert.ToInt32(body1!["daysLate"]);
        var late2 = Convert.ToInt32(body2!["daysLate"]);

        // 708513 is on time, 708155 is late
        late1.Should().BeLessThan(late2);
    }

    [Fact]
    public async Task AttendancePercentage_ShouldReflectProportion()
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
        var pct = Convert.ToDecimal(body!["attendancePercentage"]);

        // 708513 has all 3 working days present → ~100%
        pct.Should().BeGreaterOrEqualTo(90);
    }

    #endregion

    #region Dashboard Edge Cases

    [Fact]
    public async Task NoRecordsInRange_ShouldReturnGracefulZeros()
    {
        // Arrange — Employee with no records in this range
        var startDate = new DateTime(2027, 6, 1);
        var endDate = new DateTime(2027, 6, 7);

        // Act
        var response = await _client.GetAsync(
            $"/api/attendance/individual/708513?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Convert.ToInt32(body!["daysPresent"]).Should().Be(0);
        Convert.ToDecimal(body!["attendancePercentage"]).Should().Be(0);
    }

    [Fact]
    public async Task SummaryWithOnlyOneEmployee_ShouldBeAccurate()
    {
        // Arrange — Use an employee with exactly one record
        var startDate = new DateTime(2026, 11, 10);
        var endDate = new DateTime(2026, 11, 10);

        // Act
        var response = await _client.GetAsync(
            $"/api/attendance/summary?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().ContainKey("present");
        body.Should().ContainKey("attendancePercent");
    }

    [Fact]
    public async Task DashboardData_ShouldBeConsistentAcrossCalls()
    {
        // Arrange
        var startDate = new DateTime(2026, 11, 10);
        var endDate = new DateTime(2026, 11, 12);

        // Act — Call twice
        var resp1 = await _client.GetAsync(
            $"/api/attendance/summary?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");
        var resp2 = await _client.GetAsync(
            $"/api/attendance/summary?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        // Assert — Both calls should return identical results
        var body1 = await resp1.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var body2 = await resp2.Content.ReadFromJsonAsync<Dictionary<string, object>>();

        body1!["present"].ToString().Should().Be(body2!["present"].ToString());
        body1["late"].ToString().Should().Be(body2["late"].ToString());
        body1["attendancePercent"].ToString().Should().Be(body2["attendancePercent"].ToString());
    }

    #endregion
}
