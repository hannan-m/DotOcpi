namespace DotOcpi.Validation;

/// <summary>
/// Validates Session models against OCPI protocol rules across all supported versions.
/// All versions share the same validation rules: non-negative kWh, valid time range,
/// ISO 4217 currency, and completed sessions must have an end timestamp.
/// </summary>
public sealed class SessionValidator
    : IOcpiValidator<Models.V2_0.Session>,
        IOcpiValidator<Models.V2_1_1.Session>,
        IOcpiValidator<Models.V2_2.Session>,
        IOcpiValidator<Models.V2_2_1.Session>
{
    /// <inheritdoc />
    public OcpiValidationResult Validate(Models.V2_0.Session model)
    {
        var errors = new List<OcpiValidationError>();
        ValidateCore(model.Kwh, model.StartDateTime, model.EndDateTime, model.Currency, model.Status, errors);
        return errors.Count == 0 ? OcpiValidationResult.Valid() : OcpiValidationResult.Failed(errors);
    }

    /// <inheritdoc />
    public OcpiValidationResult Validate(Models.V2_1_1.Session model)
    {
        var errors = new List<OcpiValidationError>();
        ValidateCore(model.Kwh, model.StartDateTime, model.EndDateTime, model.Currency, model.Status, errors);
        return errors.Count == 0 ? OcpiValidationResult.Valid() : OcpiValidationResult.Failed(errors);
    }

    /// <inheritdoc />
    public OcpiValidationResult Validate(Models.V2_2.Session model)
    {
        var errors = new List<OcpiValidationError>();
        ValidateCore(model.Kwh, model.StartDateTime, model.EndDateTime, model.Currency, model.Status, errors);
        return errors.Count == 0 ? OcpiValidationResult.Valid() : OcpiValidationResult.Failed(errors);
    }

    /// <inheritdoc />
    public OcpiValidationResult Validate(Models.V2_2_1.Session model)
    {
        var errors = new List<OcpiValidationError>();
        ValidateCore(model.Kwh, model.StartDateTime, model.EndDateTime, model.Currency, model.Status, errors);
        return errors.Count == 0 ? OcpiValidationResult.Valid() : OcpiValidationResult.Failed(errors);
    }

    OcpiValidationResult IOcpiValidator.Validate(object model) =>
        model switch
        {
            Models.V2_0.Session m => Validate(m),
            Models.V2_1_1.Session m => Validate(m),
            Models.V2_2.Session m => Validate(m),
            Models.V2_2_1.Session m => Validate(m),
            _ => throw new ArgumentException($"Unsupported Session type: {model.GetType().Name}", nameof(model)),
        };

    // SessionStatus values are identical across versions (ACTIVE, COMPLETED, etc.)
    // but they're separate enum types per namespace, so we accept int to unify.
    private static void ValidateCore(
        decimal kwh,
        DateTimeOffset startDateTime,
        DateTimeOffset? endDateTime,
        string currency,
        object status,
        List<OcpiValidationError> errors
    )
    {
        if (kwh < 0)
        {
            errors.Add(
                new OcpiValidationError(
                    "SESSION_NEGATIVE_KWH",
                    $"Kwh value {kwh} is negative.",
                    "Energy delivered must be zero or positive."
                )
                {
                    PropertyPath = "Kwh",
                }
            );
        }

        if (endDateTime is { } end && end < startDateTime)
        {
            errors.Add(
                new OcpiValidationError(
                    "SESSION_END_BEFORE_START",
                    "EndDateTime is before StartDateTime.",
                    "EndDateTime must be equal to or after StartDateTime."
                )
                {
                    PropertyPath = "EndDateTime",
                }
            );
        }

        ValidationHelpers.ValidateCurrency(currency, "SESSION_INVALID_CURRENCY", errors);

        // COMPLETED status check — compare by string name to avoid coupling to version-specific enums
        if (status.ToString() == "COMPLETED" && endDateTime is null)
        {
            errors.Add(
                new OcpiValidationError(
                    "SESSION_COMPLETED_WITHOUT_END",
                    "Session status is COMPLETED but EndDateTime is not set.",
                    "Set EndDateTime when marking a session as COMPLETED."
                )
                {
                    PropertyPath = "EndDateTime",
                }
            );
        }
    }
}
