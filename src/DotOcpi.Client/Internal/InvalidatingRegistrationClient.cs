using DotOcpi.Registration;

namespace DotOcpi.Client.Internal;

/// <summary>
/// Wraps <see cref="IRegistrationClient"/> to automatically invalidate
/// the <see cref="ICpoConnectionContextProvider"/> cache after any
/// operation that mutates the CPO registry or rotates tokens.
/// Invalidation is post-only: during rotation the old token remains
/// valid per OCPI spec until the CPO processes the PUT credentials,
/// so pre-invalidation would only risk re-caching stale data if a
/// concurrent request resolves between pre-invalidation and rotation
/// completion.
/// </summary>
internal sealed class InvalidatingRegistrationClient : IRegistrationClient
{
    private readonly IRegistrationClient _inner;
    private readonly ICpoConnectionContextProvider _contextProvider;

    internal InvalidatingRegistrationClient(IRegistrationClient inner, ICpoConnectionContextProvider contextProvider)
    {
        _inner = inner;
        _contextProvider = contextProvider;
    }

    public async Task<RegistrationResult> RegisterAsync(
        RegistrationRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var result = await _inner.RegisterAsync(request, cancellationToken).ConfigureAwait(false);
        _contextProvider.Invalidate(result.Connection.ConnectionKey);
        return result;
    }

    public async Task<RegistrationResult> RotateCredentialsAsync(
        CredentialRotationRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var result = await _inner.RotateCredentialsAsync(request, cancellationToken).ConfigureAwait(false);
        _contextProvider.Invalidate(result.Connection.ConnectionKey);
        return result;
    }

    public async Task UnregisterAsync(UnregisterRequest request, CancellationToken cancellationToken = default)
    {
        await _inner.UnregisterAsync(request, cancellationToken).ConfigureAwait(false);
        _contextProvider.Invalidate(request.ConnectionKey);
    }
}
