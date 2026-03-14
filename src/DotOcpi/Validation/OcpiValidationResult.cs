namespace DotOcpi.Validation;

/// <summary>
/// Result of validating an OCPI model. Contains a list of errors (empty if valid).
/// </summary>
public sealed class OcpiValidationResult
{
    private static readonly OcpiValidationResult ValidInstance = new([]);

    private OcpiValidationResult(IReadOnlyList<OcpiValidationError> errors)
    {
        Errors = errors;
    }

    /// <summary>All validation errors found. Empty if the model is valid.</summary>
    public IReadOnlyList<OcpiValidationError> Errors { get; }

    /// <summary>True if no validation errors were found.</summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>Creates a successful validation result with no errors.</summary>
    public static OcpiValidationResult Valid() => ValidInstance;

    /// <summary>Creates a failed validation result with the given errors.</summary>
    public static OcpiValidationResult Failed(IReadOnlyList<OcpiValidationError> errors) => new(errors);

    /// <summary>Creates a failed validation result with a single error.</summary>
    public static OcpiValidationResult Failed(OcpiValidationError error) => new([error]);
}
