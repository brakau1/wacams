using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NBomber.CSharp;
using NBomber.Plugins.Http.ConnectionPool;
using NBomber.Plugins.Serialization.SystemTextJson;

namespace WACAMS.LoadTests;

/// <summary>
/// API Load Tests — Concurrent request performance benchmarks for WACAMS endpoints.
/// Measures p50, p95, p99 response times under various concurrency levels.
/// </summary>
public class ApiLoadTests
{
    private const string BaseUrl = "https://localhost:7288"; // Adjust to your deployment URL
    private static string? _adminAccessToken;
    private static string? _operatorAccessToken;
    private static readonly HttpClient Http;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    static ApiLoadTests()
    {
        var pool = new ConnectionPoolSettings(maxConnectionsPerServer: 200);
        Http = HttpClientFactory.Create(
            new HttpClientHandler { AutomaticDecompression = System.Net.DecompressionMethods.GZip },
            pool
        );
        Http.Timeout = TimeSpan.FromSeconds(30);
        Http.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    // ─── Bootstrap: Authenticate before tests ──────────────────────

    public static async Task InitializeAsync()
    {
        // Login as SuperAdmin for privileged operations
        var adminPayload = new
        {
            employeeId = "707995", // Adjust to valid admin employee ID
            password = "707995"    // Adjust to valid password
        };

        try
        {
            var adminResponse = await Http.PostAsJsonAsync($"{BaseUrl}/api/auth/login", adminPayload);
            if (adminResponse.IsSuccessStatusCode)
            {
                var adminData = await adminResponse.Content.ReadFromJsonAsync<LoginResponse>();
                _adminAccessToken = adminData?.AccessToken;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Admin login failed (test may use unauthenticated endpoints): {ex.Message}");
        }

        // Login as Operator for basic operations
        var operatorPayload = new
        {
            employeeId = "708513", // Adjust to valid operator employee ID
            password = "708513"
        };

        try
        {
            var opResponse = await Http.PostAsJsonAsync($"{BaseUrl}/api/auth/login", operatorPayload);
            if (opResponse.IsSuccessStatusCode)
            {
                var opData = await opResponse.Content.ReadFromJsonAsync<LoginResponse>();
                _operatorAccessToken = opData?.AccessToken;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Operator login failed: {ex.Message}");
        }
    }

    // ─── Test 1: Clock-In Under Concurrent Load (100 simultaneous users) ───

    public static NBomberStep CreateClockInLoadTest()
    {
        return Step.Create("clock_in_concurrent_100", async () =>
        {
            var sw = Stopwatch.StartNew();
            var tasks = new List<Task<HttpResponseMessage>>();

            for (int i = 0; i < 100; i++)
            {
                var employeeId = $"TEST_EMP_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{i}";
                var payload = new
                {
                    employeeId = employeeId,
                    date = DateTime.Now.ToString("yyyy-MM-dd")
                };

                var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/api/attendance/clock-in")
                {
                    Content = JsonContent.Create(payload, contentType: Microsoft.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json"), options: JsonOpts),
                    Headers = { Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _adminAccessToken ?? "") }
                };

                tasks.Add(Http.SendAsync(request, HttpCompletionOption.ResponseContentRead));
            }

            var responses = await Task.WhenAll(tasks);
            sw.Stop();

            var latencies = responses.Select(r => r.RequestMessage?.Properties.GetTimestamp().Subtract(r.RequestMessage?.Properties.GetTimestamp()).Duration() ?? TimeSpan.Zero);

            Console.WriteLine($"Clock-In 100 concurrent: Total={sw.Elapsed.TotalMilliseconds:F0}ms, " +
                              $"Avg={(sw.Elapsed.TotalMilliseconds / 100):F0}ms/response, " +
                              $"OK={responses.Count(r => r.StatusCode == System.Net.HttpStatusCode.OK)}, " +
                              $"Errors={responses.Count(r => !r.IsSuccessStatusCode)}");

            return Response.Ok();
        })
        .WithWarmUpCount(5)
        .WithLoadSimulator(Simulator.Concurrency(100));
    }

    // ─── Test 2: Dashboard Data Retrieval (200 concurrent users) ───

    public static NBomberStep CreateDashboardLoadTest()
    {
        return Step.Create("dashboard_concurrent_200", async () =>
        {
            var sw = Stopwatch.StartNew();
            var tasks = new List<Task<HttpResponseMessage>>();

            for (int i = 0; i < 200; i++)
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/api/executive-dashboard/summary")
                {
                    Headers = { Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _adminAccessToken ?? "") }
                };

                tasks.Add(Http.SendAsync(request, HttpCompletionOption.ResponseContentRead));
            }

            var responses = await Task.WhenAll(tasks);
            sw.Stop();

            var okCount = responses.Count(r => r.IsSuccessStatusCode);
            var avgLatency = sw.Elapsed.TotalMilliseconds / 200;

            Console.WriteLine($"Dashboard 200 concurrent: Total={sw.Elapsed.TotalMilliseconds:F0}ms, " +
                              $"Avg={avgLatency:F0}ms/response, " +
                              $"OK={okCount}, Errors={200 - okCount}");

            return Response.Ok();
        })
        .WithWarmUpCount(10)
        .WithLoadSimulator(Simulator.Concurrency(200));
    }

    // ─── Test 3: Attendance Report Generation (50 concurrent requests) ───

    public static NBomberStep CreateReportGenerationLoadTest()
    {
        return Step.Create("report_generation_concurrent_50", async () =>
        {
            var sw = Stopwatch.StartNew();
            var tasks = new List<Task<HttpResponseMessage>>();

            var yesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

            for (int i = 0; i < 50; i++)
            {
                var request = new HttpRequestMessage(HttpMethod.Get,
                    $"{BaseUrl}/api/reports/attendance?date={yesterday}&type=daily")
                {
                    Headers = { Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _adminAccessToken ?? "") }
                };

                tasks.Add(Http.SendAsync(request, HttpCompletionOption.ResponseContentRead));
            }

            var responses = await Task.WhenAll(tasks);
            sw.Stop();

            var okCount = responses.Count(r => r.IsSuccessStatusCode);
            Console.WriteLine($"Report Gen 50 concurrent: Total={sw.Elapsed.TotalMilliseconds:F0}ms, " +
                              $"Avg={(sw.Elapsed.TotalMilliseconds / 50):F0}ms/response, " +
                              $"OK={okCount}, Errors={50 - okCount}");

            return Response.Ok();
        })
        .WithWarmUpCount(5)
        .WithLoadSimulator(Simulator.Concurrency(50));
    }

    // ─── Test 4: Escalation List Pagination Under Load ───

    public static NBomberStep CreateEscalationPaginationLoadTest()
    {
        return Step.Create("escalation_pagination_concurrent_50", async () =>
        {
            var sw = Stopwatch.StartNew();
            var tasks = new List<Task<HttpResponseMessage>>();

            for (int i = 0; i < 50; i++)
            {
                var page = (i % 10) + 1; // Distribute across pages 1-10
                var request = new HttpRequestMessage(HttpMethod.Get,
                    $"{BaseUrl}/api/escalations?page={page}&pageSize=20")
                {
                    Headers = { Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _adminAccessToken ?? "") }
                };

                tasks.Add(Http.SendAsync(request, HttpCompletionOption.ResponseContentRead));
            }

            var responses = await Task.WhenAll(tasks);
            sw.Stop();

            var okCount = responses.Count(r => r.IsSuccessStatusCode);
            Console.WriteLine($"Escalation Pagination 50 concurrent: Total={sw.Elapsed.TotalMilliseconds:F0}ms, " +
                              $"Avg={(sw.Elapsed.TotalMilliseconds / 50):F0}ms/response, " +
                              $"OK={okCount}, Errors={50 - okCount}");

            return Response.Ok();
        })
        .WithWarmUpCount(5)
        .WithLoadSimulator(Simulator.Concurrency(50));
    }

    // ─── Test 5: Warning Generation Throughput ───

    public static NBomberStep CreateWarningThroughputTest()
    {
        return Step.Create("warning_throughput_concurrent_25", async () =>
        {
            var sw = Stopwatch.StartNew();
            var tasks = new List<Task<HttpResponseMessage>>();

            for (int i = 0; i < 25; i++)
            {
                var payload = new
                {
                    employeeId = "707995",
                    warningType = "Late Arrival",
                    description = $"Auto-generated warning during load test #{i}",
                    issuedBy = "System Load Test",
                    exceptionId = 1
                };

                var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/api/warnings")
                {
                    Content = JsonContent.Create(payload, contentType: Microsoft.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json"), options: JsonOpts),
                    Headers = { Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _adminAccessToken ?? "") }
                };

                tasks.Add(Http.SendAsync(request, HttpCompletionOption.ResponseContentRead));
            }

            var responses = await Task.WhenAll(tasks);
            sw.Stop();

            var createdCount = responses.Count(r => r.StatusCode == System.Net.HttpStatusCode.Created);
            var conflictCount = responses.Count(r => r.StatusCode == System.Net.HttpStatusCode.Conflict);
            Console.WriteLine($"Warning Throughput 25 concurrent: Total={sw.Elapsed.TotalMilliseconds:F0}ms, " +
                              $"Created={createdCount}, Conflicts={conflictCount}, Errors={25 - createdCount - conflictCount}");

            return Response.Ok();
        })
        .WithWarmUpCount(3)
        .WithLoadSimulator(Simulator.Concurrency(25));
    }

    // ─── Test 6: Employee Search/List Under Load ───

    public static NBomberStep CreateEmployeeListLoadTest()
    {
        return Step.Create("employee_list_concurrent_100", async () =>
        {
            var sw = Stopwatch.StartNew();
            var tasks = new List<Task<HttpResponseMessage>>();

            for (int i = 0; i < 100; i++)
            {
                var keyword = i % 2 == 0 ? null : $"emp{i}"; // Mix of full list and filtered searches
                var request = new HttpRequestMessage(HttpMethod.Get,
                    keyword != null
                        ? $"{BaseUrl}/api/employees?searchKeyword={keyword}&page=1&pageSize=50"
                        : $"{BaseUrl}/api/employees?page=1&pageSize=50")
                {
                    Headers = { Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _adminAccessToken ?? "") }
                };

                tasks.Add(Http.SendAsync(request, HttpCompletionOption.ResponseContentRead));
            }

            var responses = await Task.WhenAll(tasks);
            sw.Stop();

            var okCount = responses.Count(r => r.IsSuccessStatusCode);
            Console.WriteLine($"Employee List 100 concurrent: Total={sw.Elapsed.TotalMilliseconds:F0}ms, " +
                              $"Avg={(sw.Elapsed.TotalMilliseconds / 100):F0}ms/response, " +
                              $"OK={okCount}, Errors={100 - okCount}");

            return Response.Ok();
        })
        .WithWarmUpCount(5)
        .WithLoadSimulator(Simulator.Concurrency(100));
    }

    // ─── Test 7: Database Connection Pool Exhaustion ───

    public static NBomberStep CreateConnectionPoolExhaustionTest()
    {
        return Step.Create("connection_pool_exhaustion", async () =>
        {
            var maxConcurrency = 500; // Attempt more than typical EF Core connection pool
            var sw = Stopwatch.StartNew();
            var results = new List<(bool Success, double LatencyMs)>();

            for (int i = 0; i < maxConcurrency; i++)
            {
                var task = Task.Run(async () =>
                {
                    var localSw = Stopwatch.StartNew();
                    try
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/api/employees?page=1&pageSize=10")
                        {
                            Headers = { Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _adminAccessToken ?? "") }
                        };

                        using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                        localSw.Stop();
                        return (response.IsSuccessStatusCode, localSw.Elapsed.TotalMilliseconds);
                    }
                    catch (Exception)
                    {
                        localSw.Stop();
                        return (false, localSw.Elapsed.TotalMilliseconds);
                    }
                });

                results.Add(await task);
            }

            sw.Stop();

            var successes = results.Count(r => r.Success);
            var failures = maxConcurrency - successes;
            var p50 = results.OrderBy(r => r.LatencyMs).Skip((int)(maxConcurrency * 0.50)).First().LatencyMs;
            var p95 = results.OrderBy(r => r.LatencyMs).Skip((int)(maxConcurrency * 0.95)).First().LatencyMs;
            var p99 = results.OrderBy(r => r.LatencyMs).Skip((int)(maxConcurrency * 0.99)).First().LatencyMs;

            Console.WriteLine($"Pool Exhaustion ({maxConcurrency} concurrent): " +
                              $"Success={successes}, Failures={failures}, " +
                              $"p50={p50:F0}ms, p95={p95:F0}ms, p99={p99:F0}ms, " +
                              $"Total={sw.Elapsed.TotalMilliseconds:F0}ms");

            return Response.Ok();
        })
        .WithWarmUpCount(10)
        .WithLoadSimulator(Simulator.Concurrency(500));
    }

    // ─── Statistical Analysis Helper ───────────────────────────────

    /// <summary>
    /// Calculate percentile statistics from a collection of latency values.
    /// </summary>
    public static (double P50, double P95, double P99, double Avg, double Min, double Max) CalculatePercentiles(List<double> latencies)
    {
        if (!latencies.Any()) throw new ArgumentException("No latency data provided.");

        var sorted = latencies.Order().ToList();
        var count = sorted.Count;

        return (
            P50: sorted[(int)(count * 0.50)],
            P95: sorted[Math.Min((int)(count * 0.95), count - 1)],
            P99: sorted[Math.Min((int)(count * 0.99), count - 1)],
            Avg: sorted.Average(),
            Min: sorted.First(),
            Max: sorted.Last()
        );
    }

    // ─── Main Runner ───────────────────────────────────────────────

    public static async Task RunAllAsync()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine("  WACAMS API Load Test Suite");
        Console.WriteLine($"  Started at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine("═══════════════════════════════════════════════════════");

        await InitializeAsync();

        var tests = new[]
        {
            ("Clock-In (100 concurrent)", CreateClockInLoadTest()),
            ("Dashboard (200 concurrent)", CreateDashboardLoadTest()),
            ("Report Generation (50 concurrent)", CreateReportGenerationLoadTest()),
            ("Escalation Pagination (50 concurrent)", CreateEscalationPaginationLoadTest()),
            ("Warning Throughput (25 concurrent)", CreateWarningThroughputTest()),
            ("Employee List (100 concurrent)", CreateEmployeeListLoadTest()),
            ("Connection Pool Exhaustion (500 concurrent)", CreateConnectionPoolExhaustionTest()),
        };

        foreach (var (name, step) in tests)
        {
            Console.WriteLine($"\n▶ Running: {name}");
            try
            {
                var scenario = ScenarioBuilder.CreateScenario(name, step)
                    .WithPlugins(new NBomberPlugins())
                    .WithOpt(maxDuration: TimeSpan.FromSeconds(60), warmUp: TimeSpan.FromSeconds(5))
                    .WithLoadSimulator(Simulator.Rps(10, TimeSpan.FromSeconds(30)));

                var result = await NBomberRunner.RunTracking(scenario);

                Console.WriteLine($"  ✓ {name} completed. Steps: {result?.Steps?.Count ?? 0}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ {name} FAILED: {ex.Message}");
            }
        }

        Console.WriteLine($"\n✅ All load tests finished at {DateTime.Now:HH:mm:ss}");
    }

    public static void Main(string[] args) => RunAllAsync().GetAwaiter().GetResult();
}

// Required DTOs for deserialization
public record LoginResponse
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTime ExpiresAt { get; init; }
}

public class NBomberPlugins : NBomber.Plugins.Base.IPlugin
{
    public System.Collections.Generic.IEnumerable<object> GetPlugins() => Array.Empty<object>();
}
