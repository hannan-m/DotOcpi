using DotOcpi.Client.Internal;
using DotOcpi.Registry;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace DotOcpi.Client.Tests.Internal;

public class CpoConnectionContextProviderTests
{
    private static readonly CpoConnection TestConnection = new()
    {
        CpoCountryCode = "DE",
        CpoPartyId = "ALL",
        EmspCountryCode = "NL",
        EmspPartyId = "TNM",
        Version = OcpiVersion.V2_2_1,
        ModuleEndpoints = new Dictionary<string, string>
        {
            ["locations"] = "https://cpo.example.com/ocpi/2.2.1/cpo/locations",
        },
        TokenBHash = "hash",
        Status = ConnectionStatus.Connected,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    private static (
        CpoConnectionContextProvider provider,
        ICpoRegistry registry,
        IOutboundTokenProvider tokenProvider
    ) CreateProvider()
    {
        var registry = Substitute.For<ICpoRegistry>();
        registry.FindByConnectionKey("DE:ALL").Returns(TestConnection);

        var tokenProvider = Substitute.For<IOutboundTokenProvider>();
        tokenProvider.GetTokenAsync("DE:ALL", Arg.Any<CancellationToken>()).Returns("test-token");

        var provider = new CpoConnectionContextProvider(registry, tokenProvider);
        return (provider, registry, tokenProvider);
    }

    [Fact]
    public async Task ResolveAsync_CacheMiss_QueriesRegistryAndTokenProvider()
    {
        var (provider, registry, tokenProvider) = CreateProvider();

        var context = await provider.ResolveAsync("DE:ALL");

        context.Connection.Should().Be(TestConnection);
        context.RawToken.Should().Be("test-token");
        registry.Received(1).FindByConnectionKey("DE:ALL");
        await tokenProvider.Received(1).GetTokenAsync("DE:ALL", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveAsync_CacheHit_DoesNotQueryRegistryAgain()
    {
        var (provider, registry, tokenProvider) = CreateProvider();

        await provider.ResolveAsync("DE:ALL");
        await provider.ResolveAsync("DE:ALL");

        registry.Received(1).FindByConnectionKey("DE:ALL");
        await tokenProvider.Received(1).GetTokenAsync("DE:ALL", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Invalidate_CausesNextResolveToQueryRegistryAgain()
    {
        var (provider, registry, tokenProvider) = CreateProvider();

        await provider.ResolveAsync("DE:ALL");
        provider.Invalidate("DE:ALL");
        await provider.ResolveAsync("DE:ALL");

        registry.Received(2).FindByConnectionKey("DE:ALL");
        await tokenProvider.Received(2).GetTokenAsync("DE:ALL", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvalidateAll_ClearsEntireCache()
    {
        var (provider, registry, _) = CreateProvider();

        await provider.ResolveAsync("DE:ALL");
        provider.InvalidateAll();
        await provider.ResolveAsync("DE:ALL");

        registry.Received(2).FindByConnectionKey("DE:ALL");
    }

    [Fact]
    public async Task ResolveAsync_CpoNotFound_ThrowsInvalidOperationException()
    {
        var registry = Substitute.For<ICpoRegistry>();
        registry.FindByConnectionKey("XX:YY").Returns((CpoConnection?)null);
        var tokenProvider = Substitute.For<IOutboundTokenProvider>();

        var provider = new CpoConnectionContextProvider(registry, tokenProvider);

        var act = async () => await provider.ResolveAsync("XX:YY");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*XX:YY*not found*");
    }

    [Fact]
    public async Task ResolveAsync_ReturnsSameInstanceOnCacheHit()
    {
        var (provider, _, _) = CreateProvider();

        var context1 = await provider.ResolveAsync("DE:ALL");
        var context2 = await provider.ResolveAsync("DE:ALL");

        context1.Should().BeSameAs(context2);
    }

    [Fact]
    public void Invalidate_NonexistentKey_DoesNotThrow()
    {
        var (provider, _, _) = CreateProvider();

        var act = () => provider.Invalidate("XX:YY");

        act.Should().NotThrow();
    }

    [Fact]
    public async Task ResolveAsync_CaseInsensitive_ReturnsSameCachedEntry()
    {
        var registry = Substitute.For<ICpoRegistry>();
        registry.FindByConnectionKey("DE:ALL").Returns(TestConnection);
        registry.FindByConnectionKey("de:all").Returns(TestConnection);

        var tokenProvider = Substitute.For<IOutboundTokenProvider>();
        tokenProvider.GetTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("test-token");

        var provider = new CpoConnectionContextProvider(registry, tokenProvider);

        var context1 = await provider.ResolveAsync("DE:ALL");
        var context2 = await provider.ResolveAsync("de:all");

        context1.Should().BeSameAs(context2);
        registry.Received(1).FindByConnectionKey(Arg.Any<string>());
    }

    [Fact]
    public async Task ResolveAsync_TokenProviderThrows_DoesNotCacheFaultedEntry()
    {
        var registry = Substitute.For<ICpoRegistry>();
        registry.FindByConnectionKey("DE:ALL").Returns(TestConnection);

        var tokenProvider = Substitute.For<IOutboundTokenProvider>();
        tokenProvider
            .GetTokenAsync("DE:ALL", Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("vault unreachable"));

        var provider = new CpoConnectionContextProvider(registry, tokenProvider);

        var act = async () => await provider.ResolveAsync("DE:ALL");
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*vault*");

        // After failure, reconfigure the mock to succeed
        tokenProvider.GetTokenAsync("DE:ALL", Arg.Any<CancellationToken>()).Returns("recovered-token");

        // Next call should retry (not serve a cached fault)
        var context = await provider.ResolveAsync("DE:ALL");
        context.RawToken.Should().Be("recovered-token");
    }

    [Fact]
    public async Task ResolveAsync_ConcurrentColdCacheMisses_AllCallersGetValidResult()
    {
        var registry = Substitute.For<ICpoRegistry>();
        registry.FindByConnectionKey("DE:ALL").Returns(TestConnection);

        var tokenProvider = Substitute.For<IOutboundTokenProvider>();
        tokenProvider.GetTokenAsync("DE:ALL", Arg.Any<CancellationToken>()).Returns("test-token");

        var provider = new CpoConnectionContextProvider(registry, tokenProvider);

        // Launch 10 concurrent resolves on a cold cache
        var tasks = Enumerable.Range(0, 10).Select(_ => provider.ResolveAsync("DE:ALL").AsTask()).ToArray();
        var results = await Task.WhenAll(tasks);

        // All callers got a valid context with correct data
        results
            .Should()
            .AllSatisfy(ctx =>
            {
                ctx.Connection.Should().Be(TestConnection);
                ctx.RawToken.Should().Be("test-token");
            });

        // After the race settles, subsequent calls return the cached entry
        var cached = await provider.ResolveAsync("DE:ALL");
        results.Should().Contain(ctx => ReferenceEquals(ctx, cached));
    }

    [Fact]
    public async Task ResolveAsync_CancellationDoesNotPoisonCache()
    {
        var registry = Substitute.For<ICpoRegistry>();
        registry.FindByConnectionKey("DE:ALL").Returns(TestConnection);

        var tokenProvider = new CancellationAwareTokenProvider();
        var provider = new CpoConnectionContextProvider(registry, tokenProvider);

        // Caller A cancels immediately — ResolveCoreAsync throws before caching
        using var ctsA = new CancellationTokenSource();
        ctsA.Cancel();

        var actA = async () => await provider.ResolveAsync("DE:ALL", ctsA.Token);
        await actA.Should().ThrowAsync<OperationCanceledException>();

        // Caller B with a valid token should still succeed (no poisoned cache entry)
        var context = await provider.ResolveAsync("DE:ALL");
        context.Connection.Should().Be(TestConnection);
        context.RawToken.Should().Be("real-token");
    }

    private sealed class CancellationAwareTokenProvider : IOutboundTokenProvider
    {
        public ValueTask<string> GetTokenAsync(string cpoId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new ValueTask<string>("real-token");
        }
    }
}
