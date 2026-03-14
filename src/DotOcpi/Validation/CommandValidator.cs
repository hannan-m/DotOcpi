using DotOcpi.Models.V2_2_1;

namespace DotOcpi.Validation;

/// <summary>
/// Validates OCPI command models (StartSession, StopSession, ReserveNow, etc.)
/// against protocol rules. All commands share HTTPS response URL validation.
/// </summary>
public sealed class CommandValidator
    : IOcpiValidator<StartSession>,
        IOcpiValidator<StopSession>,
        IOcpiValidator<ReserveNow>,
        IOcpiValidator<CancelReservation>,
        IOcpiValidator<UnlockConnector>
{
    /// <inheritdoc />
    public OcpiValidationResult Validate(StartSession model)
    {
        var errors = new List<OcpiValidationError>();
        ValidateResponseUrl(model.ResponseUrl, errors);
        return errors.Count == 0 ? OcpiValidationResult.Valid() : OcpiValidationResult.Failed(errors);
    }

    /// <inheritdoc />
    public OcpiValidationResult Validate(StopSession model)
    {
        var errors = new List<OcpiValidationError>();
        ValidateResponseUrl(model.ResponseUrl, errors);
        return errors.Count == 0 ? OcpiValidationResult.Valid() : OcpiValidationResult.Failed(errors);
    }

    /// <inheritdoc />
    public OcpiValidationResult Validate(ReserveNow model)
    {
        var errors = new List<OcpiValidationError>();
        ValidateResponseUrl(model.ResponseUrl, errors);

        if (model.ExpiryDate <= DateTimeOffset.UtcNow)
        {
            errors.Add(
                new OcpiValidationError(
                    "COMMAND_EXPIRY_IN_PAST",
                    "ExpiryDate must be in the future.",
                    "Set ExpiryDate to a future UTC timestamp."
                )
                {
                    PropertyPath = "ExpiryDate",
                }
            );
        }

        return errors.Count == 0 ? OcpiValidationResult.Valid() : OcpiValidationResult.Failed(errors);
    }

    /// <inheritdoc />
    public OcpiValidationResult Validate(CancelReservation model)
    {
        var errors = new List<OcpiValidationError>();
        ValidateResponseUrl(model.ResponseUrl, errors);
        return errors.Count == 0 ? OcpiValidationResult.Valid() : OcpiValidationResult.Failed(errors);
    }

    /// <inheritdoc />
    public OcpiValidationResult Validate(UnlockConnector model)
    {
        var errors = new List<OcpiValidationError>();
        ValidateResponseUrl(model.ResponseUrl, errors);
        return errors.Count == 0 ? OcpiValidationResult.Valid() : OcpiValidationResult.Failed(errors);
    }

    internal static void ValidateResponseUrl(string url, List<OcpiValidationError> errors)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            errors.Add(
                new OcpiValidationError(
                    "COMMAND_INVALID_RESPONSE_URL",
                    $"ResponseUrl '{url}' is not a valid HTTPS URL.",
                    "Provide an absolute HTTPS URL for the async response callback."
                )
                {
                    PropertyPath = "ResponseUrl",
                }
            );
        }
    }
}
