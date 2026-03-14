namespace DotOcpi.Validation;

/// <summary>
/// Validates an OCPI model against protocol rules for the negotiated version.
/// Transport-layer validation (JSON schema, required fields) is handled by
/// deserialization; these validators enforce OCPI protocol semantics.
/// </summary>
/// <typeparam name="T">The OCPI model type to validate.</typeparam>
public interface IOcpiValidator<in T>
{
    /// <summary>
    /// Validates the model and returns all protocol-level errors found.
    /// </summary>
    /// <param name="model">The model instance to validate.</param>
    /// <returns>Validation result with any errors.</returns>
    OcpiValidationResult Validate(T model);
}
