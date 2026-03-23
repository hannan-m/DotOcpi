using DotOcpi.Simulator.Infrastructure;
using DotOcpi.Simulator.Models;
using DotOcpi.Simulator.State;
using Microsoft.AspNetCore.Http;

namespace DotOcpi.Simulator.Endpoints;

internal static class TariffsHandler
{
    public static async Task HandleGetList(HttpContext ctx, SimulatorState state, CpoSimulatorConfiguration config)
    {
        await FailureInjectionHelper.ApplyAsync(ctx, config).ConfigureAwait(false);
        if (!await AuthHelper.ValidateModuleAuthAsync(ctx, state, config).ConfigureAwait(false))
            return;

        var ocpiStatus = FailureInjectionHelper.ResolveOcpiStatusCode(ctx, config);

        if (config.IsLegacyMode)
        {
            await LocationsHandler.WriteLegacyList(ctx, config.Tariffs, ocpiStatus).ConfigureAwait(false);
            return;
        }

        var conn = state.DefaultConnection;
        var version = conn?.NegotiatedVersion ?? config.SupportedVersions[0];
        var tariffs = state.GetTariffs();
        var items = tariffs.Select(t => VersionModelBuilder.BuildTariff(version, t, config.CpoIdentity)).ToList();
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
            await LocationsHandler.WriteLegacySingleItem(ctx, config.Tariffs, itemId).ConfigureAwait(false);
            return;
        }

        var tariff = state.FindTariff(itemId);
        if (tariff is null)
        {
            await OcpiResponseWriter
                .WriteErrorAsync(ctx, 404, 2003, $"Tariff '{itemId}' not found.")
                .ConfigureAwait(false);
            return;
        }

        var conn = state.DefaultConnection;
        var version = conn?.NegotiatedVersion ?? config.SupportedVersions[0];
        var model = VersionModelBuilder.BuildTariff(version, tariff, config.CpoIdentity);
        await OcpiResponseWriter.WriteDataAsync(ctx, version, model).ConfigureAwait(false);
    }
}
