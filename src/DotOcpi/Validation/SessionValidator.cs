using DotOcpi.Models.V2_2_1;

namespace DotOcpi.Validation;

/// <summary>
/// Validates Session models against OCPI 2.2.1 protocol rules.
/// </summary>
public sealed class SessionValidator : IOcpiValidator<Session>
{
    /// <inheritdoc />
    public OcpiValidationResult Validate(Session model)
    {
        var errors = new List<OcpiValidationError>();

        ValidateKwh(model, errors);
        ValidateTimeRange(model, errors);
        ValidateCurrency(model, errors);
        ValidateCompletedSession(model, errors);

        return errors.Count == 0 ? OcpiValidationResult.Valid() : OcpiValidationResult.Failed(errors);
    }

    private static void ValidateKwh(Session model, List<OcpiValidationError> errors)
    {
        if (model.Kwh < 0)
        {
            errors.Add(
                new OcpiValidationError(
                    "SESSION_NEGATIVE_KWH",
                    $"Kwh value {model.Kwh} is negative.",
                    "Energy delivered must be zero or positive."
                )
                {
                    PropertyPath = "Kwh",
                }
            );
        }
    }

    private static void ValidateTimeRange(Session model, List<OcpiValidationError> errors)
    {
        if (model.EndDateTime is { } endDateTime && endDateTime < model.StartDateTime)
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
    }

    private static void ValidateCurrency(Session model, List<OcpiValidationError> errors)
    {
        if (model.Currency.Length != 3 || !model.Currency.All(char.IsLetter))
        {
            errors.Add(
                new OcpiValidationError(
                    "SESSION_INVALID_CURRENCY",
                    $"Currency '{model.Currency}' is not a valid ISO 4217 code.",
                    "Provide a 3-letter ISO 4217 currency code (e.g. 'EUR', 'USD')."
                )
                {
                    PropertyPath = "Currency",
                }
            );
        }
    }

    private static void ValidateCompletedSession(Session model, List<OcpiValidationError> errors)
    {
        if (model.Status == SessionStatus.COMPLETED && model.EndDateTime is null)
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
