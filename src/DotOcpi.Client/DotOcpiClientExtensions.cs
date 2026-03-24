using DotOcpi.Client.Internal;
using DotOcpi.Client.Sync;
using DotOcpi.Registration;
using DotOcpi.Registry;
using DotOcpi.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotOcpi.Client;

/// <summary>
/// Extension methods for adding DotOcpi client services.
/// </summary>
public static class DotOcpiClientExtensions
{
    /// <summary>
    /// Adds the OCPI HTTP client, all module clients, and supporting infrastructure.
    /// A default <see cref="IOutboundTokenProvider"/> backed by <see cref="ITokenStore"/>
    /// is registered automatically. Consumers can override by registering their own
    /// <see cref="IOutboundTokenProvider"/> before calling this method.
    /// </summary>
    public static DotOcpiBuilder AddClient(this DotOcpiBuilder builder)
    {
        var httpClientBuilder = OcpiHttpClientConfiguration.AddOcpiHttpClient(builder.Services);

        // Default outbound token provider reads from ITokenStore and unprotects
        // via ITokenProtector. Consumer-registered IOutboundTokenProvider takes
        // precedence because TryAdd only registers if no prior registration exists.
        builder.Services.TryAddSingleton<IOutboundTokenProvider>(sp => new TokenStoreOutboundTokenProvider(
            sp.GetRequiredService<ITokenStore>(),
            sp.GetRequiredService<ITokenProtector>()
        ));

        // Connection context cache — resolves and caches CpoConnection + token
        // per CPO so module clients never hit the registry or token provider
        // more than once per CPO (until explicitly invalidated).
        builder.Services.AddSingleton<ICpoConnectionContextProvider>(sp => new CpoConnectionContextProvider(
            sp.GetRequiredService<ICpoRegistry>(),
            sp.GetRequiredService<IOutboundTokenProvider>()
        ));

        // Singleton HttpClient resolved from the factory. DNS rotation is handled
        // by SocketsHttpHandler.PooledConnectionLifetime (configured in
        // OcpiHttpClientConfiguration), so IHttpClientFactory handler rotation
        // is not needed and per-call CreateClient() would add needless allocations.
        builder.Services.AddSingleton(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            return factory.CreateClient(OcpiHttpClientConfiguration.HttpClientName);
        });

        // Registration infrastructure — CredentialsClient and VersionDiscovery
        // share the same HttpClient as the module clients.
        builder.Services.AddSingleton<ICredentialsClient>(sp => new CredentialsClient(
            sp.GetRequiredService<HttpClient>()
        ));
        builder.Services.AddSingleton<IVersionDiscovery>(sp => new VersionDiscovery(
            sp.GetRequiredService<HttpClient>()
        ));
        builder.Services.AddSingleton<IRegistrationClient, RegistrationOrchestrator>();

        // Module clients — internal types require factory lambdas
        builder.Services.AddSingleton<ILocationsClient>(sp => new LocationsClient(
            sp.GetRequiredService<HttpClient>(),
            sp.GetRequiredService<ICpoConnectionContextProvider>()
        ));
        builder.Services.AddSingleton<ISessionsClient>(sp => new SessionsClient(
            sp.GetRequiredService<HttpClient>(),
            sp.GetRequiredService<ICpoConnectionContextProvider>()
        ));
        builder.Services.AddSingleton<ICdrsClient>(sp => new CdrsClient(
            sp.GetRequiredService<HttpClient>(),
            sp.GetRequiredService<ICpoConnectionContextProvider>()
        ));
        builder.Services.AddSingleton<ITariffsClient>(sp => new TariffsClient(
            sp.GetRequiredService<HttpClient>(),
            sp.GetRequiredService<ICpoConnectionContextProvider>()
        ));
        builder.Services.AddSingleton<ITokensClient>(sp => new TokensClient(
            sp.GetRequiredService<HttpClient>(),
            sp.GetRequiredService<ICpoConnectionContextProvider>()
        ));
        builder.Services.AddSingleton<ICommandsClient>(sp => new CommandsClient(
            sp.GetRequiredService<HttpClient>(),
            sp.GetRequiredService<ICpoConnectionContextProvider>()
        ));
        builder.Services.AddSingleton<IChargingProfilesClient>(sp => new ChargingProfilesClient(
            sp.GetRequiredService<HttpClient>(),
            sp.GetRequiredService<ICpoConnectionContextProvider>()
        ));

        // Callback store
        builder.Services.AddSingleton<ICallbackStore>(new InMemoryCallbackStore());

        // Sync infrastructure
        builder.Services.AddSingleton<ISyncStateStore>(new InMemorySyncStateStore());
        builder.Services.AddSingleton<IOcpiSyncService>(sp => new OcpiSyncService(
            sp.GetRequiredService<ICpoRegistry>(),
            sp.GetRequiredService<ISyncStateStore>(),
            sp.GetService<IOcpiSyncHandler>(),
            sp.GetRequiredService<ICpoConnectionContextProvider>(),
            new PaginationHandler(sp.GetRequiredService<HttpClient>()),
            sp.GetRequiredService<TimeProvider>(),
            sp.GetRequiredService<ILogger<OcpiSyncService>>()
        ));

        // Facade
        builder.Services.AddSingleton<IOcpiClient>(sp => new OcpiClient(
            sp.GetRequiredService<IRegistrationClient>(),
            sp.GetRequiredService<IVersionDiscovery>(),
            sp.GetRequiredService<ILocationsClient>(),
            sp.GetRequiredService<ISessionsClient>(),
            sp.GetRequiredService<ICdrsClient>(),
            sp.GetRequiredService<ITariffsClient>(),
            sp.GetRequiredService<ITokensClient>(),
            sp.GetRequiredService<ICommandsClient>(),
            sp.GetRequiredService<IChargingProfilesClient>(),
            sp.GetRequiredService<ICpoConnectionContextProvider>()
        ));

        return builder;
    }

    /// <summary>
    /// Configures pull synchronization and registers the background service
    /// that periodically pulls data from connected CPOs.
    /// Consumers should register an <see cref="IOcpiSyncHandler"/> via
    /// <see cref="AddSyncHandler{THandler}"/> to receive pulled data.
    /// </summary>
    public static DotOcpiBuilder AddPullSync(this DotOcpiBuilder builder, Action<PullSyncOptions>? configure = null)
    {
        if (configure is not null)
        {
            builder.Services.Configure(configure);
        }

        builder.Services.AddSingleton<IValidateOptions<PullSyncOptions>, PullSyncOptionsValidator>();
        builder.Services.AddHostedService<OcpiPullSyncBackgroundService>();

        return builder;
    }

    /// <summary>
    /// Registers an <see cref="IOcpiSyncHandler"/> implementation that receives
    /// pulled OCPI data during sync operations.
    /// </summary>
    public static DotOcpiBuilder AddSyncHandler<THandler>(this DotOcpiBuilder builder)
        where THandler : class, IOcpiSyncHandler
    {
        builder.Services.AddSingleton<IOcpiSyncHandler, THandler>();
        return builder;
    }
}
