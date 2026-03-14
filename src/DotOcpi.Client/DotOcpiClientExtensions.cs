using DotOcpi.Client.Internal;
using DotOcpi.Client.Sync;
using DotOcpi.Registry;
using Microsoft.Extensions.DependencyInjection;

namespace DotOcpi.Client;

/// <summary>
/// Extension methods for adding DotOcpi client services.
/// </summary>
public static class DotOcpiClientExtensions
{
    /// <summary>
    /// Adds the OCPI HTTP client, all module clients, and supporting infrastructure.
    /// Consumers must also register an <see cref="IOutboundTokenProvider"/> implementation.
    /// </summary>
    public static DotOcpiBuilder AddClient(this DotOcpiBuilder builder)
    {
        var httpClientBuilder = OcpiHttpClientConfiguration.AddOcpiHttpClient(builder.Services);

        // Internal infrastructure
        builder.Services.AddSingleton<OcpiHttpRequestBuilder>(sp => new OcpiHttpRequestBuilder(
            sp.GetRequiredService<ICpoRegistry>()
        ));

        builder.Services.AddSingleton(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            return factory.CreateClient(OcpiHttpClientConfiguration.HttpClientName);
        });

        // Module clients
        builder.Services.AddSingleton<ILocationsClient, LocationsClient>();
        builder.Services.AddSingleton<ISessionsClient, SessionsClient>();
        builder.Services.AddSingleton<ICdrsClient, CdrsClient>();
        builder.Services.AddSingleton<ITariffsClient, TariffsClient>();
        builder.Services.AddSingleton<ITokensClient, TokensClient>();
        builder.Services.AddSingleton<ICommandsClient, CommandsClient>();
        builder.Services.AddSingleton<IChargingProfilesClient, ChargingProfilesClient>();

        // Callback store
        builder.Services.AddSingleton<ICallbackStore, InMemoryCallbackStore>();

        // Sync state
        builder.Services.AddSingleton<ISyncStateStore, InMemorySyncStateStore>();

        // Facade
        builder.Services.AddSingleton<IOcpiClient, OcpiClient>();

        return builder;
    }

    /// <summary>
    /// Configures pull synchronization with custom options.
    /// </summary>
    public static DotOcpiBuilder AddPullSync(this DotOcpiBuilder builder, Action<PullSyncOptions>? configure = null)
    {
        if (configure is not null)
        {
            builder.Services.Configure(configure);
        }

        return builder;
    }
}
