using DotOcpi.Simulator.State;
using Microsoft.AspNetCore.Http;

namespace DotOcpi.Simulator.Endpoints;

/// <summary>
/// Applies failure injection (timeout, delay, forced status codes) before endpoint handlers.
/// </summary>
internal static class FailureInjectionHelper
{
    public static async Task ApplyAsync(HttpContext ctx, CpoSimulatorConfiguration config)
    {
        var endpointConfig = ResolveEndpointOverride(ctx.Request.Path.Value ?? "", config);
        if (endpointConfig is not null)
        {
            if (endpointConfig.SimulateTimeout)
                throw new TaskCanceledException("Simulated timeout (per-endpoint).");
            if (endpointConfig.ResponseDelay is { } epDelay)
                await Task.Delay(epDelay, ctx.RequestAborted).ConfigureAwait(false);
            return;
        }

        if (config.SimulateTimeout)
            throw new TaskCanceledException("Simulated timeout.");

        if (config.ResponseDelay is { } delay)
            await Task.Delay(delay, ctx.RequestAborted).ConfigureAwait(false);
    }

    /// <summary>
    /// Resolves the OCPI status code to use for the response envelope
    /// (per-endpoint override > global override > 1000).
    /// </summary>
    public static int ResolveOcpiStatusCode(HttpContext ctx, CpoSimulatorConfiguration config)
    {
        var endpointConfig = ResolveEndpointOverride(ctx.Request.Path.Value ?? "", config);
        return endpointConfig?.ForceStatusCode?.Value ?? config.ForceStatusCode?.Value ?? 1000;
    }

    private static EndpointFailureConfig? ResolveEndpointOverride(string requestPath, CpoSimulatorConfiguration config)
    {
        if (config.EndpointOverrides.Count == 0)
            return null;

        var segments = requestPath.TrimStart('/').Split('/');
        if (segments.Length < 2 || segments[0] != "ocpi")
            return null;

        // Skip country_code/party_id if present (2-char country code heuristic)
        var moduleStart = segments[1] is { Length: 2 } ? 3 : 1;
        if (moduleStart >= segments.Length)
            return null;

        // Try two-segment key first (e.g., "commands/START_SESSION"), then single
        if (moduleStart + 1 < segments.Length)
        {
            var twoSegment = $"{segments[moduleStart]}/{segments[moduleStart + 1]}";
            if (config.EndpointOverrides.TryGetValue(twoSegment, out var twoResult))
                return twoResult;
        }

        return config.EndpointOverrides.TryGetValue(segments[moduleStart], out var result) ? result : null;
    }
}
