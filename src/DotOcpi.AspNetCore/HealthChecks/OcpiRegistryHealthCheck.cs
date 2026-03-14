using DotOcpi.Registry;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DotOcpi.AspNetCore.HealthChecks;

/// <summary>
/// Reports the overall health of the CPO registry.
/// Healthy when all CPOs are Connected, Degraded when some are Offline/Suspended,
/// Unhealthy when all are Offline or the registry is empty.
/// </summary>
public sealed class OcpiRegistryHealthCheck : IHealthCheck
{
    private readonly ICpoRegistry _registry;

    /// <summary>
    /// Creates a new registry health check.
    /// </summary>
    public OcpiRegistryHealthCheck(ICpoRegistry registry)
    {
        _registry = registry;
    }

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        var connections = _registry.GetAll();

        if (connections.Count == 0)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("No CPO connections registered."));
        }

        var connected = 0;
        var total = 0;

        foreach (var connection in connections)
        {
            if (connection.Status is ConnectionStatus.Unregistered)
            {
                continue;
            }

            total++;
            if (connection.Status is ConnectionStatus.Connected)
            {
                connected++;
            }
        }

        if (total == 0)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("No active CPO connections."));
        }

        if (connected == total)
        {
            return Task.FromResult(HealthCheckResult.Healthy($"All {connected} CPO connections are active."));
        }

        if (connected == 0)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy($"All {total} CPO connections are offline or suspended.")
            );
        }

        return Task.FromResult(HealthCheckResult.Degraded($"{connected}/{total} CPO connections are active."));
    }
}
