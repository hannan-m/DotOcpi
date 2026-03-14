using DotOcpi.Observability;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DotOcpi;

/// <summary>
/// Extension methods for registering DotOcpi services.
/// </summary>
public static class DotOcpiServiceCollectionExtensions
{
    /// <summary>
    /// Adds core DotOcpi services with programmatic configuration.
    /// </summary>
    public static DotOcpiBuilder AddDotOcpi(this IServiceCollection services, Action<DotOcpiOptions> configure)
    {
        services.Configure(configure);
        return RegisterCoreServices(services);
    }

    /// <summary>
    /// Adds core DotOcpi services with configuration section binding.
    /// </summary>
    public static DotOcpiBuilder AddDotOcpi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DotOcpiOptions>(configuration);
        return RegisterCoreServices(services);
    }

    private static DotOcpiBuilder RegisterCoreServices(IServiceCollection services)
    {
        // Validate options on first resolve
        services.AddSingleton<IValidateOptions<DotOcpiOptions>, DotOcpiOptionsValidator>();

        // Core infrastructure — AddMetrics registers IMeterFactory
        services.AddMetrics();
        services.AddSingleton<OcpiMetrics>();

        return new DotOcpiBuilder(services);
    }

    private sealed class DotOcpiOptionsValidator : IValidateOptions<DotOcpiOptions>
    {
        public ValidateOptionsResult Validate(string? name, DotOcpiOptions options)
        {
            var errors = options.Validate();
            if (errors.Count > 0)
            {
                return ValidateOptionsResult.Fail(errors);
            }

            return ValidateOptionsResult.Success;
        }
    }
}
