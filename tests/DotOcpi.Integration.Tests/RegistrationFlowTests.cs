using DotOcpi.Integration.Tests.Fixtures;
using DotOcpi.Registration;
using DotOcpi.Registry;
using DotOcpi.Security;
using DotOcpi.Simulator;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Integration.Tests;

public class RegistrationFlowTests : IntegrationTestBase
{
    [Fact]
    public async Task VersionDiscovery_FetchesVersions_FromTestServer()
    {
        using var httpClient = CreateHttpClient();
        var discovery = new VersionDiscovery(httpClient);

        var versions = await discovery.GetVersionsAsync($"{Server.BaseUrl}ocpi/versions", Server.TokenA);

        versions.Should().ContainSingle();
        versions[0].Version.Should().Be("2.2.1");
    }

    [Fact]
    public async Task VersionDiscovery_FetchesVersionDetail_WithEndpoints()
    {
        using var httpClient = CreateHttpClient();
        var discovery = new VersionDiscovery(httpClient);

        var versions = await discovery.GetVersionsAsync($"{Server.BaseUrl}ocpi/versions", Server.TokenA);

        var detail = await discovery.GetVersionDetailAsync(versions[0].Url, Server.TokenA);

        detail.Version.Should().Be("2.2.1");
        detail.Endpoints.Should().Contain(e => e.Identifier == "credentials");
        detail.Endpoints.Should().Contain(e => e.Identifier == "locations");
    }

    [Fact]
    public async Task CredentialsClient_PostCredentials_CompletesHandshake()
    {
        using var httpClient = CreateHttpClient();
        var discovery = new VersionDiscovery(httpClient);
        var credentialsClient = new CredentialsClient(httpClient);

        var versions = await discovery.GetVersionsAsync($"{Server.BaseUrl}ocpi/versions", Server.TokenA);

        var detail = await discovery.GetVersionDetailAsync(versions[0].Url, Server.TokenA);
        var credentialsUrl = detail.Endpoints.First(e => e.Identifier == "credentials").Url;

        var tokenB = TokenGenerator.Generate();
        var credentials = TestCredentialsHelper.V2_2_1(tokenB);

        var response = await credentialsClient.PostCredentialsAsync(
            credentialsUrl,
            Server.TokenA,
            OcpiVersion.V2_2_1,
            credentials
        );

        response.Token.Should().NotBeNullOrWhiteSpace();
        response.CountryCode.Should().Be("DE");
        response.PartyId.Should().Be("CPO");

        Server.GetIssuedTokenB().Should().Be(response.Token);
        Server.GetReceivedTokenC().Should().Be(tokenB);
    }

    [Fact]
    public async Task CredentialsClient_PutCredentials_RotatesTokens()
    {
        await PerformInitialRegistration();
        var firstTokenB = Server.GetIssuedTokenB();

        using var httpClient = CreateHttpClient();
        var credentialsClient = new CredentialsClient(httpClient);

        var newTokenC = TokenGenerator.Generate();
        var credentials = TestCredentialsHelper.V2_2_1(newTokenC);

        var response = await credentialsClient.PutCredentialsAsync(
            $"{Server.BaseUrl}ocpi/credentials",
            firstTokenB,
            OcpiVersion.V2_2_1,
            credentials
        );

        response.Token.Should().NotBe(firstTokenB);
        Server.GetReceivedTokenC().Should().Be(newTokenC);
    }

    [Fact]
    public async Task CredentialsClient_DeleteCredentials_Unregisters()
    {
        await PerformInitialRegistration();
        var tokenB = Server.GetIssuedTokenB();

        using var httpClient = CreateHttpClient();
        var credentialsClient = new CredentialsClient(httpClient);

        await credentialsClient.DeleteCredentialsAsync($"{Server.BaseUrl}ocpi/credentials", tokenB);

        Server.GetReceivedTokenC().Should().BeNull();
    }

    [Fact]
    public async Task FullHandshake_WithVersionNegotiation_ProducesValidConnection()
    {
        using var httpClient = CreateHttpClient();
        var discovery = new VersionDiscovery(httpClient);
        var credentialsClient = new CredentialsClient(httpClient);

        // Step 1: Discover versions
        var versions = await discovery.GetVersionsAsync($"{Server.BaseUrl}ocpi/versions", Server.TokenA);

        // Step 2: Negotiate — pick highest mutual
        var versionStrings = versions.Select(v => v.Version);
        var negotiated = VersionNegotiator.Negotiate(versionStrings);
        negotiated.Should().Be(OcpiVersion.V2_2_1);

        // Step 3: Get version detail
        var versionUrl = versions.First(v => v.Version == negotiated!.Value.ToVersionString()).Url;
        var detail = await discovery.GetVersionDetailAsync(versionUrl, Server.TokenA);

        // Step 4: Exchange credentials
        var credentialsUrl = detail.Endpoints.First(e => e.Identifier == "credentials").Url;
        var ourTokenB = TokenGenerator.Generate();
        var ourTokenBHash = TokenHasher.Hash(ourTokenB);

        var credentials = TestCredentialsHelper.V2_2_1(ourTokenB);

        var cpoResponse = await credentialsClient.PostCredentialsAsync(
            credentialsUrl,
            Server.TokenA,
            negotiated!.Value,
            credentials
        );

        // Step 5: Build connection
        var moduleEndpoints = detail
            .Endpoints.Where(e => e.Identifier != "credentials")
            .ToDictionary(e => e.Identifier, e => e.Url, StringComparer.OrdinalIgnoreCase);

        var connection = new CpoConnection
        {
            CpoCountryCode = cpoResponse.CountryCode,
            CpoPartyId = cpoResponse.PartyId,
            EmspCountryCode = "NL",
            EmspPartyId = "MSP",
            Version = negotiated!.Value,
            ModuleEndpoints = moduleEndpoints,
            TokenBHash = ourTokenBHash,
            CpoVersionsUrl = $"{Server.BaseUrl}ocpi/versions",
            EmspVersionsUrl = "https://emsp.example.com/ocpi/versions",
            Status = ConnectionStatus.Connected,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        connection.ConnectionKey.Should().Be("DE:CPO");
        connection.Version.Should().Be(OcpiVersion.V2_2_1);
        connection.Status.Should().Be(ConnectionStatus.Connected);
        connection.ModuleEndpoints.Should().ContainKey("locations");
        cpoResponse.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task MultiVersion_NegotiatesHighest()
    {
        await using var server = await OcpiCpoSimulator.CreateAsync(c =>
            c.SupportedVersions = [OcpiVersion.V2_0, OcpiVersion.V2_1_1, OcpiVersion.V2_2_1]
        );

        using var httpClient = new HttpClient { BaseAddress = server.BaseUrl };
        var discovery = new VersionDiscovery(httpClient);

        var versions = await discovery.GetVersionsAsync($"{server.BaseUrl}ocpi/versions", server.TokenA);

        versions.Should().HaveCount(3);

        var negotiated = VersionNegotiator.Negotiate(versions.Select(v => v.Version));
        negotiated.Should().Be(OcpiVersion.V2_2_1);
    }

    private async Task PerformInitialRegistration()
    {
        using var httpClient = CreateHttpClient();
        var credentialsClient = new CredentialsClient(httpClient);

        await credentialsClient.PostCredentialsAsync(
            $"{Server.BaseUrl}ocpi/credentials",
            Server.TokenA,
            OcpiVersion.V2_2_1,
            TestCredentialsHelper.V2_2_1(TokenGenerator.Generate())
        );
    }
}
