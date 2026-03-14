using DotOcpi.AspNetCore.HealthChecks;
using DotOcpi.Registry;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using Xunit;

namespace DotOcpi.AspNetCore.Tests.HealthChecks;

public class OcpiRegistryHealthCheckTests
{
    private readonly ICpoRegistry _registry = Substitute.For<ICpoRegistry>();

    private readonly HealthCheckContext _context = new()
    {
        Registration = new HealthCheckRegistration("test", _ => null!, null, null),
    };

    [Fact]
    public async Task NoConnections_ReturnsUnhealthy()
    {
        _registry.GetAll().Returns([]);

        var check = new OcpiRegistryHealthCheck(_registry);
        var result = await check.CheckHealthAsync(_context);

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task AllConnected_ReturnsHealthy()
    {
        _registry
            .GetAll()
            .Returns([
                CreateConnection("DE:ALL", ConnectionStatus.Connected),
                CreateConnection("NL:TNM", ConnectionStatus.Connected),
            ]);

        var check = new OcpiRegistryHealthCheck(_registry);
        var result = await check.CheckHealthAsync(_context);

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task SomeOffline_ReturnsDegraded()
    {
        _registry
            .GetAll()
            .Returns([
                CreateConnection("DE:ALL", ConnectionStatus.Connected),
                CreateConnection("NL:TNM", ConnectionStatus.Offline),
            ]);

        var check = new OcpiRegistryHealthCheck(_registry);
        var result = await check.CheckHealthAsync(_context);

        result.Status.Should().Be(HealthStatus.Degraded);
    }

    [Fact]
    public async Task AllOffline_ReturnsUnhealthy()
    {
        _registry
            .GetAll()
            .Returns([
                CreateConnection("DE:ALL", ConnectionStatus.Offline),
                CreateConnection("NL:TNM", ConnectionStatus.Suspended),
            ]);

        var check = new OcpiRegistryHealthCheck(_registry);
        var result = await check.CheckHealthAsync(_context);

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task UnregisteredOnly_ReturnsUnhealthy()
    {
        _registry.GetAll().Returns([CreateConnection("DE:ALL", ConnectionStatus.Unregistered)]);

        var check = new OcpiRegistryHealthCheck(_registry);
        var result = await check.CheckHealthAsync(_context);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("No active");
    }

    private static CpoConnection CreateConnection(string key, ConnectionStatus status)
    {
        var parts = key.Split(':');
        return new CpoConnection
        {
            CpoCountryCode = parts[0],
            CpoPartyId = parts[1],
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
