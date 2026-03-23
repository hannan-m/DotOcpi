using DotOcpi.Client.Internal;
using DotOcpi.Registration;
using DotOcpi.Registry;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace DotOcpi.Client.Tests.Internal;

public class InvalidatingRegistrationClientTests
{
    private static readonly CpoConnection TestConnection = new()
    {
        CpoCountryCode = "DE",
        CpoPartyId = "ALL",
        EmspCountryCode = "NL",
        EmspPartyId = "TNM",
        Version = OcpiVersion.V2_2_1,
        ModuleEndpoints = new Dictionary<string, string>
        {
            ["locations"] = "https://cpo.example.com/ocpi/2.2.1/cpo/locations",
        },
        TokenBHash = "hash",
        Status = ConnectionStatus.Connected,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    private static readonly RegistrationResult TestResult = new(TestConnection, "cpo-token");

    private static (
        InvalidatingRegistrationClient client,
        IRegistrationClient inner,
        ICpoConnectionContextProvider contextProvider
    ) CreateClient()
    {
        var inner = Substitute.For<IRegistrationClient>();
        var contextProvider = Substitute.For<ICpoConnectionContextProvider>();
        var client = new InvalidatingRegistrationClient(inner, contextProvider);
        return (client, inner, contextProvider);
    }

    [Fact]
    public async Task RegisterAsync_InvalidatesAfterSuccess()
    {
        var (client, inner, contextProvider) = CreateClient();
        inner.RegisterAsync(Arg.Any<RegistrationRequest>(), Arg.Any<CancellationToken>()).Returns(TestResult);

        var request = new RegistrationRequest(
            "https://cpo.example.com/ocpi/versions",
            "tokenA",
            "NL",
            "TNM",
            "https://emsp.example.com/ocpi/versions",
            "TestMSP"
        );

        var result = await client.RegisterAsync(request);

        result.Should().Be(TestResult);
        contextProvider.Received(1).Invalidate("DE:ALL");
    }

    [Fact]
    public async Task RegisterAsync_InnerThrows_DoesNotInvalidate()
    {
        var (client, inner, contextProvider) = CreateClient();
        inner
            .RegisterAsync(Arg.Any<RegistrationRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("handshake failed"));

        var request = new RegistrationRequest(
            "https://cpo.example.com/ocpi/versions",
            "tokenA",
            "NL",
            "TNM",
            "https://emsp.example.com/ocpi/versions",
            "TestMSP"
        );

        var act = async () => await client.RegisterAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>();
        contextProvider.DidNotReceive().Invalidate(Arg.Any<string>());
    }

    [Fact]
    public async Task RotateCredentialsAsync_InvalidatesOnlyAfterSuccess()
    {
        var (client, inner, contextProvider) = CreateClient();
        inner
            .RotateCredentialsAsync(Arg.Any<CredentialRotationRequest>(), Arg.Any<CancellationToken>())
            .Returns(TestResult);

        var request = new CredentialRotationRequest("DE:ALL", "current-cpo-token", "TestMSP");

        var result = await client.RotateCredentialsAsync(request);

        result.Should().Be(TestResult);
        // Post-invalidation only — no pre-invalidation that could cause
        // concurrent requests to re-cache stale data.
        contextProvider.Received(1).Invalidate("DE:ALL");
    }

    [Fact]
    public async Task RotateCredentialsAsync_InnerThrows_DoesNotInvalidate()
    {
        var (client, inner, contextProvider) = CreateClient();
        inner
            .RotateCredentialsAsync(Arg.Any<CredentialRotationRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("rotation failed"));

        var request = new CredentialRotationRequest("DE:ALL", "current-cpo-token", "TestMSP");

        var act = async () => await client.RotateCredentialsAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>();
        contextProvider.DidNotReceive().Invalidate(Arg.Any<string>());
    }

    [Fact]
    public async Task UnregisterAsync_InvalidatesAfterSuccess()
    {
        var (client, inner, contextProvider) = CreateClient();

        var request = new UnregisterRequest("DE:ALL", "current-cpo-token");
        await client.UnregisterAsync(request);

        contextProvider.Received(1).Invalidate("DE:ALL");
    }

    [Fact]
    public async Task UnregisterAsync_InnerThrows_DoesNotInvalidate()
    {
        var (client, inner, contextProvider) = CreateClient();
        inner
            .UnregisterAsync(Arg.Any<UnregisterRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("unregister failed"));

        var request = new UnregisterRequest("DE:ALL", "current-cpo-token");

        var act = async () => await client.UnregisterAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>();
        contextProvider.DidNotReceive().Invalidate(Arg.Any<string>());
    }

    [Fact]
    public async Task RegisterAsync_PassesThroughResultUnchanged()
    {
        var (client, inner, _) = CreateClient();
        inner.RegisterAsync(Arg.Any<RegistrationRequest>(), Arg.Any<CancellationToken>()).Returns(TestResult);

        var request = new RegistrationRequest(
            "https://cpo.example.com/ocpi/versions",
            "tokenA",
            "NL",
            "TNM",
            "https://emsp.example.com/ocpi/versions",
            "TestMSP"
        );

        var result = await client.RegisterAsync(request);

        result.Connection.Should().BeSameAs(TestResult.Connection);
        result.CpoToken.Should().Be(TestResult.CpoToken);
    }
}
