using DotOcpi.Registration;

namespace DotOcpi.Client;

/// <summary>
/// Default implementation of <see cref="IOcpiClient"/>.
/// All module clients share the same HttpClient and request builder.
/// </summary>
internal sealed class OcpiClient : IOcpiClient
{
    public OcpiClient(
        IRegistrationClient registration,
        IVersionDiscovery versions,
        ILocationsClient locations,
        ISessionsClient sessions,
        ICdrsClient cdrs,
        ITariffsClient tariffs,
        ITokensClient tokens,
        ICommandsClient commands,
        IChargingProfilesClient chargingProfiles
    )
    {
        Registration = registration;
        Versions = versions;
        Locations = locations;
        Sessions = sessions;
        Cdrs = cdrs;
        Tariffs = tariffs;
        Tokens = tokens;
        Commands = commands;
        ChargingProfiles = chargingProfiles;
    }

    public IRegistrationClient Registration { get; }
    public IVersionDiscovery Versions { get; }
    public ILocationsClient Locations { get; }
    public ISessionsClient Sessions { get; }
    public ICdrsClient Cdrs { get; }
    public ITariffsClient Tariffs { get; }
    public ITokensClient Tokens { get; }
    public ICommandsClient Commands { get; }
    public IChargingProfilesClient ChargingProfiles { get; }
}
