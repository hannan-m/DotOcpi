namespace DotOcpi.Client.Internal;

/// <summary>
/// Resolves and caches per-CPO connection contexts (registry data + auth token).
/// Module clients call <see cref="ResolveAsync"/> once per operation instead of
/// independently querying <see cref="Registry.ICpoRegistry"/> and
/// <see cref="IOutboundTokenProvider"/> on every call.
/// </summary>
internal interface ICpoConnectionContextProvider
{
    /// <summary>
    /// Returns the cached context for <paramref name="cpoId"/>, or resolves
    /// it from the underlying registry and token provider on a cache miss.
    /// </summary>
    ValueTask<CpoConnectionContext> ResolveAsync(string cpoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a single CPO's cached context. The next <see cref="ResolveAsync"/>
    /// call for this CPO will re-query the registry and token provider.
    /// </summary>
    void Invalidate(string cpoId);

    /// <summary>
    /// Removes all cached contexts.
    /// </summary>
    void InvalidateAll();
}
