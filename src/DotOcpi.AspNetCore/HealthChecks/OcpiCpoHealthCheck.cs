using DotOcpi.Registry;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DotOcpi.AspNetCore.HealthChecks;

/// <summary>
/// Reports the health of a specific CPO connection.
/// Healthy when Connected, Degraded when Offline, Unhealthy when not found
/// or Suspended/Unregistered.
/// </summary>
public sealed class OcpiCpoHealthCheck : IHealthCheck
{
    private readonly ICpoRegistry _registry;
    private readonly string _connectionKey;

    /// <summary>
    /// Creates a health check for a specific CPO identified by connection key.
    /// </summary>
    public OcpiCpoHealthCheck(ICpoRegistry registry, string connectionKey)
    {
        _registry = registry;
        _connectionKey = connectionKey;
    }

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        var connection = _registry.FindByConnectionKey(_connectionKey);

        if (connection is null)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy($"CPO '{_connectionKey}' not found in registry."));
        }

        var data = new Dictionary<string, object>
        {
            ["connectionKey"] = connection.ConnectionKey,
            ["status"] = connection.Status.ToString(),
            ["version"] = connection.Version.ToVersionString(),
            ["cpoIdentity"] = $"{connection.CpoCountryCode}:{connection.CpoPartyId}",
        };
        if (connection.LastHealthCheckAt.HasValue)
            data["lastHealthCheck"] = connection.LastHealthCheckAt.Value.ToString("o");
        if (connection.CpoVersionsUrl is not null)
            data["versionsUrl"] = connection.CpoVersionsUrl;

        var result = connection.Status switch
        {
            ConnectionStatus.Connected => HealthCheckResult.Healthy(
                $"CPO '{_connectionKey}' is connected.",
                data: data
            ),
            ConnectionStatus.Offline => HealthCheckResult.Degraded($"CPO '{_connectionKey}' is offline.", data: data),
            ConnectionStatus.Pending => HealthCheckResult.Degraded(
                $"CPO '{_connectionKey}' registration is pending.",
                data: data
            ),
            ConnectionStatus.Suspended => HealthCheckResult.Unhealthy(
                $"CPO '{_connectionKey}' is suspended.",
                data: data
            ),
            ConnectionStatus.Unregistered => HealthCheckResult.Unhealthy(
                $"CPO '{_connectionKey}' is unregistered.",
                data: data
            ),
            _ => HealthCheckResult.Unhealthy(
                $"CPO '{_connectionKey}' has unknown status: {connection.Status}.",
                data: data
            ),
        };

        return Task.FromResult(result);
    }
}
