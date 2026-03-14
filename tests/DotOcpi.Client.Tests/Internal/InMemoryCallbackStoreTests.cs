using DotOcpi.Client.Internal;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Client.Tests.Internal;

public class InMemoryCallbackStoreTests
{
    private static PendingCallback CreateCallback(string correlationId = "corr-1", DateTimeOffset? expiresAt = null) =>
        new(
            correlationId,
            "DE:ALL",
            "START_SESSION",
            DateTimeOffset.UtcNow,
            expiresAt ?? DateTimeOffset.UtcNow.AddMinutes(5)
        );

    [Fact]
    public async Task StoreAndRetrieve_ReturnsCallback()
    {
        var store = new InMemoryCallbackStore();
        var callback = CreateCallback();

        await store.StoreAsync("corr-1", callback, TimeSpan.FromMinutes(5));
        var result = await store.GetAndRemoveAsync("corr-1");

        result.Should().NotBeNull();
        result!.CorrelationId.Should().Be("corr-1");
        result.CpoId.Should().Be("DE:ALL");
        result.CommandType.Should().Be("START_SESSION");
    }

    [Fact]
    public async Task GetAndRemove_RemovesCallback()
    {
        var store = new InMemoryCallbackStore();
        await store.StoreAsync("corr-1", CreateCallback(), TimeSpan.FromMinutes(5));

        var first = await store.GetAndRemoveAsync("corr-1");
        var second = await store.GetAndRemoveAsync("corr-1");

        first.Should().NotBeNull();
        second.Should().BeNull();
    }

    [Fact]
    public async Task GetAndRemove_NonExistentKey_ReturnsNull()
    {
        var store = new InMemoryCallbackStore();

        var result = await store.GetAndRemoveAsync("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetExpired_ReturnsOnlyExpiredCallbacks()
    {
        var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var store = new InMemoryCallbackStore(fakeTime);

        var active = CreateCallback("active", fakeTime.GetUtcNow().AddMinutes(10));
        var expired = CreateCallback("expired", fakeTime.GetUtcNow().AddMinutes(-1));

        await store.StoreAsync("active", active, TimeSpan.FromMinutes(10));
        await store.StoreAsync("expired", expired, TimeSpan.FromMinutes(0));

        var result = await store.GetExpiredAsync();

        result.Should().HaveCount(1);
        result[0].CorrelationId.Should().Be("expired");
    }

    [Fact]
    public async Task GetExpired_RemovesExpiredCallbacks()
    {
        var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var store = new InMemoryCallbackStore(fakeTime);

        var expired = CreateCallback("expired", fakeTime.GetUtcNow().AddMinutes(-1));
        await store.StoreAsync("expired", expired, TimeSpan.FromMinutes(0));

        await store.GetExpiredAsync();
        var second = await store.GetExpiredAsync();

        second.Should().BeEmpty();
    }

    [Fact]
    public async Task GetExpired_NoExpired_ReturnsEmpty()
    {
        var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var store = new InMemoryCallbackStore(fakeTime);

        var active = CreateCallback("active", fakeTime.GetUtcNow().AddMinutes(10));
        await store.StoreAsync("active", active, TimeSpan.FromMinutes(10));

        var result = await store.GetExpiredAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Store_OverwritesExistingCallback()
    {
        var store = new InMemoryCallbackStore();
        var first = CreateCallback("corr-1");
        var second = new PendingCallback(
            "corr-1",
            "NL:TNM",
            "STOP_SESSION",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMinutes(5)
        );

        await store.StoreAsync("corr-1", first, TimeSpan.FromMinutes(5));
        await store.StoreAsync("corr-1", second, TimeSpan.FromMinutes(5));

        var result = await store.GetAndRemoveAsync("corr-1");
        result!.CpoId.Should().Be("NL:TNM");
        result.CommandType.Should().Be("STOP_SESSION");
    }

    /// <summary>
    /// Fake TimeProvider for testing time-dependent logic without real delays.
    /// </summary>
    private sealed class FakeTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        internal FakeTimeProvider(DateTimeOffset startTime)
        {
            _utcNow = startTime;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;

        internal void Advance(TimeSpan duration) => _utcNow += duration;
    }
}
