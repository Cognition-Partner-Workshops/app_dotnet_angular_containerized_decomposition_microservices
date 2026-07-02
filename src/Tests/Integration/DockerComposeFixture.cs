using System.Diagnostics;
using System.Net;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// xUnit collection fixture that starts the Docker Compose stack before tests
/// and tears it down afterward. Health checks are polled to ensure all services
/// are ready before any test runs.
/// </summary>
public class DockerComposeFixture : IAsyncLifetime
{
    private readonly string _composeFile;

    public HttpClient ProductClient { get; }
    public HttpClient OrderClient { get; }
    public HttpClient NotificationClient { get; }

    public DockerComposeFixture()
    {
        var srcDir = FindSrcDirectory();
        _composeFile = Path.Combine(srcDir, "docker-compose.integration.yml");

        ProductClient = new HttpClient { BaseAddress = new Uri("http://localhost:5004") };
        OrderClient = new HttpClient { BaseAddress = new Uri("http://localhost:5003") };
        NotificationClient = new HttpClient { BaseAddress = new Uri("http://localhost:5005") };
    }

    public async Task InitializeAsync()
    {
        // Build and start the stack
        await RunDockerCompose("up --build -d");

        // Wait for all services to be healthy
        await WaitForHealthy(ProductClient, "Product", "/healthz");
        await WaitForHealthy(OrderClient, "Order", "/healthz");
        await WaitForHealthy(NotificationClient, "Notification", "/healthz");
    }

    public async Task DisposeAsync()
    {
        ProductClient.Dispose();
        OrderClient.Dispose();
        NotificationClient.Dispose();

        await RunDockerCompose("down -v --remove-orphans");
    }

    private async Task RunDockerCompose(string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"compose -f {_composeFile} {args}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi)!;
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var stderr = await process.StandardError.ReadToEndAsync();
            var stdout = await process.StandardOutput.ReadToEndAsync();
            throw new InvalidOperationException(
                $"docker compose {args} failed (exit {process.ExitCode}):\n{stderr}\n{stdout}");
        }
    }

    private static async Task WaitForHealthy(HttpClient client, string serviceName, string healthPath)
    {
        var timeout = TimeSpan.FromMinutes(3);
        var interval = TimeSpan.FromSeconds(3);
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            try
            {
                var response = await client.GetAsync(healthPath);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Console.WriteLine($"[IntegrationTests] {serviceName} service is healthy");
                    return;
                }
            }
            catch
            {
                // Service not yet reachable
            }

            await Task.Delay(interval);
        }

        throw new TimeoutException($"{serviceName} service did not become healthy within {timeout.TotalSeconds}s");
    }

    private static string FindSrcDirectory()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir, "docker-compose.integration.yml")))
                return dir;
            if (File.Exists(Path.Combine(dir, "Microservices.sln")))
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }

        // Fallback: relative to repo root
        var repoRoot = Environment.GetEnvironmentVariable("REPO_ROOT")
            ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        return repoRoot;
    }
}

[CollectionDefinition("DockerCompose")]
public class DockerComposeCollection : ICollectionFixture<DockerComposeFixture>
{
}
