using DotOcpi.Client.Sync;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Client.Tests.Sync;

public class InMemorySyncStateStoreTests
{
    [Fact]
    public async Task GetLastSync_NoPriorSync_ReturnsNull()
    {
        var store = new InMemorySyncStateStore();

        var result = await store.GetLastSyncAsync("DE:ALL", "locations");

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAndGet_ReturnsStoredTimestamp()
    {
        var store = new InMemorySyncStateStore();
        var timestamp = new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.Zero);

        await store.SetLastSyncAsync("DE:ALL", "locations", timestamp);
        var result = await store.GetLastSyncAsync("DE:ALL", "locations");

        result.Should().Be(timestamp);
    }

    [Fact]
    public async Task SetAndGet_DifferentModules_Independent()
    {
        var store = new InMemorySyncStateStore();
        var time1 = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var time2 = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero);

        await store.SetLastSyncAsync("DE:ALL", "locations", time1);
        await store.SetLastSyncAsync("DE:ALL", "tariffs", time2);

        var locResult = await store.GetLastSyncAsync("DE:ALL", "locations");
        var tarResult = await store.GetLastSyncAsync("DE:ALL", "tariffs");

        locResult.Should().Be(time1);
        tarResult.Should().Be(time2);
    }

    [Fact]
    public async Task SetAndGet_DifferentCpos_Independent()
    {
        var store = new InMemorySyncStateStore();
        var time1 = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var time2 = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero);

        await store.SetLastSyncAsync("DE:ALL", "locations", time1);
        await store.SetLastSyncAsync("NL:TNM", "locations", time2);

        var deResult = await store.GetLastSyncAsync("DE:ALL", "locations");
        var nlResult = await store.GetLastSyncAsync("NL:TNM", "locations");

        deResult.Should().Be(time1);
        nlResult.Should().Be(time2);
    }

    [Fact]
    public async Task Set_OverwritesPreviousTimestamp()
    {
        var store = new InMemorySyncStateStore();
        var first = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var second = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero);

        await store.SetLastSyncAsync("DE:ALL", "locations", first);
        await store.SetLastSyncAsync("DE:ALL", "locations", second);

        var result = await store.GetLastSyncAsync("DE:ALL", "locations");
        result.Should().Be(second);
    }
}
