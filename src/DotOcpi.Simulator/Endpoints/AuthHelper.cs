using System.Security.Cryptography;
using System.Text;
using DotOcpi.Simulator.State;
using Microsoft.AspNetCore.Http;

namespace DotOcpi.Simulator.Endpoints;

/// <summary>
/// Shared auth validation for endpoint handlers.
/// </summary>
internal static class AuthHelper
{
    /// <summary>
    /// Validates the Authorization header against the expected token using constant-time comparison.
    /// </summary>
    public static bool ValidateToken(HttpContext ctx, string expectedToken)
    {
        var authHeader = ctx.Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Token ", StringComparison.OrdinalIgnoreCase))
            return false;

        var token = authHeader["Token ".Length..];
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedToken);
        return CryptographicOperations.FixedTimeEquals(tokenBytes, expectedBytes);
    }

    /// <summary>
    /// Validates module auth (Token B). Returns true if auth passes or is not required.
    /// Writes 401 error and returns false if auth fails.
    /// </summary>
    public static async Task<bool> ValidateModuleAuthAsync(
        HttpContext ctx,
        SimulatorState state,
        CpoSimulatorConfiguration config
    )
    {
        if (!config.RequireAuth)
            return true;

        var conn = state.DefaultConnection;
        if (conn?.IssuedTokenB is null)
            return true; // No registration yet — open access

        if (!ValidateToken(ctx, conn.IssuedTokenB))
        {
            await Infrastructure
                .OcpiResponseWriter.WriteErrorAsync(ctx, 401, 2002, "Missing or invalid Authorization header.")
                .ConfigureAwait(false);
            return false;
        }
        return true;
    }
}
