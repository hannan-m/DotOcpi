using DotOcpi.Observability;
using DotOcpi.Registry;
using DotOcpi.Security;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace DotOcpi.AspNetCore.Tests;

public class DotOcpiStartupValidatorTests
{
    private static ServiceProvider BuildServices(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(NullLogger<DotOcpiStartupValidator>.Instance);
        configure?.Invoke(services);
        return services.BuildServiceProvider();
    }

    private static ServiceProvider BuildFullServices()
    {
        return BuildServices(services =>
        {
            services.AddSingleton<ICpoRegistry>(Substitute.For<ICpoRegistry>());
            services.AddSingleton<ITokenStore>(Substitute.For<ITokenStore>());
            services.AddSingleton(sp => new OcpiTokenValidator(sp.GetRequiredService<ITokenStore>()));
            services.AddMetrics();
            services.AddSingleton<OcpiMetrics>();
        });
    }

    [Fact]
    public async Task StartingAsync_AllServicesRegistered_DoesNotThrow()
    {
        var sp = BuildFullServices();
        var validator = new DotOcpiStartupValidator(sp, NullLogger<DotOcpiStartupValidator>.Instance);

        var act = () => validator.StartingAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StartingAsync_MissingTokenStore_Throws()
    {
        var sp = BuildServices(services =>
        {
            services.AddSingleton<ICpoRegistry>(Substitute.For<ICpoRegistry>());
            services.AddSingleton(new OcpiTokenValidator(Substitute.For<ITokenStore>()));
            services.AddMetrics();
            services.AddSingleton<OcpiMetrics>();
        });
        var validator = new DotOcpiStartupValidator(sp, NullLogger<DotOcpiStartupValidator>.Instance);

        var act = () => validator.StartingAsync(CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Message.Should().Contain("ITokenStore");
    }

    [Fact]
    public async Task StartingAsync_MissingCpoRegistry_Throws()
    {
        var sp = BuildServices(services =>
        {
            services.AddSingleton<ITokenStore>(Substitute.For<ITokenStore>());
            services.AddSingleton(new OcpiTokenValidator(Substitute.For<ITokenStore>()));
            services.AddMetrics();
            services.AddSingleton<OcpiMetrics>();
        });
        var validator = new DotOcpiStartupValidator(sp, NullLogger<DotOcpiStartupValidator>.Instance);

        var act = () => validator.StartingAsync(CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Message.Should().Contain("ICpoRegistry");
    }

    [Fact]
    [Trait("Category", "Security")]
    public async Task StartingAsync_TokenAInConfig_ThrowsSecurityViolation()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["DotOcpi:TokenA"] = "secret-value" })
            .Build();

        var sp = BuildServices(services =>
        {
            services.AddSingleton<ICpoRegistry>(Substitute.For<ICpoRegistry>());
            services.AddSingleton<ITokenStore>(Substitute.For<ITokenStore>());
            services.AddSingleton(new OcpiTokenValidator(Substitute.For<ITokenStore>()));
            services.AddMetrics();
            services.AddSingleton<OcpiMetrics>();
            services.AddSingleton<IConfiguration>(config);
        });
        var validator = new DotOcpiStartupValidator(sp, NullLogger<DotOcpiStartupValidator>.Instance);

        var act = () => validator.StartingAsync(CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should()
            .Contain("security violation");
    }

    [Fact]
    [Trait("Category", "Security")]
    public async Task StartingAsync_TokenAInAlternateKey_ThrowsSecurityViolation()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["OcpiTokenA"] = "leaked" })
            .Build();

        var sp = BuildServices(services =>
        {
            services.AddSingleton<ICpoRegistry>(Substitute.For<ICpoRegistry>());
            services.AddSingleton<ITokenStore>(Substitute.For<ITokenStore>());
            services.AddSingleton(new OcpiTokenValidator(Substitute.For<ITokenStore>()));
            services.AddMetrics();
            services.AddSingleton<OcpiMetrics>();
            services.AddSingleton<IConfiguration>(config);
        });
        var validator = new DotOcpiStartupValidator(sp, NullLogger<DotOcpiStartupValidator>.Instance);

        var act = () => validator.StartingAsync(CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should()
            .Contain("security violation");
    }
}
