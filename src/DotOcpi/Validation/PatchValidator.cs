using System.Text.Json;

namespace DotOcpi.Validation;

/// <summary>
/// Validates OCPI PATCH request bodies. Ensures the patch document is a
/// JSON object and does not contain unknown top-level fields.
/// </summary>
public sealed class PatchValidator
{
    private readonly HashSet<string> _allowedFields;

    /// <summary>
    /// Creates a PatchValidator that accepts the given snake_case field names.
    /// </summary>
    /// <param name="allowedFields">The set of valid snake_case field names for the target type.</param>
    public PatchValidator(IEnumerable<string> allowedFields)
    {
        _allowedFields = new HashSet<string>(allowedFields, StringComparer.Ordinal);
    }

    /// <summary>
    /// Validates a patch document against the allowed field set.
    /// </summary>
    /// <param name="patch">The JSON patch element.</param>
    /// <returns>Validation result with any errors.</returns>
    public OcpiValidationResult Validate(JsonElement patch)
    {
        if (patch.ValueKind != JsonValueKind.Object)
        {
            return OcpiValidationResult.Failed(
                new OcpiValidationError(
                    "PATCH_NOT_OBJECT",
                    "PATCH body must be a JSON object.",
                    "Send a JSON object with the fields to update."
                )
            );
        }

        var errors = new List<OcpiValidationError>();

        foreach (var property in patch.EnumerateObject())
        {
            if (!_allowedFields.Contains(property.Name))
            {
                errors.Add(
                    new OcpiValidationError(
                        "PATCH_UNKNOWN_FIELD",
                        $"Unknown field '{property.Name}' in PATCH body.",
                        $"Remove '{property.Name}' or check field name spelling."
                    )
                    {
                        PropertyPath = property.Name,
                    }
                );
            }
        }

        return errors.Count == 0 ? OcpiValidationResult.Valid() : OcpiValidationResult.Failed(errors);
    }
}
