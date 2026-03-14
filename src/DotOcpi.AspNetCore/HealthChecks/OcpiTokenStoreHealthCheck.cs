using DotOcpi.Security;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DotOcpi.AspNetCore.HealthChecks;

/// <summary>
/// Verifies the token store is accessible by performing a probe lookup.
/// Uses a dummy hash that should not exist — success means the store responded
/// without throwing.
/// </summary>
public sealed class OcpiTokenStoreHealthCheck : IHealthCheck
{
    private readonly ITokenStore _tokenStore;

    // SHA-256 hash of empty string — will never match a real token
    private const string ProbeHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

    /// <summary>
    /// Creates a new token store health check.
    /// </summary>
    public OcpiTokenStoreHealthCheck(ITokenStore tokenStore)
    {
        _tokenStore = tokenStore;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await _tokenStore.FindAsync(ProbeHash, cancellationToken).ConfigureAwait(false);
            return HealthCheckResult.Healthy("Token store is accessible.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Token store is not accessible.", ex);
        }
    }
}
