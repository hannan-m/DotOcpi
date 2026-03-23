using DotOcpi.Registration;

namespace DotOcpi.Client;

/// <summary>
/// Facade for all outbound OCPI client operations.
/// Provides access to module-specific clients and registration services.
/// </summary>
public interface IOcpiClient
{
    /// <summary>Registration and credential management.</summary>
    IRegistrationClient Registration { get; }

    /// <summary>Manual version discovery and detail queries.</summary>
    IVersionDiscovery Versions { get; }

    /// <summary>Pull locations from CPOs.</summary>
    ILocationsClient Locations { get; }

    /// <summary>Pull sessions from CPOs and send charging preferences.</summary>
    ISessionsClient Sessions { get; }

    /// <summary>Pull CDRs from CPOs.</summary>
    ICdrsClient Cdrs { get; }

    /// <summary>Pull tariffs from CPOs.</summary>
    ITariffsClient Tariffs { get; }

    /// <summary>Push tokens to CPOs.</summary>
    ITokensClient Tokens { get; }

    /// <summary>Send commands to CPOs.</summary>
    ICommandsClient Commands { get; }

    /// <summary>Manage charging profiles on CPOs (2.2+ only).</summary>
    IChargingProfilesClient ChargingProfiles { get; }

    /// <summary>
    /// Invalidates the cached connection context for a CPO, forcing the next
    /// call to re-resolve from the registry and token provider. Call this after
    /// modifying the CPO registry or rotating tokens outside the library's own
    /// registration and credential operations.
    /// </summary>
    void InvalidateConnection(string cpoId);

    /// <summary>
    /// Invalidates all cached connection contexts.
    /// </summary>
    void InvalidateAllConnections();
}
