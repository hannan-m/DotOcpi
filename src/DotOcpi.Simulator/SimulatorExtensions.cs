using Microsoft.Extensions.DependencyInjection;

namespace DotOcpi.Simulator;

/// <summary>
/// Extension methods for registering <see cref="OcpiCpoSimulator"/> with DI.
/// </summary>
public static class SimulatorExtensions
{
    /// <summary>
    /// Registers a singleton <see cref="OcpiCpoSimulator"/> that is started and stopped
    /// with the host lifetime. Intended for integration test fixtures.
    /// </summary>
    public static IServiceCollection AddTestCpoServer(
        this IServiceCollection services,
        Action<CpoSimulatorConfiguration>? configure = null
    )
    {
        services.AddSingleton(sp =>
        {
            var server = OcpiCpoSimulator.CreateAsync(configure).GetAwaiter().GetResult();
            return server;
        });

        return services;
    }
}
