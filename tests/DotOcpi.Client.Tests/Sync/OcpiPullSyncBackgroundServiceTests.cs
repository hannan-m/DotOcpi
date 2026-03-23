using DotOcpi.Client.Sync;
using DotOcpi.Registry;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace DotOcpi.Client.Tests.Sync;

public class OcpiPullSyncBackgroundServiceTests
{
    private static readonly DateTimeOffset Now = new(2024, 6, 1, 12, 0, 0, TimeSpan.Zero);

    private static CpoConnection CreateConnection(
        string cpoCountry = "DE",
        string cpoParty = "ALL",
        ConnectionStatus status = ConnectionStatus.Connected,
        params string[] modules
    )
    {
        var endpoints = new Dictionary<string, string>();
        foreach (var m in modules)
            endpoints[m] = $"https://cpo.example.com/ocpi/2.2.1/cpo/{m}";

        return new CpoConnection
        {
            CpoCountryCode = cpoCountry,
            CpoPartyId = cpoParty,
            EmspCountryCode = "NL",
            EmspPartyId = "EMS",
            Version = OcpiVersion.V2_2_1,
            ModuleEndpoints = endpoints,
            TokenBHash = "hash",
            Status = status,
            CreatedAt = Now.AddDays(-1),
            UpdatedAt = Now.AddDays(-1),
        };
    }

    private static (
        OcpiPullSyncBackgroundService service,
        IOcpiSyncService syncService,
        ISyncStateStore store,
        FakeTimeProvider time
    ) CreateBackgroundService(
        PullSyncOptions options,
        params CpoConnection[] connections
    )
    {
        var registry = Substitute.For<ICpoRegistry>();
        registry.GetAll().Returns(connections);

        var syncService = Substitute.For<IOcpiSyncService>();
        syncService
            .SyncModuleFromCpoAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new SyncResult
            {
                ItemCount = 1,
                PageCount = 1,
                Duration = TimeSpan.FromSeconds(1),
                IsSuccess = true,
            });

        var store = new InMemorySyncStateStore();
        var time = new FakeTimeProvider(Now);
        var opts = Options.Create(options);

        var service = new OcpiPullSyncBackgroundService(
            syncService,
            registry,
            opts,
            store,
            time,
            NullLogger<OcpiPullSyncBackgroundService>.Instance
        );

        return (service, syncService, store, time);
    }

    [Fact]
    public async Task RunSyncCycleAsync_FirstRun_SyncsImmediately()
    {
        var conn = CreateConnection(modules: ["locations", "tariffs"]);
        var options = new PullSyncOptions
        {
            EnabledModules = ["locations", "tariffs"],
            DefaultInterval = TimeSpan.FromHours(1),
        };

        var (service, syncService, _, _) = CreateBackgroundService(options, conn);

        await service.RunSyncCycleAsync(CancellationToken.None);

        await syncService
            .Received(1)
            .SyncModuleFromCpoAsync("DE:ALL", "locations", null, Arg.Any<CancellationToken>());
        await syncService
            .Received(1)
            .SyncModuleFromCpoAsync("DE:ALL", "tariffs", null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunSyncCycleAsync_RecentSync_SkipsUntilIntervalElapsed()
    {
        var conn = CreateConnection(modules: ["locations"]);
        var options = new PullSyncOptions
        {
            EnabledModules = ["locations"],
            DefaultInterval = TimeSpan.FromHours(1),
            MaxJitter = TimeSpan.Zero,
        };

        var (service, syncService, store, _) = CreateBackgroundService(options, conn);

        // Simulate a sync that happened 30 minutes ago
        await store.SetLastSyncAsync("DE:ALL", "locations", Now.AddMinutes(-30));

        await service.RunSyncCycleAsync(CancellationToken.None);

        await syncService
            .DidNotReceive()
            .SyncModuleFromCpoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunSyncCycleAsync_IntervalElapsed_SyncsModule()
    {
        var conn = CreateConnection(modules: ["locations"]);
        var options = new PullSyncOptions
        {
            EnabledModules = ["locations"],
            DefaultInterval = TimeSpan.FromHours(1),
            MaxJitter = TimeSpan.Zero,
        };

        var (service, syncService, store, _) = CreateBackgroundService(options, conn);

        // Simulate a sync that happened 2 hours ago
        await store.SetLastSyncAsync("DE:ALL", "locations", Now.AddHours(-2));

        await service.RunSyncCycleAsync(CancellationToken.None);

        await syncService
            .Received(1)
            .SyncModuleFromCpoAsync("DE:ALL", "locations", null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunSyncCycleAsync_SkipsNonConnectedCpos()
    {
        var pending = CreateConnection("DE", "PND", ConnectionStatus.Pending, "locations");
        var offline = CreateConnection("DE", "OFF", ConnectionStatus.Offline, "locations");
        var connected = CreateConnection("DE", "CON", ConnectionStatus.Connected, "locations");
        var options = new PullSyncOptions
        {
            EnabledModules = ["locations"],
            DefaultInterval = TimeSpan.FromHours(1),
        };

        var (service, syncService, _, _) = CreateBackgroundService(options, pending, offline, connected);

        await service.RunSyncCycleAsync(CancellationToken.None);

        await syncService
            .Received(1)
            .SyncModuleFromCpoAsync("DE:CON", "locations", null, Arg.Any<CancellationToken>());
        await syncService
            .DidNotReceive()
            .SyncModuleFromCpoAsync("DE:PND", Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>());
        await syncService
            .DidNotReceive()
            .SyncModuleFromCpoAsync("DE:OFF", Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunSyncCycleAsync_SkipsModuleNotInEndpoints()
    {
        var conn = CreateConnection(modules: ["locations"]); // no tariffs endpoint
        var options = new PullSyncOptions
        {
            EnabledModules = ["locations", "tariffs"],
            DefaultInterval = TimeSpan.FromHours(1),
        };

        var (service, syncService, _, _) = CreateBackgroundService(options, conn);

        await service.RunSyncCycleAsync(CancellationToken.None);

        await syncService
            .Received(1)
            .SyncModuleFromCpoAsync("DE:ALL", "locations", null, Arg.Any<CancellationToken>());
        await syncService
            .DidNotReceive()
            .SyncModuleFromCpoAsync("DE:ALL", "tariffs", Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunSyncCycleAsync_PerCpoModuleOverride_UsesSpecificInterval()
    {
        var conn = CreateConnection(modules: ["locations", "cdrs"]);
        var options = new PullSyncOptions
        {
            EnabledModules = ["locations", "cdrs"],
            DefaultInterval = TimeSpan.FromHours(1),
            MaxJitter = TimeSpan.Zero,
            CpoOverrides =
            {
                ["DE:ALL"] = new CpoSyncOptions
                {
                    ModuleOverrides = { ["cdrs"] = new ModuleSyncOptions { Interval = TimeSpan.FromHours(4) } },
                },
            },
        };

        var (service, syncService, store, _) = CreateBackgroundService(options, conn);

        // Both synced 2 hours ago — locations should sync (1h interval), cdrs should not (4h interval)
        await store.SetLastSyncAsync("DE:ALL", "locations", Now.AddHours(-2));
        await store.SetLastSyncAsync("DE:ALL", "cdrs", Now.AddHours(-2));

        await service.RunSyncCycleAsync(CancellationToken.None);

        await syncService
            .Received(1)
            .SyncModuleFromCpoAsync("DE:ALL", "locations", null, Arg.Any<CancellationToken>());
        await syncService
            .DidNotReceive()
            .SyncModuleFromCpoAsync("DE:ALL", "cdrs", Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunSyncCycleAsync_PerCpoEnabledModules_OverridesGlobal()
    {
        var conn = CreateConnection(modules: ["locations", "sessions", "cdrs", "tariffs"]);
        var options = new PullSyncOptions
        {
            EnabledModules = ["locations", "tariffs"],
            DefaultInterval = TimeSpan.FromHours(1),
            CpoOverrides =
            {
                ["DE:ALL"] = new CpoSyncOptions
                {
                    EnabledModules = ["sessions"],
                },
            },
        };

        var (service, syncService, _, _) = CreateBackgroundService(options, conn);

        await service.RunSyncCycleAsync(CancellationToken.None);

        // Only sessions should sync for DE:ALL (CPO override)
        await syncService
            .Received(1)
            .SyncModuleFromCpoAsync("DE:ALL", "sessions", null, Arg.Any<CancellationToken>());
        await syncService
            .DidNotReceive()
            .SyncModuleFromCpoAsync("DE:ALL", "locations", Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>());
        await syncService
            .DidNotReceive()
            .SyncModuleFromCpoAsync("DE:ALL", "tariffs", Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunSyncCycleAsync_OneSyncFails_OthersContinue()
    {
        var conn1 = CreateConnection("DE", "CPO1", modules: ["locations"]);
        var conn2 = CreateConnection("DE", "CPO2", modules: ["locations"]);
        var options = new PullSyncOptions
        {
            EnabledModules = ["locations"],
            DefaultInterval = TimeSpan.FromHours(1),
        };

        var (service, syncService, _, _) = CreateBackgroundService(options, conn1, conn2);

        syncService
            .SyncModuleFromCpoAsync("DE:CPO1", "locations", null, Arg.Any<CancellationToken>())
            .Returns<SyncResult>(x => throw new HttpRequestException("Network error"));

        await service.RunSyncCycleAsync(CancellationToken.None);

        // CPO2 should still have been synced despite CPO1 failure
        await syncService
            .Received(1)
            .SyncModuleFromCpoAsync("DE:CPO2", "locations", null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ComputeJitter_Deterministic_SamePairSameResult()
    {
        var jitter1 = OcpiPullSyncBackgroundService.ComputeJitter(TimeSpan.FromMinutes(5), "DE:ALL", "locations");
        var jitter2 = OcpiPullSyncBackgroundService.ComputeJitter(TimeSpan.FromMinutes(5), "DE:ALL", "locations");

        jitter1.Should().Be(jitter2);
    }

    [Fact]
    public void ComputeJitter_DifferentPairs_DifferentJitter()
    {
        var jitter1 = OcpiPullSyncBackgroundService.ComputeJitter(TimeSpan.FromMinutes(5), "DE:ALL", "locations");
        var jitter2 = OcpiPullSyncBackgroundService.ComputeJitter(TimeSpan.FromMinutes(5), "NL:TNM", "locations");

        jitter1.Should().NotBe(jitter2);
    }

    [Fact]
    public void ComputeJitter_ZeroMaxJitter_ReturnsZero()
    {
        var jitter = OcpiPullSyncBackgroundService.ComputeJitter(TimeSpan.Zero, "DE:ALL", "locations");

        jitter.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void ComputeJitter_WithinBounds()
    {
        var maxJitter = TimeSpan.FromMinutes(5);
        var jitter = OcpiPullSyncBackgroundService.ComputeJitter(maxJitter, "DE:ALL", "locations");

        jitter.Should().BeGreaterOrEqualTo(TimeSpan.Zero);
        jitter.Should().BeLessThanOrEqualTo(maxJitter);
    }

    private sealed class FakeTimeProvider(DateTimeOffset startTime) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => startTime;
    }
}
