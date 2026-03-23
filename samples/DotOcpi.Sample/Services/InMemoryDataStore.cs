using System.Collections.Concurrent;
using System.Text.Json;

namespace DotOcpi.Sample.Services;

/// <summary>
/// Simple in-memory store for OCPI data received from CPOs.
/// Keyed by "{cpoId}:{objectId}" for per-CPO isolation.
/// In production, replace with a database.
/// </summary>
public sealed class InMemoryDataStore
{
    private const int MaxEntriesPerCollection = 1000;

    public ConcurrentDictionary<string, object> Locations { get; } = new();
    public ConcurrentDictionary<string, object> Sessions { get; } = new();
    public ConcurrentDictionary<string, object> Tariffs { get; } = new();
    public ConcurrentDictionary<string, object> Cdrs { get; } = new();

    /// <summary>Trims a collection to half capacity if it exceeds the max.</summary>
    public static void TrimIfNeeded(ConcurrentDictionary<string, object> dict)
    {
        if (dict.Count <= MaxEntriesPerCollection) return;
        var keysToRemove = dict.Keys.Take(dict.Count - MaxEntriesPerCollection / 2);
        foreach (var key in keysToRemove)
            dict.TryRemove(key, out _);
    }

    public static string Key(string cpoId, string objectId) => $"{cpoId}:{objectId}";

    public static string? ExtractId(object data)
    {
        var json = JsonSerializer.SerializeToElement(data);

        // Check both snake_case (OCPI wire format) and PascalCase (default serializer).
        // Id may be a string or a CiString (which serializes as an object with Value property).
        if (json.TryGetProperty("id", out var id))
            return ExtractStringValue(id);
        if (json.TryGetProperty("Id", out id))
            return ExtractStringValue(id);

        return null;
    }

    private static string? ExtractStringValue(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            // CiString serializes as { "Value": "..." } with default serializer
            JsonValueKind.Object when element.TryGetProperty("Value", out var v) => v.GetString(),
            _ => element.ToString(),
        };
}
