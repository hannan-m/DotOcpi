using DotOcpi.Exceptions;
using DotOcpi.Registration;
using DotOcpi.Registry;
using DotOcpi.Security;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace DotOcpi.Tests.Registration;

public class RegistrationOrchestratorTests
{
    private readonly IVersionDiscovery _discovery = Substitute.For<IVersionDiscovery>();
    private readonly ICredentialsClient _credentialsClient = Substitute.For<ICredentialsClient>();
    private readonly ICpoRegistry _registry = Substitute.For<ICpoRegistry>();
    private readonly ITokenStore _tokenStore = Substitute.For<ITokenStore>();
    private readonly RegistrationOrchestrator _orchestrator;

    public RegistrationOrchestratorTests()
    {
        _registry.AddOrUpdate(Arg.Any<CpoConnection>()).Returns(true);
        _orchestrator = new RegistrationOrchestrator(_discovery, _credentialsClient, _registry, _tokenStore);
    }

    private static RegistrationRequest CreateRequest(
        string versionsUrl = "https://cpo.example.com/ocpi/versions",
        string tokenA = "token-a",
        string emspCountryCode = "NL",
        string emspPartyId = "TNM",
        string emspVersionsUrl = "https://emsp.example.com/ocpi/versions",
        string emspBusinessName = "Test eMSP"
    ) => new(versionsUrl, tokenA, emspCountryCode, emspPartyId, emspVersionsUrl, emspBusinessName);

    private void SetupHappyPath(
        OcpiVersion version = OcpiVersion.V2_2_1,
        string cpoCountryCode = "DE",
        string cpoPartyId = "ALL"
    )
    {
        var versionString = version.ToVersionString();
        var versionUrl = $"https://cpo.example.com/ocpi/{versionString}";

        _discovery
            .GetVersionsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<VersionInfo> { new(versionString, versionUrl) });

        _discovery
            .GetVersionDetailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(
                new VersionDetailInfo(
                    versionString,
                    new List<EndpointInfo>
                    {
                        new("credentials", "SENDER", $"https://cpo.example.com/ocpi/{versionString}/credentials"),
                        new("locations", "SENDER", $"https://cpo.example.com/ocpi/{versionString}/locations"),
                        new("sessions", "SENDER", $"https://cpo.example.com/ocpi/{versionString}/sessions"),
                    }
                )
            );

        _credentialsClient
            .PostCredentialsAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<OcpiVersion>(),
                Arg.Any<object>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new CredentialsResponse(
                    "cpo-token-b",
                    "https://cpo.example.com/ocpi/versions",
                    cpoCountryCode,
                    cpoPartyId
                )
            );
    }

    [Fact]
    public async Task RegisterAsync_HappyPath_ReturnsConnectedCpo()
    {
        SetupHappyPath();
        var request = CreateRequest();

        var result = await _orchestrator.RegisterAsync(request);

        result.Connection.CpoCountryCode.Should().Be("DE");
        result.Connection.CpoPartyId.Should().Be("ALL");
        result.Connection.EmspCountryCode.Should().Be("NL");
        result.Connection.EmspPartyId.Should().Be("TNM");
        result.Connection.Version.Should().Be(OcpiVersion.V2_2_1);
        result.Connection.Status.Should().Be(ConnectionStatus.Connected);
        result.CpoToken.Should().Be("cpo-token-b");
    }

    [Fact]
    public async Task RegisterAsync_StoresTokenBHash()
    {
        SetupHappyPath();
        var request = CreateRequest();

        var result = await _orchestrator.RegisterAsync(request);

        result.Connection.TokenBHash.Should().NotBeNullOrEmpty();
        await _tokenStore
            .Received(1)
            .StoreAsync(result.Connection.TokenBHash, TokenPurpose.TokenB, "NL:TNM", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_AddsConnectionToRegistry()
    {
        SetupHappyPath();
        var request = CreateRequest();

        await _orchestrator.RegisterAsync(request);

        _registry
            .Received(1)
            .AddOrUpdate(
                Arg.Is<CpoConnection>(c =>
                    c.CpoCountryCode == "DE" && c.CpoPartyId == "ALL" && c.Status == ConnectionStatus.Connected
                )
            );
    }

    [Fact]
    public async Task RegisterAsync_StoresCpoVersionsUrl()
    {
        SetupHappyPath();
        var request = CreateRequest();

        var result = await _orchestrator.RegisterAsync(request);

        result.Connection.CpoVersionsUrl.Should().Be("https://cpo.example.com/ocpi/versions");
    }

    [Fact]
    public async Task RegisterAsync_StoresEmspVersionsUrl()
    {
        SetupHappyPath();
        var request = CreateRequest();

        var result = await _orchestrator.RegisterAsync(request);

        result.Connection.EmspVersionsUrl.Should().Be("https://emsp.example.com/ocpi/versions");
    }

    [Fact]
    public async Task RegisterAsync_ExcludesCredentialsFromModuleEndpoints()
    {
        SetupHappyPath();
        var request = CreateRequest();

        var result = await _orchestrator.RegisterAsync(request);

        result.Connection.ModuleEndpoints.Should().NotContainKey("credentials");
        result.Connection.ModuleEndpoints.Should().ContainKey("locations");
        result.Connection.ModuleEndpoints.Should().ContainKey("sessions");
    }

    [Fact]
    public async Task RegisterAsync_NegotiatesHighestVersion()
    {
        _discovery
            .GetVersionsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(
                new List<VersionInfo>
                {
                    new("2.0", "https://cpo.example.com/ocpi/2.0"),
                    new("2.1.1", "https://cpo.example.com/ocpi/2.1.1"),
                    new("2.2.1", "https://cpo.example.com/ocpi/2.2.1"),
                }
            );

        _discovery
            .GetVersionDetailAsync(
                "https://cpo.example.com/ocpi/2.2.1",
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new VersionDetailInfo(
                    "2.2.1",
                    new List<EndpointInfo>
                    {
                        new("credentials", "SENDER", "https://cpo.example.com/ocpi/2.2.1/credentials"),
                    }
                )
            );

        _credentialsClient
            .PostCredentialsAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<OcpiVersion>(),
                Arg.Any<object>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new CredentialsResponse("token", "https://cpo.example.com/ocpi/versions", "DE", "ALL"));

        var result = await _orchestrator.RegisterAsync(CreateRequest());

        result.Connection.Version.Should().Be(OcpiVersion.V2_2_1);
    }

    [Fact]
    public async Task RegisterAsync_HttpVersionsUrl_Throws()
    {
        var request = CreateRequest(versionsUrl: "http://cpo.example.com/ocpi/versions");

        var act = () => _orchestrator.RegisterAsync(request);

        await act.Should().ThrowAsync<OcpiRegistrationException>().WithMessage("*HTTPS*");
    }

    [Fact]
    public async Task RegisterAsync_HttpEmspVersionsUrl_Throws()
    {
        var request = CreateRequest(emspVersionsUrl: "http://emsp.example.com/ocpi/versions");

        var act = () => _orchestrator.RegisterAsync(request);

        await act.Should().ThrowAsync<OcpiRegistrationException>().WithMessage("*HTTPS*");
    }

    [Fact]
    public async Task RegisterAsync_NoMutualVersion_Throws()
    {
        _discovery
            .GetVersionsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<VersionInfo> { new("3.0", "https://cpo.example.com/ocpi/3.0") });

        var act = () => _orchestrator.RegisterAsync(CreateRequest());

        await act.Should().ThrowAsync<OcpiRegistrationException>().WithMessage("*No mutually supported*");
    }

    [Fact]
    public async Task RegisterAsync_NoCredentialsEndpoint_Throws()
    {
        _discovery
            .GetVersionsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<VersionInfo> { new("2.2.1", "https://cpo.example.com/ocpi/2.2.1") });

        _discovery
            .GetVersionDetailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(
                new VersionDetailInfo(
                    "2.2.1",
                    new List<EndpointInfo>
                    {
                        new("locations", "SENDER", "https://cpo.example.com/ocpi/2.2.1/locations"),
                    }
                )
            );

        var act = () => _orchestrator.RegisterAsync(CreateRequest());

        await act.Should().ThrowAsync<OcpiRegistrationException>().WithMessage("*credentials endpoint*");
    }

    [Fact]
    public async Task RegisterAsync_RegistryConflict_Throws()
    {
        SetupHappyPath();
        _registry.AddOrUpdate(Arg.Any<CpoConnection>()).Returns(false);

        var act = () => _orchestrator.RegisterAsync(CreateRequest());

        await act.Should().ThrowAsync<OcpiRegistrationException>().WithMessage("*concurrency conflict*");
    }

    [Fact]
    public async Task RegisterAsync_PostsCredentialsWithTokenA()
    {
        SetupHappyPath();
        var request = CreateRequest(tokenA: "my-token-a");

        await _orchestrator.RegisterAsync(request);

        await _credentialsClient
            .Received(1)
            .PostCredentialsAsync(
                Arg.Any<string>(),
                "my-token-a",
                OcpiVersion.V2_2_1,
                Arg.Any<object>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task RegisterAsync_V2_0_UsesCorrectVersion()
    {
        SetupHappyPath(OcpiVersion.V2_0);
        var request = CreateRequest();

        var result = await _orchestrator.RegisterAsync(request);

        result.Connection.Version.Should().Be(OcpiVersion.V2_0);
    }

    [Fact]
    public async Task RegisterAsync_SetsTimestamps()
    {
        SetupHappyPath();
        var before = DateTimeOffset.UtcNow;

        var result = await _orchestrator.RegisterAsync(CreateRequest());

        var after = DateTimeOffset.UtcNow;
        result.Connection.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        result.Connection.UpdatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }
}
