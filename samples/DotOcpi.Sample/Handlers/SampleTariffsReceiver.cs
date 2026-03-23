using System.Text.Json;
using DotOcpi.Modules;
using DotOcpi.Sample.Services;

namespace DotOcpi.Sample.Handlers;

/// <summary>
/// Receives tariff pushes from CPOs. Stores and retrieves from <see cref="InMemoryDataStore"/>.
/// </summary>
public sealed partial class SampleTariffsReceiver(
    InMemoryDataStore store,
    ILogger<SampleTariffsReceiver> logger
) : ITariffsReceiver
{
    private readonly ILogger _logger = logger;

    public Task<OcpiResult> OnTariffPutAsync(
        OcpiRequestContext context, string tariffId, object data, CancellationToken ct)
    {
        store.Tariffs[InMemoryDataStore.Key(context.CpoId, tariffId)] = data;
        LogTariffPut(tariffId, context.CpoId);
        return Task.FromResult(OcpiResult.Success());
    }

    public Task<OcpiResult> OnTariffPatchAsync(
        OcpiRequestContext context, string tariffId, JsonElement patch, CancellationToken ct)
    {
        LogTariffPatch(tariffId, context.CpoId);
        return Task.FromResult(OcpiResult.Success());
    }

    public Task<OcpiResult> OnTariffDeleteAsync(
        OcpiRequestContext context, string tariffId, CancellationToken ct)
    {
        store.Tariffs.TryRemove(InMemoryDataStore.Key(context.CpoId, tariffId), out _);
        LogTariffDelete(tariffId, context.CpoId);
        return Task.FromResult(OcpiResult.Success());
    }

    public Task<OcpiResult<object>> GetTariffAsync(
        OcpiRequestContext context, string tariffId, CancellationToken ct)
    {
        var key = InMemoryDataStore.Key(context.CpoId, tariffId);
        if (store.Tariffs.TryGetValue(key, out var tariff))
            return Task.FromResult(OcpiResult<object>.Success(tariff));

        return Task.FromResult(
            OcpiResult<object>.Failure(OcpiStatusCode.GenericClientError, $"Tariff '{tariffId}' not found."));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "[Tariffs] PUT tariff {TariffId} from {CpoId}")]
    private partial void LogTariffPut(string tariffId, string cpoId);

    [LoggerMessage(Level = LogLevel.Information, Message = "[Tariffs] PATCH tariff {TariffId} from {CpoId}")]
    private partial void LogTariffPatch(string tariffId, string cpoId);

    [LoggerMessage(Level = LogLevel.Information, Message = "[Tariffs] DELETE tariff {TariffId} from {CpoId}")]
    private partial void LogTariffDelete(string tariffId, string cpoId);
}
