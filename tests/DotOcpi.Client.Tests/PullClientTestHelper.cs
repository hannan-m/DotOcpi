using DotOcpi.Client.Internal;
using DotOcpi.Registry;
using NSubstitute;

namespace DotOcpi.Client.Tests;

/// <summary>
/// Shared setup for pull client tests using MockHttpMessageHandler.
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
                },
            TokenBHash = "hash",
            Status = ConnectionStatus.Connected,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

    internal static OcpiHttpRequestBuilder CreateBuilder(CpoConnection connection)
    {
        var registry = Substitute.For<ICpoRegistry>();
        registry.FindByConnectionKey(connection.ConnectionKey).Returns(connection);
        return new OcpiHttpRequestBuilder(registry);
    }

    internal static IOutboundTokenProvider CreateTokenProvider(string token = "test-token")
    {
        var provider = Substitute.For<IOutboundTokenProvider>();
        provider.GetTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(token);
        return provider;
    }
}
