using System.Collections.Concurrent;
using System.Text.Json;
using DotOcpi.Modules;

namespace DotOcpi.Integration.Tests.Fixtures;

/// <summary>
/// Captures location push events for test assertions.
/// </summary>
public sealed class CapturingLocationsReceiver : ILocationsReceiver
{
    public ConcurrentBag<(string LocationId, object Data)> PutLocations { get; } = [];
    public ConcurrentBag<(string LocationId, JsonElement Patch)> PatchedLocations { get; } = [];

    public Task<OcpiResult> OnLocationPutAsync(
        OcpiRequestContext context,
        string locationId,
        object data,
        CancellationToken ct = default
    )
    {
        PutLocations.Add((locationId, data));
        return Task.FromResult(OcpiResult.Success());
    }

    public Task<OcpiResult> OnLocationPatchAsync(
        OcpiRequestContext context,
        string locationId,
        JsonElement patch,
        CancellationToken ct = default
    )
    {
        PatchedLocations.Add((locationId, patch));
        return Task.FromResult(OcpiResult.Success());
    }

    public Task<OcpiResult> OnEvsePutAsync(
        OcpiRequestContext context,
        string locationId,
        string evseUid,
        object data,
        CancellationToken ct = default
    ) => Task.FromResult(OcpiResult.Success());

    public Task<OcpiResult> OnEvsePatchAsync(
        OcpiRequestContext context,
        string locationId,
        string evseUid,
        JsonElement patch,
        CancellationToken ct = default
    ) => Task.FromResult(OcpiResult.Success());

    public Task<OcpiResult> OnConnectorPutAsync(
        OcpiRequestContext context,
        string locationId,
        string evseUid,
        string connectorId,
        object data,
        CancellationToken ct = default
    ) => Task.FromResult(OcpiResult.Success());

    public Task<OcpiResult> OnConnectorPatchAsync(
        OcpiRequestContext context,
        string locationId,
        string evseUid,
        string connectorId,
        JsonElement patch,
        CancellationToken ct = default
    ) => Task.FromResult(OcpiResult.Success());

    public Task<OcpiResult<object>> GetLocationAsync(
        OcpiRequestContext context,
        string locationId,
        CancellationToken ct = default
    ) => Task.FromResult(OcpiResult<object>.Failure(new OcpiStatusCode(2003), "Not found"));
}
