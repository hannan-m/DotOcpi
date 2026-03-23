using System.Text.Json;
using DotOcpi.Modules;
using DotOcpi.Sample.Services;

namespace DotOcpi.Sample.Handlers;

/// <summary>
/// Receives location pushes from CPOs. Stores in <see cref="InMemoryDataStore"/>
/// and returns stored data on GET.
/// </summary>
public sealed partial class SampleLocationsReceiver(
    InMemoryDataStore store,
    ILogger<SampleLocationsReceiver> logger
) : ILocationsReceiver
{
    private readonly ILogger _logger = logger;

    public Task<OcpiResult> OnLocationPutAsync(
        OcpiRequestContext context, string locationId, object data, CancellationToken ct)
    {
        store.Locations[InMemoryDataStore.Key(context.CpoId, locationId)] = data;
        LogLocationPut(locationId, context.CpoId);
        return Task.FromResult(OcpiResult.Success());
    }

    public Task<OcpiResult> OnLocationPatchAsync(
        OcpiRequestContext context, string locationId, JsonElement patch, CancellationToken ct)
    {
        LogLocationPatch(locationId, context.CpoId);
        return Task.FromResult(OcpiResult.Success());
    }

    public Task<OcpiResult> OnEvsePutAsync(
        OcpiRequestContext context, string locationId, string evseUid, object data, CancellationToken ct)
    {
        LogEvsePut(evseUid, locationId, context.CpoId);
        return Task.FromResult(OcpiResult.Success());
    }

    public Task<OcpiResult> OnEvsePatchAsync(
        OcpiRequestContext context, string locationId, string evseUid, JsonElement patch, CancellationToken ct)
    {
        LogEvsePatch(evseUid, locationId, context.CpoId);
        return Task.FromResult(OcpiResult.Success());
    }

    public Task<OcpiResult> OnConnectorPutAsync(
        OcpiRequestContext context, string locationId, string evseUid, string connectorId, object data, CancellationToken ct)
    {
        LogConnectorPut(connectorId, evseUid, locationId);
        return Task.FromResult(OcpiResult.Success());
    }

    public Task<OcpiResult> OnConnectorPatchAsync(
        OcpiRequestContext context, string locationId, string evseUid, string connectorId, JsonElement patch, CancellationToken ct)
    {
        LogConnectorPatch(connectorId, evseUid, locationId);
        return Task.FromResult(OcpiResult.Success());
    }

    public Task<OcpiResult<object>> GetLocationAsync(
        OcpiRequestContext context, string locationId, CancellationToken ct)
    {
        var key = InMemoryDataStore.Key(context.CpoId, locationId);
        if (store.Locations.TryGetValue(key, out var location))
            return Task.FromResult(OcpiResult<object>.Success(location));

        return Task.FromResult(
            OcpiResult<object>.Failure(OcpiStatusCode.UnknownLocation, $"Location '{locationId}' not found."));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "[Locations] PUT location {LocationId} from {CpoId}")]
    private partial void LogLocationPut(string locationId, string cpoId);

    [LoggerMessage(Level = LogLevel.Information, Message = "[Locations] PATCH location {LocationId} from {CpoId}")]
    private partial void LogLocationPatch(string locationId, string cpoId);

    [LoggerMessage(Level = LogLevel.Information, Message = "[Locations] PUT EVSE {EvseUid} at {LocationId} from {CpoId}")]
    private partial void LogEvsePut(string evseUid, string locationId, string cpoId);

    [LoggerMessage(Level = LogLevel.Information, Message = "[Locations] PATCH EVSE {EvseUid} at {LocationId} from {CpoId}")]
    private partial void LogEvsePatch(string evseUid, string locationId, string cpoId);

    [LoggerMessage(Level = LogLevel.Information, Message = "[Locations] PUT connector {ConnectorId} on EVSE {EvseUid} at {LocationId}")]
    private partial void LogConnectorPut(string connectorId, string evseUid, string locationId);

    [LoggerMessage(Level = LogLevel.Information, Message = "[Locations] PATCH connector {ConnectorId} on EVSE {EvseUid} at {LocationId}")]
    private partial void LogConnectorPatch(string connectorId, string evseUid, string locationId);
}
