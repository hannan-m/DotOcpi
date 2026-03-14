using DotOcpi.Exceptions;
using DotOcpi.Registration;
using DotOcpi.Registry;
using DotOcpi.Security;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace DotOcpi.Tests.Registration;

public class CredentialRotationTests
{
    private readonly IVersionDiscovery _discovery = Substitute.For<IVersionDiscovery>();
    private readonly ICredentialsClient _credentialsClient = Substitute.For<ICredentialsClient>();
    private readonly ICpoRegistry _registry = Substitute.For<ICpoRegistry>();
    private readonly ITokenStore _tokenStore = Substitute.For<ITokenStore>();
    private readonly RegistrationOrchestrator _orchestrator;

    public CredentialRotationTests()
    {
        _registry.AddOrUpdate(Arg.Any<CpoConnection>()).Returns(true);
        _orchestrator = new RegistrationOrchestrator(_discovery, _credentialsClient, _registry, _tokenStore);
    }

    private static CpoConnection CreateExistingConnection(
        string connectionKey = "DE:ALL",
        OcpiVersion version = OcpiVersion.V2_2_1,
        string? cpoVersionsUrl = "https://cpo.example.com/ocpi/versions",
        string? emspVersionsUrl = "https://emsp.example.com/ocpi/versions"
    ) =>
        new()
        {
            CpoCountryCode = connectionKey.Split(':')[0],
            CpoPartyId = connectionKey.Split(':')[1],
            EmspCountryCode = "NL",
            EmspPartyId = "TNM",
            Version = version,
            ModuleEndpoints = new Dictionary<string, string>
            {
                ["locations"] = "https://cpo.example.com/ocpi/2.2.1/locations",
            },
            TokenBHash = "old-token-hash",
            CpoVersionsUrl = cpoVersionsUrl,
            EmspVersionsUrl = emspVersionsUrl,
            Status = ConnectionStatus.Connected,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1),
        };

    private void SetupRotationHappyPath(CpoConnection existing)
    {
        _registry.FindByConnectionKey(existing.ConnectionKey).Returns(existing);

        _discovery
            .GetVersionDetailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(
                new VersionDetailInfo(
                    existing.Version.ToVersionString(),
                    new List<EndpointInfo>
                    {
                        new("credentials", "SENDER", "https://cpo.example.com/ocpi/2.2.1/credentials"),
                        new("locations", "SENDER", "https://cpo.example.com/ocpi/2.2.1/locations"),
                    }
                )
            );

        _credentialsClient
            .PutCredentialsAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<OcpiVersion>(),
                Arg.Any<object>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new CredentialsResponse("cpo-new-token", "https://cpo.example.com/ocpi/versions", "DE", "ALL"));
    }

    [Fact]
    public async Task RotateCredentials_HappyPath_ReturnsUpdatedConnection()
    {
        var existing = CreateExistingConnection();
        SetupRotationHappyPath(existing);

        var result = await _orchestrator.RotateCredentialsAsync(
            new CredentialRotationRequest("DE:ALL", "current-cpo-token", "Test eMSP")
        );

        result.Connection.TokenBHash.Should().NotBe("old-token-hash");
        result.CpoToken.Should().Be("cpo-new-token");
        result.Connection.CpoCountryCode.Should().Be("DE");
    }

    [Fact]
    public async Task RotateCredentials_StoresNewTokenHash()
    {
        var existing = CreateExistingConnection();
        SetupRotationHappyPath(existing);

        var result = await _orchestrator.RotateCredentialsAsync(
            new CredentialRotationRequest("DE:ALL", "current-cpo-token", "Test eMSP")
        );

        await _tokenStore
            .Received(1)
            .StoreAsync(result.Connection.TokenBHash, TokenPurpose.TokenB, "NL:TNM", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RotateCredentials_RemovesOldTokenHash()
    {
        var existing = CreateExistingConnection();
        SetupRotationHappyPath(existing);

        await _orchestrator.RotateCredentialsAsync(
            new CredentialRotationRequest("DE:ALL", "current-cpo-token", "Test eMSP")
        );

        await _tokenStore.Received(1).RemoveAsync("old-token-hash", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RotateCredentials_PutsCredentialsWithCurrentToken()
    {
        var existing = CreateExistingConnection();
        SetupRotationHappyPath(existing);

        await _orchestrator.RotateCredentialsAsync(
            new CredentialRotationRequest("DE:ALL", "current-cpo-token", "Test eMSP")
        );

        await _credentialsClient
            .Received(1)
            .PutCredentialsAsync(
                "https://cpo.example.com/ocpi/2.2.1/credentials",
                "current-cpo-token",
                OcpiVersion.V2_2_1,
                Arg.Any<object>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task RotateCredentials_UpdatesRegistry()
    {
        var existing = CreateExistingConnection();
        SetupRotationHappyPath(existing);

        await _orchestrator.RotateCredentialsAsync(
            new CredentialRotationRequest("DE:ALL", "current-cpo-token", "Test eMSP")
        );

        _registry
            .Received(1)
            .AddOrUpdate(Arg.Is<CpoConnection>(c => c.TokenBHash != "old-token-hash" && c.CpoCountryCode == "DE"));
    }

    [Fact]
    public async Task RotateCredentials_ConnectionNotFound_Throws()
    {
        _registry.FindByConnectionKey("DE:ALL").Returns((CpoConnection?)null);

        var act = () => _orchestrator.RotateCredentialsAsync(new CredentialRotationRequest("DE:ALL", "token", "eMSP"));

        await act.Should().ThrowAsync<OcpiRegistrationException>().WithMessage("*No CPO connection found*");
    }

    [Fact]
    public async Task RotateCredentials_NoCpoVersionsUrl_Throws()
    {
        var existing = CreateExistingConnection(cpoVersionsUrl: null);
        _registry.FindByConnectionKey("DE:ALL").Returns(existing);

        var act = () => _orchestrator.RotateCredentialsAsync(new CredentialRotationRequest("DE:ALL", "token", "eMSP"));

        await act.Should().ThrowAsync<OcpiRegistrationException>().WithMessage("*no stored versions URL*");
    }

    [Fact]
    public async Task RotateCredentials_NoEmspVersionsUrl_Throws()
    {
        var existing = CreateExistingConnection(emspVersionsUrl: null);
        SetupRotationHappyPath(existing);
        _registry.FindByConnectionKey("DE:ALL").Returns(existing);

        var act = () => _orchestrator.RotateCredentialsAsync(new CredentialRotationRequest("DE:ALL", "token", "eMSP"));

        await act.Should().ThrowAsync<OcpiRegistrationException>().WithMessage("*no stored eMSP versions URL*");
    }

    [Fact]
    public async Task RotateCredentials_RegistryConflict_Throws()
    {
        var existing = CreateExistingConnection();
        SetupRotationHappyPath(existing);
        _registry.AddOrUpdate(Arg.Any<CpoConnection>()).Returns(false);

        var act = () => _orchestrator.RotateCredentialsAsync(new CredentialRotationRequest("DE:ALL", "token", "eMSP"));

        await act.Should().ThrowAsync<OcpiRegistrationException>().WithMessage("*concurrency conflict*");
    }

    [Fact]
    public async Task Unregister_HappyPath_RemovesFromRegistry()
    {
        var existing = CreateExistingConnection();
        _registry.FindByConnectionKey("DE:ALL").Returns(existing);

        _discovery
            .GetVersionDetailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(
                new VersionDetailInfo(
                    "2.2.1",
                    new List<EndpointInfo>
                    {
                        new("credentials", "SENDER", "https://cpo.example.com/ocpi/2.2.1/credentials"),
                    }
                )
            );

        await _orchestrator.UnregisterAsync(new UnregisterRequest("DE:ALL", "current-cpo-token"));

        _registry.Received(1).Remove("DE:ALL");
    }

    [Fact]
    public async Task Unregister_DeletesCredentialsWithToken()
    {
        var existing = CreateExistingConnection();
        _registry.FindByConnectionKey("DE:ALL").Returns(existing);

        _discovery
            .GetVersionDetailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(
                new VersionDetailInfo(
                    "2.2.1",
                    new List<EndpointInfo>
                    {
                        new("credentials", "SENDER", "https://cpo.example.com/ocpi/2.2.1/credentials"),
                    }
                )
            );

        await _orchestrator.UnregisterAsync(new UnregisterRequest("DE:ALL", "current-cpo-token"));

        await _credentialsClient
            .Received(1)
            .DeleteCredentialsAsync(
                "https://cpo.example.com/ocpi/2.2.1/credentials",
                "current-cpo-token",
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task Unregister_RemovesTokenFromStore()
    {
        var existing = CreateExistingConnection();
        _registry.FindByConnectionKey("DE:ALL").Returns(existing);

        _discovery
            .GetVersionDetailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(
                new VersionDetailInfo(
                    "2.2.1",
                    new List<EndpointInfo>
                    {
                        new("credentials", "SENDER", "https://cpo.example.com/ocpi/2.2.1/credentials"),
                    }
                )
            );

        await _orchestrator.UnregisterAsync(new UnregisterRequest("DE:ALL", "current-cpo-token"));

        await _tokenStore.Received(1).RemoveAsync("old-token-hash", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unregister_ConnectionNotFound_Throws()
    {
        _registry.FindByConnectionKey("DE:ALL").Returns((CpoConnection?)null);

        var act = () => _orchestrator.UnregisterAsync(new UnregisterRequest("DE:ALL", "token"));

        await act.Should().ThrowAsync<OcpiRegistrationException>().WithMessage("*No CPO connection found*");
    }

    [Fact]
    public async Task Unregister_NoCpoVersionsUrl_Throws()
    {
        var existing = CreateExistingConnection(cpoVersionsUrl: null);
        _registry.FindByConnectionKey("DE:ALL").Returns(existing);

        var act = () => _orchestrator.UnregisterAsync(new UnregisterRequest("DE:ALL", "token"));

        await act.Should().ThrowAsync<OcpiRegistrationException>().WithMessage("*no stored versions URL*");
    }
}
