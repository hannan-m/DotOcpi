using DotOcpi.Client.Sync;
using DotOcpi.Registration;
using DotOcpi.Registry;
using DotOcpi.Security;
using DotOcpi.Simulator;

namespace DotOcpi.Sample.Services;

/// <summary>
/// Auto-registers test CPOs on startup and triggers initial sync.
/// </summary>
public sealed partial class AutoRegistrationService(
    IHostApplicationLifetime lifetime,
    CpoSimulator cpoSim,
    IVersionDiscovery versionDiscovery,
    ICredentialsClient credentialsClient,
    ICpoRegistry registry,
    ITokenStore tokenStore,
    SampleTokenProvider tokenProvider,
    IOcpiSyncService syncService,
    ILogger<AutoRegistrationService> logger
) : BackgroundService
{
    private readonly ILogger _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lifetime.ApplicationStarted.Register(() => ready.SetResult());
        await ready.Task.ConfigureAwait(false);

        try
        {
            await RegisterAndSyncAsync(cpoSim.GermanCpo, stoppingToken).ConfigureAwait(false);
            await RegisterAndSyncAsync(cpoSim.FrenchCpo, stoppingToken).ConfigureAwait(false);
            LogInfo("Auto-registration complete. Dashboard ready.");
        }
        catch (Exception ex)
        {
            LogError(ex);
        }
    }

    private async Task RegisterAndSyncAsync(OcpiCpoSimulator cpo, CancellationToken ct)
    {
        var versionsUrl = $"{cpo.BaseUrl}ocpi/versions";
        var versions = await versionDiscovery.GetVersionsAsync(versionsUrl, cpo.TokenA, ct).ConfigureAwait(false);
        var negotiated = VersionNegotiator.Negotiate(versions.Select(v => v.Version))
            ?? throw new InvalidOperationException("No version match");
        var versionEntry = versions.First(v => v.Version == negotiated.ToVersionString());
        var detail = await versionDiscovery.GetVersionDetailAsync(versionEntry.Url, cpo.TokenA, ct).ConfigureAwait(false);

        var credentialsUrl = detail.Endpoints
            .First(e => string.Equals(e.Identifier, "credentials", StringComparison.OrdinalIgnoreCase)).Url;

        var ourTokenB = TokenGenerator.Generate();
        var ourTokenBHash = TokenHasher.Hash(ourTokenB);
        var credentials = CredentialsHelper.Build(ourTokenB, negotiated);

        var cpoResponse = await credentialsClient
            .PostCredentialsAsync(credentialsUrl, cpo.TokenA, negotiated, credentials, ct)
            .ConfigureAwait(false);

        await tokenStore.StoreAsync(ourTokenBHash, TokenPurpose.TokenB, "NL:MSP", ct).ConfigureAwait(false);

        var moduleEndpoints = detail.Endpoints
            .Where(e => !string.Equals(e.Identifier, "credentials", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(e => e.Identifier, e => e.Url, StringComparer.OrdinalIgnoreCase);

        var now = DateTimeOffset.UtcNow;
        var connection = new CpoConnection
        {
            CpoCountryCode = cpoResponse.CountryCode,
            CpoPartyId = cpoResponse.PartyId,
            EmspCountryCode = "NL",
            EmspPartyId = "MSP",
            Version = negotiated,
            ModuleEndpoints = moduleEndpoints,
            TokenBHash = ourTokenBHash,
            CpoVersionsUrl = versionsUrl,
            EmspVersionsUrl = "https://emsp.example.com/ocpi/versions",
            Status = ConnectionStatus.Connected,
            CreatedAt = now,
            UpdatedAt = now,
        };

        registry.AddOrUpdate(connection);
        tokenProvider.StoreToken(connection.ConnectionKey, cpoResponse.Token);
        LogRegistered(connection.ConnectionKey, negotiated.ToVersionString());

        await syncService.SyncFromCpoAsync(connection.ConnectionKey, cancellationToken: ct).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Auto-registered {CpoKey} (OCPI {Version})")]
    private partial void LogRegistered(string cpoKey, string version);

    [LoggerMessage(Level = LogLevel.Information, Message = "{Message}")]
    private partial void LogInfo(string message);

    [LoggerMessage(Level = LogLevel.Error, Message = "Auto-registration failed")]
    private partial void LogError(Exception ex);
}
