using DotOcpi.AspNetCore.HealthChecks;
using DotOcpi.Registry;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using Xunit;

namespace DotOcpi.AspNetCore.Tests.HealthChecks;

public class OcpiCpoHealthCheckTests
{
    private readonly ICpoRegistry _registry = Substitute.For<ICpoRegistry>();

    private readonly HealthCheckContext _context = new()
    {
        Registration = new HealthCheckRegistration("test", _ => null!, null, null),
    };

    [Fact]
    public async Task CpoConnected_ReturnsHealthy()
    {
        _registry.FindByConnectionKey("DE:ALL").Returns(CreateConnection("DE", "ALL", ConnectionStatus.Connected));

        var check = new OcpiCpoHealthCheck(_registry, "DE:ALL");
        var result = await check.CheckHealthAsync(_context);

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CpoOffline_ReturnsDegraded()
    {
        _registry.FindByConnectionKey("DE:ALL").Returns(CreateConnection("DE", "ALL", ConnectionStatus.Offline));

        var check = new OcpiCpoHealthCheck(_registry, "DE:ALL");
        var result = await check.CheckHealthAsync(_context);

        result.Status.Should().Be(HealthStatus.Degraded);
    }

    [Fact]
    public async Task CpoPending_ReturnsDegraded()
    {
        _registry.FindByConnectionKey("DE:ALL").Returns(CreateConnection("DE", "ALL", ConnectionStatus.Pending));

        var check = new OcpiCpoHealthCheck(_registry, "DE:ALL");
        var result = await check.CheckHealthAsync(_context);

        result.Status.Should().Be(HealthStatus.Degraded);
    }

    [Fact]
    public async Task CpoSuspended_ReturnsUnhealthy()
    {
        _registry.FindByConnectionKey("DE:ALL").Returns(CreateConnection("DE", "ALL", ConnectionStatus.Suspended));

        var check = new OcpiCpoHealthCheck(_registry, "DE:ALL");
        var result = await check.CheckHealthAsync(_context);

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CpoNotFound_ReturnsUnhealthy()
    {
        _registry.FindByConnectionKey("XX:YYY").Returns((CpoConnection?)null);

        var check = new OcpiCpoHealthCheck(_registry, "XX:YYY");
        var result = await check.CheckHealthAsync(_context);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("not found");
    }

    [Fact]
    public async Task CpoUnregistered_ReturnsUnhealthy()
    {
        _registry.FindByConnectionKey("DE:ALL").Returns(CreateConnection("DE", "ALL", ConnectionStatus.Unregistered));

        var check = new OcpiCpoHealthCheck(_registry, "DE:ALL");
        var result = await check.CheckHealthAsync(_context);

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    private static CpoConnection CreateConnection(string countryCode, string partyId, ConnectionStatus status)
    {
        return new CpoConnection
        {
            CpoCountryCode = countryCode,
            CpoPartyId = partyId,
            EmspCountryCode = "DE",
            EmspPartyId = "MSP",
            Version = OcpiVersion.V2_2_1,
            Status = status,
            ModuleEndpoints = new Dictionary<string, string>(),
            TokenBHash = "testhash",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
    }
}
