using DotOcpi.Client.Internal;
using DotOcpi.Security;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace DotOcpi.Client.Tests.Internal;

[Trait("Category", "Security")]
public class TokenStoreOutboundTokenProviderTests
{
    private readonly ITokenStore _tokenStore = Substitute.For<ITokenStore>();
    private readonly ITokenProtector _protector = Substitute.For<ITokenProtector>();
    private readonly TokenStoreOutboundTokenProvider _provider;

    public TokenStoreOutboundTokenProviderTests()
    {
        _provider = new TokenStoreOutboundTokenProvider(_tokenStore, _protector);
    }

    [Fact]
    public async Task GetToken_ReturnsUnprotectedToken()
    {
        _tokenStore.GetCpoTokenAsync("DE:ALL", Arg.Any<CancellationToken>()).Returns("protected-value");
        _protector.Unprotect("protected-value").Returns("raw-cpo-token");

        var token = await _provider.GetTokenAsync("DE:ALL");

        token.Should().Be("raw-cpo-token");
    }

    [Fact]
    public async Task GetToken_UnknownCpo_ThrowsWithGuidance()
    {
        _tokenStore.GetCpoTokenAsync("UNKNOWN", Arg.Any<CancellationToken>()).Returns(default(string?));

        var act = () => _provider.GetTokenAsync("UNKNOWN").AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*No CPO token found*UNKNOWN*");
    }

    [Fact]
    public async Task GetToken_PassesCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        _tokenStore.GetCpoTokenAsync("DE:ALL", cts.Token).Returns("protected");
        _protector.Unprotect("protected").Returns("raw");

        await _provider.GetTokenAsync("DE:ALL", cts.Token);

        await _tokenStore.Received(1).GetCpoTokenAsync("DE:ALL", cts.Token);
    }
}
