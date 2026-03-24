using System.Text.Json;
using DotOcpi.Simulator.Infrastructure;
using DotOcpi.Simulator.Models;
using DotOcpi.Simulator.State;
using Microsoft.AspNetCore.Http;

namespace DotOcpi.Simulator.Endpoints;

internal static class LocationsHandler
{
    public static async Task HandleGetList(HttpContext ctx, SimulatorState state, CpoSimulatorConfiguration config)
    {
        await FailureInjectionHelper.ApplyAsync(ctx, config).ConfigureAwait(false);
        if (!await AuthHelper.ValidateModuleAuthAsync(ctx, state, config).ConfigureAwait(false))
            return;

        var ocpiStatus = FailureInjectionHelper.ResolveOcpiStatusCode(ctx, config);

        if (config.IsLegacyMode)
        {
            await WriteLegacyList(ctx, config.Locations, ocpiStatus).ConfigureAwait(false);
            return;
        }

        var conn = state.DefaultConnection;
        var version = conn?.NegotiatedVersion ?? config.SupportedVersions[0];
        var locations = state.GetLocations();
        var items = locations
            .Select(l => VersionModelBuilder.BuildLocation(version, l, config.CpoIdentity, state.Evses))
            .ToList();
        await WritePagedList(ctx, version, items).ConfigureAwait(false);
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
            await WriteLegacySingleItem(ctx, config.Locations, itemId).ConfigureAwait(false);
            return;
        }

        var conn = state.DefaultConnection;
        var version = conn?.NegotiatedVersion ?? config.SupportedVersions[0];
        var spec = state.FindLocation(itemId);
        if (spec is null)
        {
            await OcpiResponseWriter
                .WriteErrorAsync(ctx, 404, 2003, $"Object '{itemId}' not found.")
                .ConfigureAwait(false);
            return;
        }

        var model = VersionModelBuilder.BuildLocation(version, spec, config.CpoIdentity, state.Evses);
        await OcpiResponseWriter.WriteDataAsync(ctx, version, model).ConfigureAwait(false);
    }

    public static async Task HandleGetEvse(
        HttpContext ctx,
        string locationId,
        string evseUid,
        SimulatorState state,
        CpoSimulatorConfiguration config
    )
    {
        await FailureInjectionHelper.ApplyAsync(ctx, config).ConfigureAwait(false);
        if (!await AuthHelper.ValidateModuleAuthAsync(ctx, state, config).ConfigureAwait(false))
            return;

        var conn = state.DefaultConnection;
        var version = conn?.NegotiatedVersion ?? config.SupportedVersions[0];
        var spec = state.FindLocation(locationId);
        var evseSpec = spec?.Evses.FirstOrDefault(e => e.Uid == evseUid);
        if (evseSpec is null)
        {
            await OcpiResponseWriter
                .WriteErrorAsync(ctx, 404, 2003, $"EVSE '{evseUid}' not found.")
                .ConfigureAwait(false);
            return;
        }

        var model = VersionModelBuilder.BuildEvse(version, evseSpec, state.Evses, locationId);
        await OcpiResponseWriter.WriteDataAsync(ctx, version, model).ConfigureAwait(false);
    }

    public static async Task HandleGetConnector(
        HttpContext ctx,
        string locationId,
        string evseUid,
        string connectorId,
        SimulatorState state,
        CpoSimulatorConfiguration config
    )
    {
        await FailureInjectionHelper.ApplyAsync(ctx, config).ConfigureAwait(false);
        if (!await AuthHelper.ValidateModuleAuthAsync(ctx, state, config).ConfigureAwait(false))
            return;

        var spec = state.FindLocation(locationId);
        var evseSpec = spec?.Evses.FirstOrDefault(e => e.Uid == evseUid);
        if (evseSpec is null || evseSpec.Connector.Id != connectorId)
        {
            await OcpiResponseWriter
                .WriteErrorAsync(ctx, 404, 2003, $"Connector '{connectorId}' not found.")
                .ConfigureAwait(false);
            return;
        }

        var conn = state.DefaultConnection;
        var version = conn?.NegotiatedVersion ?? config.SupportedVersions[0];
        var model = VersionModelBuilder.BuildConnector(version, evseSpec.Connector);
        await OcpiResponseWriter.WriteDataAsync(ctx, version, model).ConfigureAwait(false);
    }

    // ── Legacy helpers (anonymous object mode) ────────────────

    internal static async Task WriteLegacyList(HttpContext ctx, List<object> items, int? ocpiStatusOverride = null)
    {
        var query = ctx.Request.Query;
        var offset = int.TryParse(query["offset"], out var o) ? Math.Max(0, o) : 0;
        int snapshotCount;
        lock (items)
        {
            snapshotCount = items.Count;
        }
        var limit = int.TryParse(query["limit"], out var l) ? Math.Max(1, l) : 50;

        List<object> snapshot;
        lock (items)
        {
            snapshot = items.ToList();
        }

        if (query.ContainsKey("date_from") && DateTimeOffset.TryParse(query["date_from"], out var dateFrom))
            snapshot = snapshot.Where(item => HasLastUpdatedAfter(item, dateFrom)).ToList();
        if (query.ContainsKey("date_to") && DateTimeOffset.TryParse(query["date_to"], out var dateTo))
            snapshot = snapshot.Where(item => HasLastUpdatedBefore(item, dateTo)).ToList();

        var totalCount = snapshot.Count;
        var page = snapshot.Skip(offset).Take(limit).ToList();
        await OcpiResponseWriter
            .WriteLegacyListAsync(ctx, page, totalCount, offset, limit, ocpiStatusOverride ?? 1000)
            .ConfigureAwait(false);
    }

    internal static async Task WriteLegacySingleItem(HttpContext ctx, List<object> items, string itemId)
    {
        var item = FindItemById(items, itemId);
        if (item is null)
        {
            await OcpiResponseWriter
                .WriteErrorAsync(ctx, 404, 2003, $"Object '{itemId}' not found.")
                .ConfigureAwait(false);
            return;
        }
        await OcpiResponseWriter.WriteLegacyDataAsync(ctx, item).ConfigureAwait(false);
    }

    internal static async Task WritePagedList(HttpContext ctx, OcpiVersion version, List<object> items)
    {
        var query = ctx.Request.Query;
        var offset = int.TryParse(query["offset"], out var o) ? Math.Max(0, o) : 0;
        var limit = int.TryParse(query["limit"], out var l) ? Math.Max(1, l) : 50;
        var totalCount = items.Count;
        var page = items.Skip(offset).Take(limit).ToList();
        await OcpiResponseWriter.WriteListAsync(ctx, version, page, totalCount, offset, limit).ConfigureAwait(false);
    }

    private static object? FindItemById(List<object> items, string id)
    {
        foreach (var item in items)
        {
            var json = JsonSerializer.SerializeToElement(item);
            if (json.TryGetProperty("id", out var idProp) && idProp.GetString() == id)
                return item;
        }
        return null;
    }

    private static bool HasLastUpdatedAfter(object item, DateTimeOffset threshold)
    {
        var json = JsonSerializer.SerializeToElement(item);
        return json.TryGetProperty("last_updated", out var lu)
            && DateTimeOffset.TryParse(lu.GetString(), out var ts)
            && ts >= threshold;
    }

    private static bool HasLastUpdatedBefore(object item, DateTimeOffset threshold)
    {
        var json = JsonSerializer.SerializeToElement(item);
        return json.TryGetProperty("last_updated", out var lu)
            && DateTimeOffset.TryParse(lu.GetString(), out var ts)
            && ts < threshold;
    }
}
