using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace DotOcpi.Client.Internal;

/// <summary>
/// Configures the named HttpClient for OCPI operations with standard resilience policies.
/// </summary>
public static class OcpiHttpClientConfiguration
{
    /// <summary>
    /// The named HttpClient used for all OCPI outbound requests.
    /// </summary>
    public const string HttpClientName = "OcpiClient";

    /// <summary>
    /// Registers the OCPI HttpClient with standard resilience handler (retry, circuit breaker, timeout).
    /// </summary>
    public static IHttpClientBuilder AddOcpiHttpClient(this IServiceCollection services)
    {
        var builder = services
            .AddHttpClient(
                HttpClientName,
                static client =>
                {
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                }
            )
            .ConfigurePrimaryHttpMessageHandler(static () =>
                new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                    MaxConnectionsPerServer = 20,
                }
            );

        builder.AddStandardResilienceHandler();

        return builder;
    }
}
