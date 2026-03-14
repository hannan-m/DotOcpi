using System.Text.Json;

namespace DotOcpi.Modules;

/// <summary>
/// Consumer interface for receiving Location data pushed by CPOs.
/// Implement this to handle PUT/PATCH/GET operations on Locations, EVSEs, and Connectors.
/// The <paramref name="data"/> parameter is the version-specific model type (cast based on context version).
/// </summary>
public interface ILocationsReceiver
{
    /// <summary>Handles a full Location PUT from a CPO.</summary>
    Task<OcpiResult> OnLocationPutAsync(OcpiRequestContext context, string locationId, object data, CancellationToken ct);

    /// <summary>Handles a Location PATCH from a CPO.</summary>
    Task<OcpiResult> OnLocationPatchAsync(OcpiRequestContext context, string locationId, JsonElement patch, CancellationToken ct);

    /// <summary>Handles an EVSE PUT from a CPO.</summary>
    Task<OcpiResult> OnEvsePutAsync(OcpiRequestContext context, string locationId, string evseUid, object data, CancellationToken ct);

    /// <summary>Handles an EVSE PATCH from a CPO.</summary>
    Task<OcpiResult> OnEvsePatchAsync(OcpiRequestContext context, string locationId, string evseUid, JsonElement patch, CancellationToken ct);

    /// <summary>Handles a Connector PUT from a CPO.</summary>
    Task<OcpiResult> OnConnectorPutAsync(OcpiRequestContext context, string locationId, string evseUid, string connectorId, object data, CancellationToken ct);

    /// <summary>Handles a Connector PATCH from a CPO.</summary>
    Task<OcpiResult> OnConnectorPatchAsync(OcpiRequestContext context, string locationId, string evseUid, string connectorId, JsonElement patch, CancellationToken ct);

    /// <summary>Retrieves a Location by ID (for CPO GET requests).</summary>
    Task<OcpiResult<object>> GetLocationAsync(OcpiRequestContext context, string locationId, CancellationToken ct);
}
