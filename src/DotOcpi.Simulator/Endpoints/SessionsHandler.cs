using DotOcpi.Simulator.Infrastructure;
using DotOcpi.Simulator.Models;
using DotOcpi.Simulator.State;
using Microsoft.AspNetCore.Http;

namespace DotOcpi.Simulator.Endpoints;

internal static class SessionsHandler
{
    public static async Task HandleGetList(HttpContext ctx, SimulatorState state, CpoSimulatorConfiguration config)
    {
        await FailureInjectionHelper.ApplyAsync(ctx, config).ConfigureAwait(false);
        if (!await AuthHelper.ValidateModuleAuthAsync(ctx, state, config).ConfigureAwait(false))
            return;

        var ocpiStatus = FailureInjectionHelper.ResolveOcpiStatusCode(ctx, config);

        if (config.IsLegacyMode)
        {
            await LocationsHandler.WriteLegacyList(ctx, config.Sessions, ocpiStatus).ConfigureAwait(false);
            return;
        }

        var conn = state.DefaultConnection;
        var version = conn?.NegotiatedVersion ?? config.SupportedVersions[0];
        var sessions = state.Sessions.Values.ToList();
        var items = sessions
            .Select(s =>
            {
                var loc = state.FindLocation(s.LocationId) ?? LocationSpec.Default;
                return VersionModelBuilder.BuildSession(
                    version,
                    s,
                    config.CpoIdentity,
                    config.EmspIdentity,
                    loc,
                    config.CpoCurrency
                );
            })
            .ToList();
        await LocationsHandler.WritePagedList(ctx, version, items).ConfigureAwait(false);
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

        if (config.IsLegacyMode)
        {
            await LocationsHandler.WriteLegacySingleItem(ctx, config.Sessions, itemId).ConfigureAwait(false);
            return;
        }

        if (!state.Sessions.TryGetValue(itemId, out var session))
        {
            await OcpiResponseWriter
                .WriteErrorAsync(ctx, 404, 2003, $"Session '{itemId}' not found.")
                .ConfigureAwait(false);
            return;
        }

        var conn = state.DefaultConnection;
        var version = conn?.NegotiatedVersion ?? config.SupportedVersions[0];
        var loc = state.FindLocation(session.LocationId) ?? LocationSpec.Default;
        var model = VersionModelBuilder.BuildSession(
            version,
            session,
            config.CpoIdentity,
            config.EmspIdentity,
            loc,
            config.CpoCurrency
        );
        await OcpiResponseWriter.WriteDataAsync(ctx, version, model).ConfigureAwait(false);
    }
}
