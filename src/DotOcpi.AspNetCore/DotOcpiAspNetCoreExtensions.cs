using DotOcpi.AspNetCore.Middleware;
using DotOcpi.AspNetCore.Security;
using DotOcpi.Security;
using DotOcpi.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotOcpi.AspNetCore;

/// <summary>
/// Extension methods for adding DotOcpi ASP.NET Core server integration.
/// </summary>
public static class DotOcpiAspNetCoreExtensions
{
    /// <summary>
    /// Adds the ASP.NET Core server pipeline: OCPI auth, token validation,
    /// protocol-level validators, exception middleware, and startup validation.
    /// </summary>
    public static DotOcpiBuilder AddAspNetCoreServer(this DotOcpiBuilder builder)
    {
        builder.Services.AddSingleton<OcpiExceptionMiddleware>();
        builder.Services.AddSingleton<OcpiTokenValidator>();

        // Replace the default PlaintextTokenProtector with Data Protection-backed
        // encryption for outbound CPO token storage. Consumers can override with
        // AddTokenProtector<T>() if they need a different implementation.
        builder.Services.RemoveAll<ITokenProtector>();
        builder.Services.AddSingleton<ITokenProtector, DataProtectionTokenProtector>();

        // Protocol-level model validators — resolved by EndpointHelper at runtime.
        // Each validator implements IOcpiValidator<T> for all four OCPI version model types,
        // so protocol rules (coordinate ranges, EVSE uniqueness, currency codes, etc.)
        // are enforced regardless of the negotiated version.
        builder.Services.AddSingleton<LocationValidator>();
        builder.Services.AddSingleton<IOcpiValidator<Models.V2_0.Location>>(sp =>
            sp.GetRequiredService<LocationValidator>()
        );
        builder.Services.AddSingleton<IOcpiValidator<Models.V2_1_1.Location>>(sp =>
            sp.GetRequiredService<LocationValidator>()
        );
        builder.Services.AddSingleton<IOcpiValidator<Models.V2_2.Location>>(sp =>
            sp.GetRequiredService<LocationValidator>()
        );
        builder.Services.AddSingleton<IOcpiValidator<Models.V2_2_1.Location>>(sp =>
            sp.GetRequiredService<LocationValidator>()
        );

        builder.Services.AddSingleton<SessionValidator>();
        builder.Services.AddSingleton<IOcpiValidator<Models.V2_0.Session>>(sp =>
            sp.GetRequiredService<SessionValidator>()
        );
        builder.Services.AddSingleton<IOcpiValidator<Models.V2_1_1.Session>>(sp =>
            sp.GetRequiredService<SessionValidator>()
        );
        builder.Services.AddSingleton<IOcpiValidator<Models.V2_2.Session>>(sp =>
            sp.GetRequiredService<SessionValidator>()
        );
        builder.Services.AddSingleton<IOcpiValidator<Models.V2_2_1.Session>>(sp =>
            sp.GetRequiredService<SessionValidator>()
        );

        builder.Services.AddSingleton<CdrValidator>();
        builder.Services.AddSingleton<IOcpiValidator<Models.V2_0.Cdr>>(sp => sp.GetRequiredService<CdrValidator>());
        builder.Services.AddSingleton<IOcpiValidator<Models.V2_1_1.Cdr>>(sp => sp.GetRequiredService<CdrValidator>());
        builder.Services.AddSingleton<IOcpiValidator<Models.V2_2.Cdr>>(sp => sp.GetRequiredService<CdrValidator>());
        builder.Services.AddSingleton<IOcpiValidator<Models.V2_2_1.Cdr>>(sp => sp.GetRequiredService<CdrValidator>());

        builder.Services.AddSingleton<TariffValidator>();
        builder.Services.AddSingleton<IOcpiValidator<Models.V2_0.Tariff>>(sp =>
            sp.GetRequiredService<TariffValidator>()
        );
        builder.Services.AddSingleton<IOcpiValidator<Models.V2_1_1.Tariff>>(sp =>
            sp.GetRequiredService<TariffValidator>()
        );
        builder.Services.AddSingleton<IOcpiValidator<Models.V2_2.Tariff>>(sp =>
            sp.GetRequiredService<TariffValidator>()
        );
        builder.Services.AddSingleton<IOcpiValidator<Models.V2_2_1.Tariff>>(sp =>
            sp.GetRequiredService<TariffValidator>()
        );

        builder.Services.AddSingleton<TokenValidator>();
        builder.Services.AddSingleton<IOcpiValidator<Models.V2_0.Token>>(sp => sp.GetRequiredService<TokenValidator>());
        builder.Services.AddSingleton<IOcpiValidator<Models.V2_1_1.Token>>(sp =>
            sp.GetRequiredService<TokenValidator>()
        );
        builder.Services.AddSingleton<IOcpiValidator<Models.V2_2.Token>>(sp => sp.GetRequiredService<TokenValidator>());
        builder.Services.AddSingleton<IOcpiValidator<Models.V2_2_1.Token>>(sp =>
            sp.GetRequiredService<TokenValidator>()
        );

        builder.Services.AddSingleton<CredentialsValidator>();
        builder.Services.AddSingleton<IOcpiValidator<Models.V2_0.Credentials>>(sp =>
            sp.GetRequiredService<CredentialsValidator>()
        );
        builder.Services.AddSingleton<IOcpiValidator<Models.V2_1_1.Credentials>>(sp =>
            sp.GetRequiredService<CredentialsValidator>()
        );
        builder.Services.AddSingleton<IOcpiValidator<Models.V2_2.Credentials>>(sp =>
            sp.GetRequiredService<CredentialsValidator>()
        );
        builder.Services.AddSingleton<IOcpiValidator<Models.V2_2_1.Credentials>>(sp =>
            sp.GetRequiredService<CredentialsValidator>()
        );

        builder.Services.AddHostedService<DotOcpiStartupValidator>();

        // CpoHealthMonitor probes CPO connections in the background.
        // It respects EnableHealthMonitoring from DotOcpiOptions — when disabled,
        // ExecuteAsync returns immediately without starting the timer loop.
        builder.Services.AddHostedService(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<DotOcpiOptions>>().Value;
            return new Registry.CpoHealthMonitor(
                sp.GetRequiredService<Registry.ICpoRegistry>(),
                sp.GetRequiredService<IHttpClientFactory>().CreateClient("OcpiHealthCheck"),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Registry.CpoHealthMonitor>>(),
                sp.GetService<TimeProvider>(),
                options.HealthMonitoringInterval
            )
            {
                Enabled = options.EnableHealthMonitoring,
            };
        });

        return builder;
    }
}
