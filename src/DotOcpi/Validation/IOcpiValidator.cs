namespace DotOcpi.Validation;

/// <summary>
/// Non-generic base for OCPI validators, enabling dispatch without reflection.
/// Resolved from DI using the generic <see cref="IOcpiValidator{T}"/> registration,
/// then invoked via this base interface when the model type is only known at runtime.
/// </summary>
public interface IOcpiValidator
{
    /// <summary>
    /// Validates the model and returns all protocol-level errors found.
    /// </summary>
    /// <param name="model">The model instance to validate.</param>
    /// <returns>Validation result with any errors.</returns>
    OcpiValidationResult Validate(object model);
}

/// <summary>
/// Validates an OCPI model against protocol rules for the negotiated version.
/// Transport-layer validation (JSON schema, required fields) is handled by
/// deserialization; these validators enforce OCPI protocol semantics.
/// </summary>
/// <typeparam name="T">The OCPI model type to validate.</typeparam>
public interface IOcpiValidator<in T> : IOcpiValidator
{
    /// <summary>
    /// Validates the model and returns all protocol-level errors found.
    /// </summary>
    /// <param name="model">The typed model instance to validate.</param>
    /// <returns>Validation result with any errors.</returns>
    OcpiValidationResult Validate(T model);

    /// <inheritdoc />
    OcpiValidationResult IOcpiValidator.Validate(object model) => Validate((T)model);
}
