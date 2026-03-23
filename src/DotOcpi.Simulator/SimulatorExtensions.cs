using Microsoft.Extensions.DependencyInjection;

namespace DotOcpi.Simulator;

/// <summary>
/// Extension methods for registering <see cref="OcpiCpoSimulator"/> with DI.
/// </summary>
public static class SimulatorExtensions
{
    /// <summary>
    /// Creates and registers a singleton <see cref="OcpiCpoSimulator"/>.
    /// Intended for integration test fixtures.
    /// </summary>
    public static async Task<IServiceCollection> AddTestCpoServerAsync(
        this IServiceCollection services,
        Action<CpoSimulatorConfiguration>? configure = null
    )
    {
        var server = await OcpiCpoSimulator.CreateAsync(configure).ConfigureAwait(false);
        services.AddSingleton(server);
        return services;
    }

    /// <summary>
    /// Registers a singleton <see cref="OcpiCpoSimulator"/> that is started and stopped
    /// with the host lifetime. Intended for integration test fixtures.
    /// Prefer <see cref="AddTestCpoServerAsync"/> when called from an async context.
    /// </summary>
    public static IServiceCollection AddTestCpoServer(
        this IServiceCollection services,
        Action<CpoSimulatorConfiguration>? configure = null
    )
    {
        // Synchronous wrapper — safe in test hosts that have no SynchronizationContext
        var server = OcpiCpoSimulator.CreateAsync(configure).GetAwaiter().GetResult();
        services.AddSingleton(server);
        return services;
    }
}
