using System.ComponentModel.DataAnnotations;
using DotOcpi.Logging;

namespace DotOcpi;

/// <summary>
/// Root configuration for the DotOcpi library. Bound from configuration
/// or configured via the <c>AddDotOcpi</c> extension method.
/// </summary>
public sealed class DotOcpiOptions
{
    /// <summary>
    /// OCPI versions this eMSP supports. At least one version is required.
    /// Version negotiation will select the highest mutual version with each CPO.
    /// </summary>
    [Required]
    [MinLength(1)]
    public IReadOnlyList<OcpiVersion> SupportedVersions { get; set; } = [OcpiVersion.V2_2_1];

    /// <summary>
    /// Default eMSP identity used when no per-CPO identity is configured.
    /// Required when registering with CPOs via the automated handshake.
    /// </summary>
    public PartyIdentity? DefaultEmspIdentity { get; set; }

    /// <summary>
    /// Base URL for this eMSP's OCPI endpoints (e.g., <c>https://my-emsp.com/ocpi</c>).
    /// Must use HTTPS in production.
    /// </summary>
    public Uri? BaseUrl { get; set; }

    /// <summary>
    /// OCPI-specific logging options (body logging, token sanitization).
    /// </summary>
    public DotOcpiLoggingOptions Logging { get; set; } = new();

    /// <summary>
    /// Whether to run the background health monitor that probes CPO connections.
    /// </summary>
    public bool EnableHealthMonitoring { get; set; } = true;

    /// <summary>
    /// How often the health monitor probes CPO connections.
    /// </summary>
    public TimeSpan HealthMonitoringInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// How long a CPO connection can be idle before the health monitor probes it.
    /// </summary>
    public TimeSpan StaleConnectionThreshold { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Validates the options and returns any validation errors.
    /// </summary>
    internal IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (SupportedVersions is null || SupportedVersions.Count == 0)
        {
            errors.Add("At least one supported OCPI version must be configured.");
        }

        if (BaseUrl is not null && BaseUrl.Scheme != Uri.UriSchemeHttps)
        {
            errors.Add($"BaseUrl must use HTTPS. Got: {BaseUrl.Scheme}");
        }

        if (HealthMonitoringInterval <= TimeSpan.Zero)
        {
            errors.Add("HealthMonitoringInterval must be positive.");
        }

        if (StaleConnectionThreshold <= TimeSpan.Zero)
        {
            errors.Add("StaleConnectionThreshold must be positive.");
        }

        return errors;
    }
}
