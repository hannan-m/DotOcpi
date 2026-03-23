using System.Text.Json;

namespace DotOcpi.Modules;

/// <summary>
/// Consumer interface for receiving Tariff data pushed by CPOs.
/// </summary>
public interface ITariffsReceiver
{
    /// <summary>Handles a Tariff PUT from a CPO.</summary>
    Task<OcpiResult> OnTariffPutAsync(OcpiRequestContext context, string tariffId, object data, CancellationToken ct);

    /// <summary>Handles a Tariff PATCH from a CPO (2.0/2.1.1 only; 2.2+ rejects PATCH with 405).</summary>
    Task<OcpiResult> OnTariffPatchAsync(
        OcpiRequestContext context,
        string tariffId,
        JsonElement patch,
        CancellationToken ct
    );

    /// <summary>Handles a Tariff DELETE from a CPO.</summary>
    Task<OcpiResult> OnTariffDeleteAsync(OcpiRequestContext context, string tariffId, CancellationToken ct);

    /// <summary>Retrieves a Tariff by ID.</summary>
    Task<OcpiResult<object>> GetTariffAsync(OcpiRequestContext context, string tariffId, CancellationToken ct);
}
