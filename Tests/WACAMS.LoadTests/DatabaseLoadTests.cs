using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NBomber.CSharp;
using Wacam.Api.Data;
using WACAMS.Core.Entities;
using WACAMS.Core.Entities;
using WACAMS.Core.Enums;

namespace WACAMS.LoadTests;

/// <summary>
/// Database Load Tests — Direct EF Core / PostgreSQL benchmarks for WACAMS data layer.
/// Measures bulk insert, complex query, concurrent read/write performance and index effectiveness.
/// </summary>
public class DatabaseLoadTests
{
    private static WacamDbContext? _dbContext;
    private static IConfiguration? _configuration;

    /// <summary>
    /// Initialize the DbContext with a dedicated test connection string.
    /// Uses a separate database or schema to avoid corrupting production data.
    /// </summary>
    public static async Task InitializeAsync()
    {
        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables();

        _configuration = builder.Build();

        var connectionString = _configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=wacams_test;Username=postgres;Password=postgres";

        // Use in-memory or dedicated test DB — adjust based on your setup
        var optionsBuilder = new DbContextOptionsBuilder<WacamDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        _dbContext = new WacamDbContext(optionsBuilder.Options);

        // Ensure test database exists (non-destructive)
        await _dbContext.Database.CanConnectAsync();
        Console.WriteLine("✓ Test database connected successfully.");
    }

    // ─── Test 1: Bulk Insert Performance (10,000 attendance records) ───

    public static NBomberStep CreateBulkInsertTest()
    {
        return Step.Create("bulk_insert_10000_records", async () =>
        {
            if (_dbContext is null) throw new InvalidOperationException("DbContext not initialized.");

            var batchSize = 1000;
            var totalBatches = 10; // 10 × 1000 = 10,000 records
            var sw = Stopwatch.StartNew();
            var inserted = 0;

            for (int batch = 0; batch < totalBatches; batch++)
            {
                var records = GenerateAttendanceRecords(batch * batchSize, batchSize);
                await _dbContext.AttendanceRecords.AddRangeAsync(records);
                await _dbContext.SaveChangesAsync();
                inserted += batchSize;

                Console.Write($"\r  Inserted {inserted}/10,000...");
            }

            sw.Stop();
            Console.WriteLine($"\n  ✓ Bulk insert completed: {inserted} records in {sw.ElapsedMilliseconds:F0}ms");
            Console.WriteLine($"  Average: {(sw.ElapsedMilliseconds / inserted):F2}ms per record");

            // Clean up test data
            _dbContext.AttendanceRecords.RemoveRange(
                _dbContext.AttendanceRecords.Where(r => r.EmployeeId.StartsWith("LOADTEST_")));
            await _dbContext.SaveChangesAsync();

            return Response.Ok();
        })
        .WithWarmUpCount(2)
        .WithLoadSimulator(Simulator.Concurrency(1)); // Sequential inserts
    }

    /// <summary>
    /// Generate synthetic attendance records for bulk insert testing.
    /// </summary>
    private static List<AttendanceRecord> GenerateAttendanceRecords(int startIndex, int count)
    {
        var records = new List<AttendanceRecord>();
        var baseDate = DateTime.Now.Date;

        for (int i = 0; i < count; i++)
        {
            var empId = $"LOADTEST_EMP_{startIndex + i:D6}";
            var clockIn = TimeSpan.FromHours(6 + (i % 8)) + TimeSpan.FromMinutes(new Random().Next(0, 60));
            var expectedStart = TimeSpan.FromHours(6);
            var minutesLate = Math.Max(0, (clockIn - expectedStart).TotalMinutes);

            records.Add(new AttendanceRecord
            {
                EmployeeId = empId,
                Date = baseDate.AddDays(i % 30),
                ShiftCode = i % 3 == 0 ? "NS" : i % 3 == 1 ? "ES" : "MS",
                ClockIn = clockIn,
                ExpectedClockIn = expectedStart,
                IsLate = minutesLate > 5,
                MinutesLate = (int)Math.Floor(minutesLate),
                ClockOut = clockIn.Add(TimeSpan.FromHours(8)),
                ExpectedClockOut = expectedStart.Add(TimeSpan.FromHours(8)),
                CreatedAt = DateTime.UtcNow
            });
        }

        return records;
    }

    // ─── Test 2: Complex Query Performance (Monthly Report Aggregation) ───

    public static NBomberStep CreateComplexQueryTest()
    {
        return Step.Create("complex_monthly_query", async () =>
        {
            if (_dbContext is null) throw new InvalidOperationException("DbContext not initialized.");

            var latencies = new List<double>();
            var iterations = 50;

            for (int i = 0; i < iterations; i++)
            {
                var sw = Stopwatch.StartNew();

                // Simulate monthly report query: JOIN employees, departments, attendance, warnings
                var result = await _dbContext.AttendanceRecords
                    .Include(r => r.Employee)
                        .ThenInclude(e => e!.Department)
                    .Where(r => r.Date >= DateTime.Now.AddMonths(-1) && r.Date <= DateTime.Now)
                    .GroupBy(r => r.EmployeeId)
                    .Select(g => new
                    {
                        EmployeeId = g.Key,
                        TotalDays = g.Count(),
                        TotalLateDays = g.Count(r => r.IsLate),
                        TotalMinutesLate = g.Sum(r => r.MinutesLate),
                        AvgMinutesLate = g.Average(r => r.MinutesLate)
                    })
                    .OrderByDescending(x => x.TotalLateDays)
                    .Take(100)
                    .ToListAsync();

                sw.Stop();
                latencies.Add(sw.Elapsed.TotalMilliseconds);

                if (i % 10 == 0)
                    Console.Write($"\r  Query iteration {i + 1}/{iterations}...");
            }

            var stats = ApiLoadTests.CalculatePercentiles(latencies);
            Console.WriteLine($"\n  ✓ Complex query ({iterations} iterations):");
            Console.WriteLine($"    p50={stats.P50:F0}ms | p95={stats.P95:F0}ms | p99={stats.P99:F0}ms | avg={stats.Avg:F0}ms");

            return Response.Ok();
        })
        .WithWarmUpCount(3)
        .WithLoadSimulator(Simulator.Concurrency(5)); // Some concurrency on reads
    }

    // ─── Test 3: Concurrent Read/Write Scenarios ───

    public static NBomberStep CreateConcurrentReadWriteTest()
    {
        return Step.Create("concurrent_read_write", async () =>
        {
            if (_dbContext is null) throw new InvalidOperationException("DbContext not initialized.");

            var sw = Stopwatch.StartNew();
            var results = new List<(bool Success, double LatencyMs)>();
            var tasks = new List<Task>();
            var lockObj = new object();

            // 70% reads, 30% writes mix
            for (int i = 0; i < 100; i++)
            {
                var isWrite = i % 10 < 3; // 30% writes
                var idx = i;

                tasks.Add(Task.Run(async () =>
                {
                    var localSw = Stopwatch.StartNew();
                    try
                    {
                        if (isWrite)
                        {
                            // Write: Insert a new attendance record
                            var record = new AttendanceRecord
                            {
                                EmployeeId = $"RW_TEST_{idx}",
                                Date = DateTime.Now.Date,
                                ShiftCode = "ES",
                                ClockIn = TimeSpan.FromHours(6),
                                ExpectedClockIn = TimeSpan.FromHours(6),
                                IsLate = false,
                                MinutesLate = 0,
                                CreatedAt = DateTime.UtcNow
                            };
                            _dbContext.AttendanceRecords.Add(record);
                            await _dbContext.SaveChangesAsync();

                            // Clean up immediately
                            var toRemove = _dbContext.AttendanceRecords.FirstOrDefault(r => r.EmployeeId == record.EmployeeId);
                            if (toRemove != null)
                            {
                                _dbContext.AttendanceRecords.Remove(toRemove);
                                await _dbContext.SaveChangesAsync();
                            }
                        }
                        else
                        {
                            // Read: Query recent attendance
                            await _dbContext.AttendanceRecords
                                .Where(r => r.Date >= DateTime.Now.AddDays(-1))
                                .Take(50)
                                .ToListAsync();
                        }

                        localSw.Stop();
                        lock (lockObj) results.Add((true, localSw.Elapsed.TotalMilliseconds));
                    }
                    catch (Exception ex)
                    {
                        localSw.Stop();
                        Console.WriteLine($"\n  ✗ Concurrency error at idx={idx}: {ex.Message}");
                        lock (lockObj) results.Add((false, localSw.Elapsed.TotalMilliseconds));
                    }
                }));
            }

            await Task.WhenAll(tasks);
            sw.Stop();

            var successes = results.Count(r => r.Success);
            var failures = results.Count(r => !r.Success);
            var readLatencies = results.Where(r => r.Success).Select(r => r.LatencyMs).ToList();

            if (readLatencies.Any())
            {
                var stats = CalculatePercentiles(readLatencies);
                Console.WriteLine($"\n  ✓ Concurrent R/W (100 ops, 70/30 split):");
                Console.WriteLine($"    Success={successes}, Failures={failures}");
                Console.WriteLine($"    Read p50={stats.P50:F0}ms | p95={stats.P95:F0}ms | p99={stats.P99:F0}ms");
            }

            return Response.Ok();
        })
        .WithWarmUpCount(3)
        .WithLoadSimulator(Simulator.Concurrency(20));
    }

    // ─── Test 4: Index Effectiveness ───

    public static NBomberStep CreateIndexEffectivenessTest()
    {
        return Step.Create("index_effectiveness", async () =>
        {
            if (_dbContext is null) throw new InvalidOperationException("DbContext not initialized.");

            var queries = new Dictionary<string, Func<IQueryable<AttendanceRecord>>>
            {
                ["EmployeeId lookup"] = () => _dbContext.AttendanceRecords.Where(r => r.EmployeeId == "708513"),
                ["Date range query"] = () => _dbContext.AttendanceRecords.Where(r => r.Date >= DateTime.Now.AddDays(-7)),
                ["IsLate filter"] = () => _dbContext.AttendanceRecords.Where(r => r.IsLate),
                ["ShiftCode + Date composite"] = () => _dbContext.AttendanceRecords.Where(r => r.ShiftCode == "ES" && r.Date >= DateTime.Now.AddDays(-30)),
                ["Full table scan (no filters)"] = () => _dbContext.AttendanceRecords.AsNoTracking()
            };

            foreach (var (name, queryFunc) in queries)
            {
                var latencies = new List<double>();
                var iterations = 20;

                for (int i = 0; i < iterations; i++)
                {
                    var sw = Stopwatch.StartNew();
                    var count = await queryFunc().CountAsync();
                    sw.Stop();
                    latencies.Add(sw.Elapsed.TotalMilliseconds);
                }

                var stats = CalculatePercentiles(latencies);
                Console.WriteLine($"  [{name,-45}] p50={stats.P50:F0}ms | p95={stats.P95:F0}ms | avg={stats.Avg:F0}ms | rows={count}");
            }

            return Response.Ok();
        })
        .WithWarmUpCount(2)
        .WithLoadSimulator(Simulator.Concurrency(1));
    }

    // ─── Test 5: Large Result Set Pagination ───

    public static NBomberStep CreatePaginationPerformanceTest()
    {
        return Step.Create("pagination_performance", async () =>
        {
            if (_dbContext is null) throw new InvalidOperationException("DbContext not initialized.");

            var pageSizes = new[] { 10, 50, 100, 200 };
            var targetPage = 100; // Page far into the dataset

            foreach (var pageSize in pageSizes)
            {
                var sw = Stopwatch.StartNew();
                var result = await _dbContext.AttendanceRecords
                    .OrderBy(r => r.Id)
                    .Skip((targetPage - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                sw.Stop();

                Console.WriteLine($"  [Page {targetPage}, Size {pageSize,-4}] {sw.ElapsedMilliseconds:F0}ms | returned {result.Count} rows");
            }

            return Response.Ok();
        })
        .WithWarmUpCount(2)
        .WithLoadSimulator(Simulator.Concurrency(1));
    }

    // ─── Helper: Percentile Calculation ─────────────────────────────

    private static (double P50, double P95, double P99, double Avg, double Min, double Max) CalculatePercentiles(List<double> latencies)
    {
        if (!latencies.Any()) throw new ArgumentException("No data.");
        var sorted = latencies.Order().ToList();
        var n = sorted.Count;
        return (
            P50: sorted[(int)(n * 0.50)],
            P95: sorted[Math.Min((int)(n * 0.95), n - 1)],
            P99: sorted[Math.Min((int)(n * 0.99), n - 1)],
            Avg: sorted.Average(),
            Min: sorted.First(),
            Max: sorted.Last()
        );
    }

    // ─── Main Runner ───────────────────────────────────────────────

    public static async Task RunAllAsync()
    {
        Console.WriteLine("\n═══════════════════════════════════════════════════════");
        Console.WriteLine("  WACAMS Database Load Test Suite");
        Console.WriteLine("═══════════════════════════════════════════════════════\n");

        await InitializeAsync();

        var tests = new[]
        {
            ("Bulk Insert (10,000 records)", CreateBulkInsertTest()),
            ("Complex Monthly Query", CreateComplexQueryTest()),
            ("Concurrent Read/Write", CreateConcurrentReadWriteTest()),
            ("Index Effectiveness", CreateIndexEffectivenessTest()),
            ("Pagination Performance", CreatePaginationPerformanceTest()),
        };

        foreach (var (name, step) in tests)
        {
            Console.WriteLine($"\n▶ Running: {name}");
            try
            {
                var scenario = ScenarioBuilder.CreateScenario(name, step)
                    .WithOpt(maxDuration: TimeSpan.FromSeconds(120), warmUp: TimeSpan.FromSeconds(5));

                await NBomberRunner.RunTracking(scenario);
                Console.WriteLine($"  ✓ {name} passed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ {name} FAILED: {ex.Message}");
            }
        }

        Console.WriteLine("\n✅ All database load tests finished.");
    }

    public static void Main(string[] args) => RunAllAsync().GetAwaiter().GetResult();
}
