using DotOcpi.Security;
using DotOcpi.Simulator.Infrastructure;
using DotOcpi.Simulator.State;
using Microsoft.AspNetCore.Http;

namespace DotOcpi.Simulator.Endpoints;

internal static class CredentialsHandler
{
    public static async Task HandlePost(
        HttpContext ctx,
        SimulatorState state,
        CpoSimulatorConfiguration config,
        Uri baseUrl,
        string tokenA
    )
    {
        if (!AuthHelper.ValidateToken(ctx, tokenA))
        {
            await OcpiResponseWriter
                .WriteErrorAsync(ctx, 401, 2002, "Missing or invalid Authorization header.")
                .ConfigureAwait(false);
            return;
        }

        if (config.RejectRegistration)
        {
            await OcpiResponseWriter.WriteErrorAsync(ctx, 400, 3001, "Registration rejected.").ConfigureAwait(false);
            return;
        }

        var body = RequestRecordingMiddleware.GetParsedObjectBody(ctx);
        string? receivedTokenC = null;
        string? emspVersionsUrl = null;
        if (body?.TryGetProperty("token", out var tokenProp) == true)
            receivedTokenC = tokenProp.GetString();
        if (body?.TryGetProperty("url", out var urlProp) == true)
            emspVersionsUrl = urlProp.GetString();

        var tokenB = TokenGenerator.Generate();
        var conn = state.Connections.GetOrAdd("default", _ => new ConnectionState { ConnectionId = "default" });
        conn.SetRegistration(tokenB, receivedTokenC, emspVersionsUrl);

        var version = conn.NegotiatedVersion != default ? conn.NegotiatedVersion : config.SupportedVersions[0];
        await WriteCredentialsResponse(ctx, version, tokenB, config.CpoIdentity, baseUrl).ConfigureAwait(false);
    }

    public static async Task HandlePut(
        HttpContext ctx,
        SimulatorState state,
        CpoSimulatorConfiguration config,
        Uri baseUrl
    )
    {
        var conn = state.DefaultConnection;
        if (conn?.IssuedTokenB is null || !AuthHelper.ValidateToken(ctx, conn.IssuedTokenB))
        {
            await OcpiResponseWriter
                .WriteErrorAsync(ctx, 401, 2002, "Missing or invalid Authorization header.")
                .ConfigureAwait(false);
            return;
        }

        var body = RequestRecordingMiddleware.GetParsedObjectBody(ctx);
        string? newTokenC = null;
        if (body?.TryGetProperty("token", out var tokenProp) == true)
            newTokenC = tokenProp.GetString();

        var newTokenB = TokenGenerator.Generate();
        conn.SetRegistration(newTokenB, newTokenC, conn.EmspVersionsUrl);

        var version = conn.NegotiatedVersion != default ? conn.NegotiatedVersion : config.SupportedVersions[0];
        await WriteCredentialsResponse(ctx, version, newTokenB, config.CpoIdentity, baseUrl).ConfigureAwait(false);
    }

    public static async Task HandleDelete(HttpContext ctx, SimulatorState state)
    {
        var conn = state.DefaultConnection;
        if (conn?.IssuedTokenB is null || !AuthHelper.ValidateToken(ctx, conn.IssuedTokenB))
        {
            await OcpiResponseWriter
                .WriteErrorAsync(ctx, 401, 2002, "Missing or invalid Authorization header.")
                .ConfigureAwait(false);
            return;
        }

        conn.ClearRegistration();

        await OcpiResponseWriter.WriteSuccessAsync(ctx).ConfigureAwait(false);
    }

    private static async Task WriteCredentialsResponse(
        HttpContext ctx,
        OcpiVersion version,
        string tokenB,
        PartyIdentity cpoIdentity,
        Uri baseUrl
    )
    {
        var baseUrlStr = baseUrl.ToString().TrimEnd('/');

        // Write both flat (2.0/2.1.1) and roles-based (2.2+) fields for broad compatibility
        await OcpiResponseWriter
            .WriteSuccessAsync(
                ctx,
                static (writer, s) =>
                {
                    writer.WriteStartObject("data"u8);
                    writer.WriteString("token"u8, s.tokenB);
                    writer.WriteString("url"u8, $"{s.baseUrl}/ocpi/versions");

                    // Flat fields for 2.0/2.1.1
                    writer.WriteString("country_code"u8, s.identity.CountryCode);
                    writer.WriteString("party_id"u8, s.identity.PartyId);
                    writer.WriteString("business_name"u8, "Test CPO");

                    // Roles array for 2.2+
                    writer.WriteStartArray("roles"u8);
                    writer.WriteStartObject();
                    writer.WriteString("role"u8, "CPO");
                    writer.WriteString("country_code"u8, s.identity.CountryCode);
                    writer.WriteString("party_id"u8, s.identity.PartyId);
                    writer.WriteStartObject("business_details"u8);
                    writer.WriteString("name"u8, "Test CPO");
                    writer.WriteEndObject();
                    writer.WriteEndObject();
                    writer.WriteEndArray();

                    writer.WriteEndObject();
                },
                (tokenB, baseUrl: baseUrlStr, identity: cpoIdentity)
            )
            .ConfigureAwait(false);
    }
}
