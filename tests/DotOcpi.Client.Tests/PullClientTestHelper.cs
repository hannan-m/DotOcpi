using DotOcpi.Client.Internal;
using DotOcpi.Registry;
using NSubstitute;

namespace DotOcpi.Client.Tests;

/// <summary>
/// Shared setup for client tests using MockHttpMessageHandler.
/// </summary>
internal static class PullClientTestHelper
{
    internal static CpoConnection CreateConnection(
        OcpiVersion version = OcpiVersion.V2_2_1,
        Dictionary<string, string>? endpoints = null
    ) =>
        new()
        {
            CpoCountryCode = "DE",
            CpoPartyId = "ALL",
            EmspCountryCode = "NL",
            EmspPartyId = "TNM",
            Version = version,
            ModuleEndpoints =
                endpoints
                ?? new Dictionary<string, string>
                {
                    ["locations"] = "https://cpo.example.com/ocpi/2.2.1/cpo/locations",
                    ["sessions"] = "https://cpo.example.com/ocpi/2.2.1/cpo/sessions",
                    ["cdrs"] = "https://cpo.example.com/ocpi/2.2.1/cpo/cdrs",
                    ["tariffs"] = "https://cpo.example.com/ocpi/2.2.1/cpo/tariffs",
                    ["tokens"] = "https://cpo.example.com/ocpi/2.2.1/cpo/tokens",
                    ["commands"] = "https://cpo.example.com/ocpi/2.2.1/cpo/commands",
                    ["chargingprofiles"] = "https://cpo.example.com/ocpi/2.2.1/cpo/chargingprofiles",
                },
            TokenBHash = "hash",
            Status = ConnectionStatus.Connected,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

    internal static ICpoConnectionContextProvider CreateContextProvider(
        CpoConnection connection,
        string token = "test-token"
    )
    {
        var provider = Substitute.For<ICpoConnectionContextProvider>();
        var context = new CpoConnectionContext { Connection = connection, RawToken = token };
        provider.ResolveAsync(connection.ConnectionKey, Arg.Any<CancellationToken>()).Returns(context);
        return provider;
    }
}
