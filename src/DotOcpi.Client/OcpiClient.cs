using DotOcpi.Client.Internal;
using DotOcpi.Registration;

namespace DotOcpi.Client;

/// <summary>
/// Default implementation of <see cref="IOcpiClient"/>.
/// Wraps <see cref="IRegistrationClient"/> with automatic cache invalidation
/// so registration and credential operations keep the connection cache consistent.
/// </summary>
internal sealed class OcpiClient : IOcpiClient
{
    private readonly ICpoConnectionContextProvider _contextProvider;

    public OcpiClient(
        IRegistrationClient registration,
        IVersionDiscovery versions,
        ILocationsClient locations,
        ISessionsClient sessions,
        ICdrsClient cdrs,
        ITariffsClient tariffs,
        ITokensClient tokens,
        ICommandsClient commands,
        IChargingProfilesClient chargingProfiles,
        ICpoConnectionContextProvider contextProvider
    )
    {
        _contextProvider = contextProvider;
        Registration = new InvalidatingRegistrationClient(registration, contextProvider);
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

    public void InvalidateConnection(string cpoId) => _contextProvider.Invalidate(cpoId);

    public void InvalidateAllConnections() => _contextProvider.InvalidateAll();
}
