using DotOcpi.Registry;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotOcpi.Tests.Registry;

public class CpoHealthMonitorTests
{
    [Fact]
    public async Task ExecuteAsync_CompletesOnCancellation()
    {
        var registry = new InMemoryCpoRegistry();
        var monitor = new CpoHealthMonitor(
            registry,
            new HttpClient(),
            NullLogger<CpoHealthMonitor>.Instance,
            interval: TimeSpan.FromMilliseconds(50)
        );

        using var cts = new CancellationTokenSource();

        await monitor.StartAsync(cts.Token);
        await cts.CancelAsync();
        await monitor.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAsync_SkipsUnregisteredConnections()
    {
        var registry = new InMemoryCpoRegistry();
        registry.AddOrUpdate(
            new CpoConnection
            {
                CpoCountryCode = "DE",
                CpoPartyId = "ALL",
                EmspCountryCode = "NL",
                EmspPartyId = "TNM",
                Version = OcpiVersion.V2_2_1,
                ModuleEndpoints = new Dictionary<string, string>(),
                TokenBHash = "hash1",
                Status = ConnectionStatus.Unregistered,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            }
        );

        var monitor = new CpoHealthMonitor(
            registry,
            new HttpClient(),
            NullLogger<CpoHealthMonitor>.Instance,
            interval: TimeSpan.FromMilliseconds(50)
        );

        using var cts = new CancellationTokenSource();

        await monitor.StartAsync(cts.Token);
        await cts.CancelAsync();
        await monitor.StopAsync(CancellationToken.None);

        var conn = registry.FindByConnectionKey("DE:ALL");
        conn!.Status.Should().Be(ConnectionStatus.Unregistered);
    }

    [Fact]
    public void Constructor_WithDefaults_Succeeds()
    {
        var registry = new InMemoryCpoRegistry();
        var monitor = new CpoHealthMonitor(registry, new HttpClient(), NullLogger<CpoHealthMonitor>.Instance);

        monitor.Should().NotBeNull();
    }
}
