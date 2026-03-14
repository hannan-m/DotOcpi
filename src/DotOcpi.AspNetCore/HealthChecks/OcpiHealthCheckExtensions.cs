using DotOcpi.Registry;
using DotOcpi.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DotOcpi.AspNetCore.HealthChecks;

/// <summary>
/// Extension methods for registering OCPI health checks.
/// </summary>
public static class OcpiHealthCheckExtensions
{
    /// <summary>
    /// Adds a health check that reports the overall status of the CPO registry.
    /// </summary>
    public static IHealthChecksBuilder AddOcpiRegistryHealthCheck(
        this IHealthChecksBuilder builder,
        string name = "ocpi-registry",
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null
    )
    {
        return builder.Add(
            new HealthCheckRegistration(
                name,
                sp => new OcpiRegistryHealthCheck(sp.GetRequiredService<ICpoRegistry>()),
                failureStatus,
                tags
            )
        );
    }

    /// <summary>
    /// Adds a health check for a specific CPO connection identified by connection key.
    /// </summary>
    public static IHealthChecksBuilder AddOcpiCpoHealthCheck(
        this IHealthChecksBuilder builder,
        string connectionKey,
        string? name = null,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null
    )
    {
        return builder.Add(
            new HealthCheckRegistration(
                name ?? $"ocpi-cpo-{connectionKey}",
                sp => new OcpiCpoHealthCheck(sp.GetRequiredService<ICpoRegistry>(), connectionKey),
                failureStatus,
                tags
            )
        );
    }

    /// <summary>
    /// Adds a health check that verifies the OCPI token store is accessible.
    /// </summary>
    public static IHealthChecksBuilder AddOcpiTokenStoreHealthCheck(
        this IHealthChecksBuilder builder,
        string name = "ocpi-tokens",
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null
    )
    {
        return builder.Add(
            new HealthCheckRegistration(
                name,
                sp => new OcpiTokenStoreHealthCheck(sp.GetRequiredService<ITokenStore>()),
                failureStatus,
                tags
            )
        );
    }
}
