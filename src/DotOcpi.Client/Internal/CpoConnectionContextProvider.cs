using System.Collections.Concurrent;
using DotOcpi.Registry;

namespace DotOcpi.Client.Internal;

/// <summary>
/// Caches <see cref="CpoConnectionContext"/> per CPO in a
/// <see cref="ConcurrentDictionary{TKey,TValue}"/>. Cache entries live
/// until explicitly invalidated — connection metadata and tokens are
/// effectively static between registration/rotation events.
/// </summary>
/// <remarks>
/// <para>
/// <b>Concurrency:</b> Concurrent cache misses for the same CPO may
/// trigger parallel resolutions. This is by design — the alternative
/// (<c>Lazy&lt;Task&gt;</c>) captures the first caller's
/// <see cref="CancellationToken"/>, causing unrelated requests to fail
/// when one caller cancels. Since resolution happens at most once per
/// CPO per process lifetime, the duplicate work is negligible.
/// </para>
/// <para>
/// <b>Security note:</b> Raw outbound tokens (Token B/C issued by the CPO)
/// are held in managed-heap strings that cannot be zeroed. This is an
/// intentional trade-off: the token is required in plaintext for every
/// outbound HTTP request header, and caching avoids per-call round-trips
/// to potentially remote secret stores. The exposure window is bounded by
/// cache invalidation (automatic after registration/rotation, or manual
/// via <see cref="IOcpiClient.InvalidateConnection"/>).
/// </para>
/// </remarks>
internal sealed class CpoConnectionContextProvider : ICpoConnectionContextProvider
{
    private readonly ICpoRegistry _registry;
    private readonly IOutboundTokenProvider _tokenProvider;
    private readonly ConcurrentDictionary<string, CpoConnectionContext> _cache = new(StringComparer.OrdinalIgnoreCase);

    internal CpoConnectionContextProvider(ICpoRegistry registry, IOutboundTokenProvider tokenProvider)
    {
        _registry = registry;
        _tokenProvider = tokenProvider;
    }

    public ValueTask<CpoConnectionContext> ResolveAsync(string cpoId, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(cpoId, out var cached))
            return new ValueTask<CpoConnectionContext>(cached);

        return ResolveCoreAsync(cpoId, cancellationToken);
    }

    private async ValueTask<CpoConnectionContext> ResolveCoreAsync(string cpoId, CancellationToken cancellationToken)
    {
        var connection =
            _registry.FindByConnectionKey(cpoId)
            ?? throw new InvalidOperationException($"CPO '{cpoId}' not found in registry.");

        var token = await _tokenProvider.GetTokenAsync(cpoId, cancellationToken).ConfigureAwait(false);

        var context = new CpoConnectionContext { Connection = connection, RawToken = token };

        // TryAdd is safe: if another thread won the race, both contexts
        // are valid (same CPO, same token). The loser's context is discarded
        // but the caller still gets a correct result. GetOrAdd is not used
        // here because its factory is synchronous.
        return _cache.GetOrAdd(cpoId, context);
    }

    public void Invalidate(string cpoId) => _cache.TryRemove(cpoId, out _);

    public void InvalidateAll() => _cache.Clear();
}
