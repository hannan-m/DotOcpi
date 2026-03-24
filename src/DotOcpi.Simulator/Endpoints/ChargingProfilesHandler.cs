using DotOcpi.Simulator.Infrastructure;
using DotOcpi.Simulator.State;
using Microsoft.AspNetCore.Http;

namespace DotOcpi.Simulator.Endpoints;

internal static class ChargingProfilesHandler
{
    public static async Task HandlePut(
        HttpContext ctx,
        string sessionId,
        SimulatorState state,
        CpoSimulatorConfiguration config
    )
    {
        await FailureInjectionHelper.ApplyAsync(ctx, config).ConfigureAwait(false);
        if (!await AuthHelper.ValidateModuleAuthAsync(ctx, state, config).ConfigureAwait(false))
            return;

        var body = RequestRecordingMiddleware.GetParsedBody(ctx) ?? default;
        state.ChargingProfiles[sessionId] = body;

        await OcpiResponseWriter.WriteSuccessAsync(ctx).ConfigureAwait(false);
    }

    public static async Task HandleDelete(
        HttpContext ctx,
        string sessionId,
        SimulatorState state,
        CpoSimulatorConfiguration config
    )
    {
        await FailureInjectionHelper.ApplyAsync(ctx, config).ConfigureAwait(false);
        if (!await AuthHelper.ValidateModuleAuthAsync(ctx, state, config).ConfigureAwait(false))
            return;

        state.ChargingProfiles.TryRemove(sessionId, out _);
        await OcpiResponseWriter.WriteSuccessAsync(ctx).ConfigureAwait(false);
    }

    public static async Task HandleGet(
        HttpContext ctx,
        string sessionId,
        SimulatorState state,
        CpoSimulatorConfiguration config
    )
    {
        await FailureInjectionHelper.ApplyAsync(ctx, config).ConfigureAwait(false);
        if (!await AuthHelper.ValidateModuleAuthAsync(ctx, state, config).ConfigureAwait(false))
            return;

        if (!state.ChargingProfiles.TryGetValue(sessionId, out var profile))
        {
            await OcpiResponseWriter
                .WriteErrorAsync(ctx, 404, 2003, $"No charging profile for session '{sessionId}'.")
                .ConfigureAwait(false);
            return;
        }

        await OcpiResponseWriter.WriteLegacyDataAsync(ctx, profile).ConfigureAwait(false);
    }
}
