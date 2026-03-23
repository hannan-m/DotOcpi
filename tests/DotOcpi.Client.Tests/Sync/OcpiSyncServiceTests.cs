using DotOcpi.Client.Internal;
using DotOcpi.Client.Sync;
using DotOcpi.Client.Tests.Internal;
using DotOcpi.Registry;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace DotOcpi.Client.Tests.Sync;

public class OcpiSyncServiceTests
{
    private static readonly DateTimeOffset SyncStart = new(2024, 6, 1, 12, 0, 0, TimeSpan.Zero);

    private static CpoConnection CreateConnection(string cpoId = "DE:ALL") =>
        new()
        {
            CpoCountryCode = cpoId.Split(':')[0],
            CpoPartyId = cpoId.Split(':')[1],
            EmspCountryCode = "NL",
            EmspPartyId = "EMS",
            Version = OcpiVersion.V2_2_1,
            ModuleEndpoints = new Dictionary<string, string>
            {
                ["locations"] = "https://cpo.example.com/ocpi/2.2.1/cpo/locations",
                ["tariffs"] = "https://cpo.example.com/ocpi/2.2.1/cpo/tariffs",
            },
            TokenBHash = "hash",
            Status = ConnectionStatus.Connected,
            CreatedAt = SyncStart.AddDays(-1),
            UpdatedAt = SyncStart.AddDays(-1),
        };

    private static (
        OcpiSyncService service,
        IOcpiSyncHandler handler,
        ISyncStateStore store,
        FakeTimeProvider time
    ) CreateService(CpoConnection? connection = null, string? responseJson = null)
    {
        var registry = Substitute.For<ICpoRegistry>();
        var conn = connection ?? CreateConnection();
        registry.FindByConnectionKey(conn.ConnectionKey).Returns(conn);

        var store = new InMemorySyncStateStore();
        var handler = Substitute.For<IOcpiSyncHandler>();
        handler
            .OnPageReceivedAsync(Arg.Any<SyncContext>(), Arg.Any<IReadOnlyList<object>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        handler
            .OnSyncCompletedAsync(Arg.Any<SyncContext>(), Arg.Any<SyncResult>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var contextProvider = Substitute.For<ICpoConnectionContextProvider>();
        contextProvider
            .ResolveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CpoConnectionContext { Connection = conn, RawToken = "raw-token-base64" });

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.EnqueueResponse(
            responseJson ?? TestJsonData.WrapList(TestJsonData.Location, TestJsonData.Location2),
            headers: new Dictionary<string, string> { ["X-Total-Count"] = "2" }
        );

        var httpClient = new HttpClient(mockHttp);
        var pagination = new PaginationHandler(httpClient);

        var time = new FakeTimeProvider(SyncStart);
        var logger = NullLogger<OcpiSyncService>.Instance;

        var service = new OcpiSyncService(registry, store, handler, contextProvider, pagination, time, logger);

        return (service, handler, store, time);
    }

    private static OcpiSyncService CreateServiceWithMockHttp(
        ICpoRegistry registry,
        CpoConnection conn,
        ISyncStateStore store,
        IOcpiSyncHandler? handler,
        MockHttpMessageHandler mockHttp,
        FakeTimeProvider? time = null
    )
    {
        var contextProvider = Substitute.For<ICpoConnectionContextProvider>();
        contextProvider
            .ResolveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CpoConnectionContext { Connection = conn, RawToken = "token" });

        return new OcpiSyncService(
            registry,
            store,
            handler,
            contextProvider,
            new PaginationHandler(new HttpClient(mockHttp)),
            time ?? new FakeTimeProvider(SyncStart),
            NullLogger<OcpiSyncService>.Instance
        );
    }

    [Fact]
    public async Task SyncModuleFromCpoAsync_DeliversPageToHandler()
    {
        var (service, handler, _, _) = CreateService();

        var result = await service.SyncModuleFromCpoAsync("DE:ALL", "locations");

        result.IsSuccess.Should().BeTrue();
        result.ItemCount.Should().Be(2);
        result.PageCount.Should().Be(1);

        await handler
            .Received(1)
            .OnPageReceivedAsync(
                Arg.Is<SyncContext>(c => c.CpoId == "DE:ALL" && c.ModuleId == "locations"),
                Arg.Is<IReadOnlyList<object>>(items => items.Count == 2),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task SyncModuleFromCpoAsync_CallsOnSyncCompleted()
    {
        var (service, handler, _, _) = CreateService();

        await service.SyncModuleFromCpoAsync("DE:ALL", "locations");

        await handler
            .Received(1)
            .OnSyncCompletedAsync(
                Arg.Is<SyncContext>(c => c.CpoId == "DE:ALL"),
                Arg.Is<SyncResult>(r => r.IsSuccess && r.ItemCount == 2),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task SyncModuleFromCpoAsync_UpdatesTimestampAfterSuccess()
    {
        var (service, _, store, _) = CreateService();

        await service.SyncModuleFromCpoAsync("DE:ALL", "locations");

        var lastSync = await store.GetLastSyncAsync("DE:ALL", "locations");
        lastSync.Should().Be(SyncStart);
    }

    [Fact]
    public async Task SyncModuleFromCpoAsync_InvalidModuleId_ReturnsFailure()
    {
        var (service, _, _, _) = CreateService();

        var result = await service.SyncModuleFromCpoAsync("DE:ALL", "commands");

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("commands");
        result.ErrorMessage.Should().Contain("not a pullable module");
    }

    [Fact]
    public async Task SyncModuleFromCpoAsync_CpoNotFound_ReturnsFailure()
    {
        var registry = Substitute.For<ICpoRegistry>();
        registry.FindByConnectionKey("UNKNOWN").Returns((CpoConnection?)null);

        var service = new OcpiSyncService(
            registry,
            new InMemorySyncStateStore(),
            null,
            Substitute.For<ICpoConnectionContextProvider>(),
            new PaginationHandler(new HttpClient()),
            new FakeTimeProvider(),
            NullLogger<OcpiSyncService>.Instance
        );

        var result = await service.SyncModuleFromCpoAsync("UNKNOWN", "locations");

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("UNKNOWN");
    }

    [Fact]
    public async Task SyncModuleFromCpoAsync_NullHandler_StillUpdatesTimestamp()
    {
        var registry = Substitute.For<ICpoRegistry>();
        var conn = CreateConnection();
        registry.FindByConnectionKey("DE:ALL").Returns(conn);

        var store = new InMemorySyncStateStore();
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.EnqueueResponse(TestJsonData.WrapList(TestJsonData.Location));

        var service = CreateServiceWithMockHttp(registry, conn, store, null, mockHttp);

        var result = await service.SyncModuleFromCpoAsync("DE:ALL", "locations");

        result.IsSuccess.Should().BeTrue();
        result.ItemCount.Should().Be(1);
        var lastSync = await store.GetLastSyncAsync("DE:ALL", "locations");
        lastSync.Should().Be(SyncStart);
    }

    [Fact]
    public async Task SyncModuleFromCpoAsync_OcpiError_ZeroItemsYielded()
    {
        var registry = Substitute.For<ICpoRegistry>();
        var conn = CreateConnection();
        registry.FindByConnectionKey("DE:ALL").Returns(conn);

        var handler = Substitute.For<IOcpiSyncHandler>();
        handler
            .OnSyncCompletedAsync(Arg.Any<SyncContext>(), Arg.Any<SyncResult>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.EnqueueResponse(TestJsonData.WrapError(), System.Net.HttpStatusCode.InternalServerError);

        var service = CreateServiceWithMockHttp(registry, conn, new InMemorySyncStateStore(), handler, mockHttp);

        var result = await service.SyncModuleFromCpoAsync("DE:ALL", "locations");

        result.IsSuccess.Should().BeTrue();
        result.ItemCount.Should().Be(0);
        await handler
            .DidNotReceive()
            .OnPageReceivedAsync(
                Arg.Any<SyncContext>(),
                Arg.Any<IReadOnlyList<object>>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task SyncModuleFromCpoAsync_SyncContextHasCorrectVersion()
    {
        var (service, handler, _, _) = CreateService();

        await service.SyncModuleFromCpoAsync("DE:ALL", "locations");

        await handler
            .Received()
            .OnPageReceivedAsync(
                Arg.Is<SyncContext>(c => c.Version == OcpiVersion.V2_2_1),
                Arg.Any<IReadOnlyList<object>>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task SyncModuleFromCpoAsync_UsesSinceParameter_OverridesStoredTimestamp()
    {
        var (service, handler, store, _) = CreateService();
        var customSince = new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero);

        await store.SetLastSyncAsync("DE:ALL", "locations", new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));

        await service.SyncModuleFromCpoAsync("DE:ALL", "locations", since: customSince);

        await handler
            .Received()
            .OnPageReceivedAsync(
                Arg.Is<SyncContext>(c => c.DateFrom == customSince),
                Arg.Any<IReadOnlyList<object>>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task SyncModuleFromCpoAsync_EmptyPage_HandlerNotCalledForEmptyItems()
    {
        var registry = Substitute.For<ICpoRegistry>();
        var conn = CreateConnection();
        registry.FindByConnectionKey("DE:ALL").Returns(conn);

        var handler = Substitute.For<IOcpiSyncHandler>();
        handler
            .OnSyncCompletedAsync(Arg.Any<SyncContext>(), Arg.Any<SyncResult>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.EnqueueResponse(TestJsonData.WrapEmpty());

        var service = CreateServiceWithMockHttp(registry, conn, new InMemorySyncStateStore(), handler, mockHttp);

        var result = await service.SyncModuleFromCpoAsync("DE:ALL", "locations");

        result.IsSuccess.Should().BeTrue();
        result.ItemCount.Should().Be(0);
        result.PageCount.Should().Be(1);

        await handler
            .DidNotReceive()
            .OnPageReceivedAsync(
                Arg.Any<SyncContext>(),
                Arg.Any<IReadOnlyList<object>>(),
                Arg.Any<CancellationToken>()
            );

        await handler
            .Received(1)
            .OnSyncCompletedAsync(Arg.Any<SyncContext>(), Arg.Any<SyncResult>(), Arg.Any<CancellationToken>());
    }

    private sealed class FakeTimeProvider(DateTimeOffset startTime) : TimeProvider
    {
        private DateTimeOffset _utcNow = startTime;

        public FakeTimeProvider()
            : this(DateTimeOffset.UtcNow) { }

        public override DateTimeOffset GetUtcNow() => _utcNow;

        internal void Advance(TimeSpan duration) => _utcNow += duration;
    }
}
