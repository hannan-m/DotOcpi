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

        var result = connection.Status switch
        {
            ConnectionStatus.Connected => HealthCheckResult.Healthy($"CPO '{_connectionKey}' is connected."),
            ConnectionStatus.Offline => HealthCheckResult.Degraded($"CPO '{_connectionKey}' is offline."),
            ConnectionStatus.Pending => HealthCheckResult.Degraded($"CPO '{_connectionKey}' registration is pending."),
            ConnectionStatus.Suspended => HealthCheckResult.Unhealthy($"CPO '{_connectionKey}' is suspended."),
            ConnectionStatus.Unregistered => HealthCheckResult.Unhealthy($"CPO '{_connectionKey}' is unregistered."),
            _ => HealthCheckResult.Unhealthy($"CPO '{_connectionKey}' has unknown status: {connection.Status}."),
        };

        return Task.FromResult(result);
    }
}
