using System.Buffers;
using System.Collections.Frozen;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DotOcpi.AspNetCore.Handlers.Versions;

/// <summary>
/// Maps the OCPI versions discovery endpoints required by the registration handshake.
/// These endpoints are public (no token auth) — CPOs call them to discover
/// which OCPI versions the eMSP supports and what module endpoints are available.
/// </summary>
public static class VersionsEndpoints
{
    private static readonly string[] V2_0_Modules =
    [
        "credentials",
        "locations",
        "sessions",
        "cdrs",
        "tariffs",
        "tokens",
    ];

    private static readonly string[] V2_1_1_Modules =
    [
        "credentials",
        "locations",
        "sessions",
        "cdrs",
        "tariffs",
        "tokens",
        "commands",
    ];

    private static readonly string[] V2_2_Modules =
    [
        "credentials",
        "locations",
        "sessions",
        "cdrs",
        "tariffs",
        "tokens",
        "commands",
        "charging_profiles",
    ];

    private static readonly FrozenDictionary<string, string> EmspModuleRoles = new Dictionary<string, string>
    {
        ["credentials"] = "RECEIVER",
        ["locations"] = "RECEIVER",
        ["sessions"] = "RECEIVER",
        ["cdrs"] = "RECEIVER",
        ["tariffs"] = "RECEIVER",
        ["tokens"] = "SENDER",
        ["commands"] = "SENDER",
        ["charging_profiles"] = "SENDER",
    }.ToFrozenDictionary();

    /// <summary>
    /// Maps <c>GET /ocpi/versions</c> and <c>GET /ocpi/versions/{versionId}</c>.
    /// These are outside the auth-filtered pipeline — CPOs call them before registration.
    /// </summary>
    public static void MapVersionsEndpoints(this WebApplication app, string basePath = "/ocpi")
    {
        app.MapGet($"{basePath}/versions", HandleVersions);
        app.MapGet($"{basePath}/versions/{{versionId}}", HandleVersionDetail);
    }

    internal static async Task HandleVersions(HttpContext httpContext)
    {
        var options = httpContext.RequestServices.GetRequiredService<IOptions<DotOcpiOptions>>().Value;

        if (options.BaseUrl is null)
        {
            await OcpiResponseWriter
                .WriteErrorAsync(httpContext, 500, 3000, "eMSP BaseUrl is not configured.", httpContext.RequestAborted)
                .ConfigureAwait(false);
            return;
        }

        var timestamp = GetTimestamp(httpContext);
        var baseUrl = options.BaseUrl.ToString().TrimEnd('/');
        var versions = options.SupportedVersions;

        httpContext.Response.ContentType = "application/json";

        var buffer = new ArrayBufferWriter<byte>(256);
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteNumber("status_code"u8, 1000);
            writer.WriteString("timestamp"u8, timestamp);
            writer.WriteStartArray("data"u8);
            foreach (var v in versions)
            {
                var vs = v.ToVersionString();
                writer.WriteStartObject();
                writer.WriteString("version"u8, vs);
                writer.WriteString("url"u8, $"{baseUrl}/versions/{vs}");
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        await httpContext
            .Response.Body.WriteAsync(buffer.WrittenMemory, httpContext.RequestAborted)
            .ConfigureAwait(false);
    }

    internal static async Task HandleVersionDetail(string versionId, HttpContext httpContext)
    {
        var options = httpContext.RequestServices.GetRequiredService<IOptions<DotOcpiOptions>>().Value;

        if (
            !OcpiVersionExtensions.TryParse(versionId, out var version)
            || !options.SupportedVersions.Contains(version.Value)
        )
        {
            await OcpiResponseWriter
                .WriteErrorAsync(
                    httpContext,
                    404,
                    2001,
                    $"Version '{versionId}' is not supported.",
                    httpContext.RequestAborted
                )
                .ConfigureAwait(false);
            return;
        }

        if (options.BaseUrl is null)
        {
            await OcpiResponseWriter
                .WriteErrorAsync(httpContext, 500, 3000, "eMSP BaseUrl is not configured.", httpContext.RequestAborted)
                .ConfigureAwait(false);
            return;
        }

        var timestamp = GetTimestamp(httpContext);
        var baseUrl = options.BaseUrl.ToString().TrimEnd('/');
        var modules = version.Value switch
        {
            OcpiVersion.V2_0 => V2_0_Modules,
            OcpiVersion.V2_1_1 => V2_1_1_Modules,
            _ => V2_2_Modules,
        };

        httpContext.Response.ContentType = "application/json";

        var buffer = new ArrayBufferWriter<byte>(512);
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteNumber("status_code"u8, 1000);
            writer.WriteString("timestamp"u8, timestamp);
            writer.WriteStartObject("data"u8);
            writer.WriteString("version"u8, versionId);
            writer.WriteStartArray("endpoints"u8);
            foreach (var module in modules)
            {
                writer.WriteStartObject();
                writer.WriteString("identifier"u8, module);
                writer.WriteString("role"u8, EmspModuleRoles.GetValueOrDefault(module, "RECEIVER"));
                writer.WriteString("url"u8, $"{baseUrl}/{versionId}/{module}");
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        await httpContext
            .Response.Body.WriteAsync(buffer.WrittenMemory, httpContext.RequestAborted)
            .ConfigureAwait(false);
    }

    private static DateTimeOffset GetTimestamp(HttpContext httpContext) =>
        httpContext.RequestServices.GetService<TimeProvider>()?.GetUtcNow() ?? DateTimeOffset.UtcNow;
}
