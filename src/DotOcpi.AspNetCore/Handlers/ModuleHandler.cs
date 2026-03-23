using System.Text.Json;

namespace DotOcpi.AspNetCore.Handlers;

/// <summary>
/// Generic module handler that deserializes request bodies to a specific model type
/// using version-appropriate JSON serializer options.
/// </summary>
internal sealed class ModuleHandler<T> : IModuleHandler
    where T : class
{
    private readonly JsonSerializerOptions _options;

    public ModuleHandler(JsonSerializerOptions options)
    {
        _options = options;
    }

    public async ValueTask<object?> DeserializeAsync(Stream body, CancellationToken ct)
    {
        return await JsonSerializer.DeserializeAsync(body, _options.GetTypeInfo(typeof(T)), ct).ConfigureAwait(false);
    }
}
