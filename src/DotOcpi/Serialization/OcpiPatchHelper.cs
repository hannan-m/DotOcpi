using System.Text.Json;

namespace DotOcpi.Serialization;

/// <summary>
/// Applies JSON merge patch (RFC 7396) to OCPI model JSON.
/// In OCPI PATCH semantics: present fields are updated, absent fields are unchanged,
/// explicit null removes the field.
/// </summary>
public static class OcpiPatchHelper
{
    /// <summary>
    /// Merges a patch document into a target document using JSON merge patch semantics.
    /// </summary>
    /// <param name="target">The current JSON document.</param>
    /// <param name="patch">The patch to apply.</param>
    /// <returns>The merged result as a JsonElement.</returns>
    public static JsonElement ApplyPatch(JsonElement target, JsonElement patch)
    {
        if (patch.ValueKind != JsonValueKind.Object)
        {
            return patch.Clone();
        }

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            MergeObjects(writer, target, patch);
        }

        return JsonDocument.Parse(stream.ToArray()).RootElement;
    }

    private static void MergeObjects(Utf8JsonWriter writer, JsonElement target, JsonElement patch)
    {
        writer.WriteStartObject();

        // Write all existing target properties, applying patch overrides
        if (target.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in target.EnumerateObject())
            {
                if (patch.TryGetProperty(property.Name, out var patchValue))
                {
                    // Explicit null in patch removes the field
                    if (patchValue.ValueKind == JsonValueKind.Null)
                    {
                        continue;
                    }

                    writer.WritePropertyName(property.Name);

                    if (
                        patchValue.ValueKind == JsonValueKind.Object
                        && property.Value.ValueKind == JsonValueKind.Object
                    )
                    {
                        MergeObjects(writer, property.Value, patchValue);
                    }
                    else
                    {
                        patchValue.WriteTo(writer);
                    }
                }
                else
                {
                    // Not in patch — keep original
                    writer.WritePropertyName(property.Name);
                    property.Value.WriteTo(writer);
                }
            }
        }

        // Write new properties from patch that don't exist in target
        foreach (var property in patch.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Null)
            {
                continue;
            }

            if (target.ValueKind != JsonValueKind.Object || !target.TryGetProperty(property.Name, out _))
            {
                writer.WritePropertyName(property.Name);
                property.Value.WriteTo(writer);
            }
        }

        writer.WriteEndObject();
    }
}
