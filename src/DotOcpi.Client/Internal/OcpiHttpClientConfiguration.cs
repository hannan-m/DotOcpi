using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace DotOcpi.Client.Internal;

/// <summary>
/// Configures the named HttpClient for OCPI operations with standard
/// resilience policies and SSRF prevention via DNS-level validation.
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
                    ConnectCallback = ValidateAndConnectAsync,
                }
            );

        // Disable handler rotation — the singleton HttpClient in DotOcpiClientExtensions
        // captures the handler chain once. SocketsHttpHandler.PooledConnectionLifetime
        // handles DNS rotation, so factory-level rotation would only orphan the resilience state.
        builder.SetHandlerLifetime(Timeout.InfiniteTimeSpan);

        builder.AddStandardResilienceHandler();

        return builder;
    }

    /// <summary>
    /// SocketsHttpHandler ConnectCallback that resolves DNS and validates the
    /// resulting IP addresses against private/loopback/link-local ranges before
    /// establishing a connection. Blocks SSRF attacks from CPO-provided URLs.
    /// </summary>
    private static async ValueTask<Stream> ValidateAndConnectAsync(
        SocketsHttpConnectionContext context,
        CancellationToken cancellationToken
    )
    {
        var host = context.DnsEndPoint.Host;
        var port = context.DnsEndPoint.Port;

        var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken).ConfigureAwait(false);

        if (addresses.Length == 0)
            throw new InvalidOperationException($"DNS resolution for '{host}' returned no addresses.");

        SsrfGuard.Validate(addresses, host, port);

        // Connect to the first resolved address that succeeds
        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };

        try
        {
            await socket.ConnectAsync(addresses, port, cancellationToken).ConfigureAwait(false);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}
