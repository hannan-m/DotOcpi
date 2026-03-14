using Microsoft.Extensions.DependencyInjection;

namespace DotOcpi.Testing;

/// <summary>
/// Extension methods for registering <see cref="OcpiTestCpoServer"/> with DI.
/// </summary>
public static class TestingExtensions
{
    /// <summary>
    /// Registers a singleton <see cref="OcpiTestCpoServer"/> that is started and stopped
    /// with the host lifetime. Intended for integration test fixtures.
    /// </summary>
    public static IServiceCollection AddTestCpoServer(
        this IServiceCollection services,
        Action<TestCpoConfiguration>? configure = null
    )
    {
        services.AddSingleton(sp =>
        {
            var server = OcpiTestCpoServer.CreateAsync(configure).GetAwaiter().GetResult();
            return server;
        });

        return services;
    }
}
