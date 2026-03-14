namespace DotOcpi.AspNetCore.Handlers;

/// <summary>
/// Provides version-specific deserialization of OCPI request bodies.
/// Implementations target a specific model type for a specific OCPI version.
/// </summary>
internal interface IModuleHandler
{
    /// <summary>
    /// Deserializes the request body to the version-specific model type.
    /// </summary>
    /// <returns>The deserialized model, or null if the body is empty.</returns>
    ValueTask<object?> DeserializeAsync(Stream body, CancellationToken ct);
}
