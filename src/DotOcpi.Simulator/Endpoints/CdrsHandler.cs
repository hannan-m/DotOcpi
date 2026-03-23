using DotOcpi.Simulator.Infrastructure;
using DotOcpi.Simulator.State;
using Microsoft.AspNetCore.Http;

namespace DotOcpi.Simulator.Endpoints;

internal static class CdrsHandler
{
    public static async Task HandleGetList(HttpContext ctx, SimulatorState state, CpoSimulatorConfiguration config)
    {
        await FailureInjectionHelper.ApplyAsync(ctx, config).ConfigureAwait(false);
        if (!await AuthHelper.ValidateModuleAuthAsync(ctx, state, config).ConfigureAwait(false))
            return;

        var ocpiStatus = FailureInjectionHelper.ResolveOcpiStatusCode(ctx, config);

        if (!config.IsLegacyMode)
        {
            var conn = state.DefaultConnection;
            var version = conn?.NegotiatedVersion ?? config.SupportedVersions[0];
            List<object> snapshot;
            lock (config.Cdrs)
            {
                snapshot = config.Cdrs.ToList();
            }
            await LocationsHandler.WritePagedList(ctx, version, snapshot).ConfigureAwait(false);
            return;
        }

        await LocationsHandler.WriteLegacyList(ctx, config.Cdrs, ocpiStatus).ConfigureAwait(false);
    }

    public static async Task HandleGetSingle(
        HttpContext ctx,
        string itemId,
        SimulatorState state,
        CpoSimulatorConfiguration config
    )
    {
        await FailureInjectionHelper.ApplyAsync(ctx, config).ConfigureAwait(false);
        if (!await AuthHelper.ValidateModuleAuthAsync(ctx, state, config).ConfigureAwait(false))
            return;

        await LocationsHandler.WriteLegacySingleItem(ctx, config.Cdrs, itemId).ConfigureAwait(false);
    }
}
