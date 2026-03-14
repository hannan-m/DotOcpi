namespace DotOcpi.Validation;

/// <summary>
/// A single validation error with a machine-readable code, message, and fix suggestion.
/// </summary>
/// <param name="Code">Machine-readable error code (e.g. "LOCATION_MISSING_COUNTRY").</param>
/// <param name="Message">Human-readable description of what failed.</param>
/// <param name="Suggestion">Actionable guidance on how to fix the error.</param>
public sealed record OcpiValidationError(string Code, string Message, string Suggestion)
{
    /// <summary>
    /// The property path where the error occurred (e.g. "Evses[0].Connectors[1].MaxVoltage").
    /// </summary>
    public string? PropertyPath { get; init; }
}
