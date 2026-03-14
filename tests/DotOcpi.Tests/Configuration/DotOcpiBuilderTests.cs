using DotOcpi.Observability;
using DotOcpi.Registry;
using DotOcpi.Security;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DotOcpi.Tests.Configuration;

public class DotOcpiBuilderTests
{
    [Fact]
    public void AddDotOcpi_RegistersOcpiMetrics()
    {
        var services = new ServiceCollection();

        services.AddDotOcpi(options =>
        {
            options.SupportedVersions = [OcpiVersion.V2_2_1];
        });

        var provider = services.BuildServiceProvider();
        var metrics = provider.GetService<OcpiMetrics>();

        metrics.Should().NotBeNull();
    }

    [Fact]
    public void AddDotOcpi_RegistersOptionsValidation()
    {
        var services = new ServiceCollection();

        services.AddDotOcpi(options =>
        {
            options.SupportedVersions = [];
        });

        var provider = services.BuildServiceProvider();
        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<DotOcpiOptions>>();

        var act = () => optionsMonitor.CurrentValue;
        act.Should().Throw<OptionsValidationException>().Which.Message.Should().Contain("version");
    }

    [Fact]
    public void AddDotOcpi_ConfiguresOptions()
    {
        var services = new ServiceCollection();

        services.AddDotOcpi(options =>
        {
            options.SupportedVersions = [OcpiVersion.V2_1_1, OcpiVersion.V2_2_1];
            options.DefaultEmspIdentity = new PartyIdentity("DE", "MSP");
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<DotOcpiOptions>>().Value;

        options.SupportedVersions.Should().HaveCount(2);
        options.DefaultEmspIdentity.Should().Be(new PartyIdentity("DE", "MSP"));
    }

    [Fact]
    public void AddInMemoryTokenStore_RegistersTokenStore()
    {
        var services = new ServiceCollection();

        services
            .AddDotOcpi(options =>
            {
                options.SupportedVersions = [OcpiVersion.V2_2_1];
            })
            .AddInMemoryTokenStore();

        var provider = services.BuildServiceProvider();
        var tokenStore = provider.GetService<ITokenStore>();

        tokenStore.Should().NotBeNull();
        tokenStore.Should().BeOfType<InMemoryTokenStore>();
    }

    [Fact]
    public void AddInMemoryCpoRegistry_RegistersRegistry()
    {
        var services = new ServiceCollection();

        services
            .AddDotOcpi(options =>
            {
                options.SupportedVersions = [OcpiVersion.V2_2_1];
            })
            .AddInMemoryCpoRegistry();

        var provider = services.BuildServiceProvider();
        var registry = provider.GetService<ICpoRegistry>();

        registry.Should().NotBeNull();
        registry.Should().BeOfType<InMemoryCpoRegistry>();
    }

    [Fact]
    public void Builder_ChainingWorks()
    {
        var services = new ServiceCollection();

        services
            .AddDotOcpi(options =>
            {
                options.SupportedVersions = [OcpiVersion.V2_2_1];
            })
            .AddInMemoryTokenStore()
            .AddInMemoryCpoRegistry();

        var provider = services.BuildServiceProvider();

        provider.GetService<ITokenStore>().Should().NotBeNull();
        provider.GetService<ICpoRegistry>().Should().NotBeNull();
        provider.GetService<OcpiMetrics>().Should().NotBeNull();
    }

    [Fact]
    public void AddDotOcpi_InvalidHttpsUrl_ThrowsOnResolve()
    {
        var services = new ServiceCollection();

        services.AddDotOcpi(options =>
        {
            options.BaseUrl = new Uri("http://insecure.example.com");
        });

        var provider = services.BuildServiceProvider();
        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<DotOcpiOptions>>();

        var act = () => optionsMonitor.CurrentValue;
        act.Should().Throw<OptionsValidationException>().Which.Message.Should().Contain("HTTPS");
    }
}
