using DotOcpi.AspNetCore.HealthChecks;
using DotOcpi.Security;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace DotOcpi.AspNetCore.Tests.HealthChecks;

public class OcpiTokenStoreHealthCheckTests
{
    private readonly ITokenStore _tokenStore = Substitute.For<ITokenStore>();

    private readonly HealthCheckContext _context = new()
    {
        Registration = new HealthCheckRegistration("test", _ => null!, null, null),
    };

    [Fact]
    public async Task StoreAccessible_ReturnsHealthy()
    {
        _tokenStore.FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((TokenEntry?)null);

        var check = new OcpiTokenStoreHealthCheck(_tokenStore);
        var result = await check.CheckHealthAsync(_context);

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task StoreThrows_ReturnsUnhealthy()
    {
        _tokenStore
            .FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Store unavailable"));

        var check = new OcpiTokenStoreHealthCheck(_tokenStore);
        var result = await check.CheckHealthAsync(_context);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Exception.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task StoreReturnsEntry_StillHealthy()
    {
        // Even if the probe hash somehow matches, the store responded successfully
        _tokenStore
            .FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TokenEntry("hash", TokenPurpose.TokenB, "DE:MSP"));

        var check = new OcpiTokenStoreHealthCheck(_tokenStore);
        var result = await check.CheckHealthAsync(_context);

        result.Status.Should().Be(HealthStatus.Healthy);
    }
}
