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
    private readonly TimeProvider _timeProvider;

    public CommandValidator(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

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

        if (model.ExpiryDate <= _timeProvider.GetUtcNow())
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

    OcpiValidationResult IOcpiValidator.Validate(object model) =>
        model switch
        {
            StartSession m => Validate(m),
            StopSession m => Validate(m),
            ReserveNow m => Validate(m),
            CancelReservation m => Validate(m),
            UnlockConnector m => Validate(m),
            _ => throw new ArgumentException($"Unsupported command type: {model.GetType().Name}", nameof(model)),
        };

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
            return;
        }

        // Pre-DNS SSRF check: reject literal private/loopback/link-local IPs.
        // The post-DNS SsrfGuard in SocketsHttpHandler catches DNS-rebinding;
        // this catches obvious attempts at validation time.
        if (ValidationHelpers.IsLiteralPrivateHost(uri))
        {
            errors.Add(
                new OcpiValidationError(
                    "COMMAND_RESPONSE_URL_SSRF",
                    $"ResponseUrl '{url}' resolves to a private/loopback address.",
                    "Provide a publicly routable HTTPS URL."
                )
                {
                    PropertyPath = "ResponseUrl",
                }
            );
        }
    }
}
