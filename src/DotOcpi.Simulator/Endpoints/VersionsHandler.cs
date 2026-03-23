using System.Text.Json;
using DotOcpi.Simulator.Infrastructure;
using DotOcpi.Simulator.State;
using Microsoft.AspNetCore.Http;

namespace DotOcpi.Simulator.Endpoints;

internal static class VersionsHandler
{
    // Modules available per OCPI version (CPO role)
    private static readonly string[] V2_0_Modules =
    [
        "credentials",
        "locations",
        "tariffs",
        "sessions",
        "cdrs",
        "tokens",
    ];

    private static readonly string[] V2_1_1_Modules =
    [
        "credentials",
        "locations",
        "tariffs",
        "sessions",
        "cdrs",
        "tokens",
        "commands",
    ];

    private static readonly string[] V2_2_Modules =
    [
        "credentials",
        "locations",
        "tariffs",
        "sessions",
        "cdrs",
        "tokens",
        "commands",
        "charging_profiles",
    ];

    // CPO sender/receiver role per module
    private static readonly Dictionary<string, string> CpoModuleRoles = new()
    {
        ["credentials"] = "SENDER",
        ["locations"] = "SENDER",
        ["tariffs"] = "SENDER",
        ["sessions"] = "SENDER",
        ["cdrs"] = "SENDER",
        ["tokens"] = "RECEIVER",
        ["commands"] = "RECEIVER",
        ["charging_profiles"] = "RECEIVER",
    };

    public static async Task HandleVersions(
        HttpContext ctx,
        SimulatorState state,
        CpoSimulatorConfiguration config,
        Uri baseUrl
    )
    {
        await FailureInjectionHelper.ApplyAsync(ctx, config).ConfigureAwait(false);
        var baseUrlStr = baseUrl.ToString().TrimEnd('/');

        var statusCode = FailureInjectionHelper.ResolveOcpiStatusCode(ctx, config);

        await OcpiResponseWriter
            .WriteEnvelopeAsync(
                ctx,
                statusCode,
                static (writer, s) =>
                {
                    writer.WriteStartArray("data"u8);
                    foreach (var v in s.config.SupportedVersions)
                    {
                        var vs = v.ToVersionString();
                        writer.WriteStartObject();
                        writer.WriteString("version"u8, vs);
                        writer.WriteString("url"u8, $"{s.baseUrl}/ocpi/versions/{vs}");
                        writer.WriteEndObject();
                    }
                    writer.WriteEndArray();
                },
                (config, baseUrl: baseUrlStr)
            )
            .ConfigureAwait(false);
    }

    public static async Task HandleVersionDetail(
        HttpContext ctx,
        string versionId,
        SimulatorState state,
        CpoSimulatorConfiguration config,
        Uri baseUrl
    )
    {
        if (
            !OcpiVersionExtensions.TryParse(versionId, out var version)
            || !config.SupportedVersions.Contains(version.Value)
        )
        {
            await OcpiResponseWriter
                .WriteErrorAsync(ctx, 404, 2001, $"Version {versionId} not supported.")
                .ConfigureAwait(false);
            return;
        }

        // Track negotiated version on default connection
        var conn = state.Connections.GetOrAdd("default", _ => new ConnectionState { ConnectionId = "default" });
        conn.SetNegotiatedVersion(version.Value);

        var baseUrlStr = baseUrl.ToString().TrimEnd('/');
        var usesPartyId = version.Value.UsesPartyIdInUrls();
        var prefix = usesPartyId ? $"{config.CpoIdentity.CountryCode}/{config.CpoIdentity.PartyId}/" : "";
        var includeRole = usesPartyId; // role field required in 2.2+

        var allModules = version.Value switch
        {
            OcpiVersion.V2_0 => V2_0_Modules,
            OcpiVersion.V2_1_1 => V2_1_1_Modules,
            _ => V2_2_Modules,
        };

        var modules = config.AdvertisedModules is not null
            ? config.AdvertisedModules.Prepend("credentials").Distinct().Where(m => allModules.Contains(m)).ToArray()
            : allModules;

        await OcpiResponseWriter
            .WriteSuccessAsync(
                ctx,
                static (writer, s) =>
                {
                    writer.WriteStartObject("data"u8);
                    writer.WriteString("version"u8, s.versionId);
                    writer.WriteStartArray("endpoints"u8);
                    foreach (var m in s.modules)
                    {
                        writer.WriteStartObject();
                        writer.WriteString("identifier"u8, m);

                        if (s.includeRole && CpoModuleRoles.TryGetValue(m, out var role))
                            writer.WriteString("role"u8, role);

                        var url = m == "credentials" ? $"{s.baseUrl}/ocpi/{m}" : $"{s.baseUrl}/ocpi/{s.prefix}{m}";
                        writer.WriteString("url"u8, url);
                        writer.WriteEndObject();
                    }
                    writer.WriteEndArray();
                    writer.WriteEndObject();
                },
                (versionId, baseUrl: baseUrlStr, modules, prefix, includeRole)
            )
            .ConfigureAwait(false);
    }
}
