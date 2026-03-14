namespace DotOcpi.Logging;

/// <summary>
/// Category name constants for DotOcpi loggers. Consumers use these in
/// appsettings.json LogLevel filtering to control verbosity per subsystem.
/// </summary>
public static class LogCategories
{
    /// <summary>Library root category.</summary>
    public const string Root = "DotOcpi";

    /// <summary>Registration handshake, version discovery, token exchange.</summary>
    public const string Registration = "DotOcpi.Registration";

    /// <summary>Token validation and auth failures.</summary>
    public const string Auth = "DotOcpi.Auth";

    /// <summary>Token storage and retrieval.</summary>
    public const string TokenStore = "DotOcpi.TokenStore";

    /// <summary>Registry mutations and cache invalidation.</summary>
    public const string CpoRegistry = "DotOcpi.CpoRegistry";

    /// <summary>Outbound HTTP client root.</summary>
    public const string Client = "DotOcpi.Client";

    /// <summary>Version discovery calls.</summary>
    public const string ClientVersions = "DotOcpi.Client.Versions";

    /// <summary>Credentials client calls.</summary>
    public const string ClientCredentials = "DotOcpi.Client.Credentials";

    /// <summary>Locations client calls.</summary>
    public const string ClientLocations = "DotOcpi.Client.Locations";

    /// <summary>Sessions client calls.</summary>
    public const string ClientSessions = "DotOcpi.Client.Sessions";

    /// <summary>CDRs client calls.</summary>
    public const string ClientCdrs = "DotOcpi.Client.Cdrs";

    /// <summary>Tariffs client calls.</summary>
    public const string ClientTariffs = "DotOcpi.Client.Tariffs";

    /// <summary>Tokens push calls.</summary>
    public const string ClientTokens = "DotOcpi.Client.Tokens";

    /// <summary>Commands client calls.</summary>
    public const string ClientCommands = "DotOcpi.Client.Commands";

    /// <summary>Charging profiles client calls.</summary>
    public const string ClientChargingProfiles = "DotOcpi.Client.ChargingProfiles";

    /// <summary>Inbound request handling root.</summary>
    public const string Server = "DotOcpi.Server";

    /// <summary>Locations receiver endpoint handling.</summary>
    public const string ServerLocations = "DotOcpi.Server.Locations";

    /// <summary>Sessions receiver endpoint handling.</summary>
    public const string ServerSessions = "DotOcpi.Server.Sessions";

    /// <summary>CDRs receiver endpoint handling.</summary>
    public const string ServerCdrs = "DotOcpi.Server.Cdrs";

    /// <summary>Tariffs receiver endpoint handling.</summary>
    public const string ServerTariffs = "DotOcpi.Server.Tariffs";

    /// <summary>Tokens sender endpoint and authorize handling.</summary>
    public const string ServerTokens = "DotOcpi.Server.Tokens";

    /// <summary>Commands callback handling.</summary>
    public const string ServerCommands = "DotOcpi.Server.Commands";

    /// <summary>Charging profiles callback handling.</summary>
    public const string ServerChargingProfiles = "DotOcpi.Server.ChargingProfiles";

    /// <summary>Pull synchronization.</summary>
    public const string Sync = "DotOcpi.Sync";

    /// <summary>Health monitoring probes.</summary>
    public const string Health = "DotOcpi.Health";

    /// <summary>Model validation.</summary>
    public const string Validation = "DotOcpi.Validation";
}
