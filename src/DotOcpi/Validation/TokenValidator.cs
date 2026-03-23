namespace DotOcpi.Validation;

/// <summary>
/// Validates Token models against OCPI protocol rules across all supported versions.
/// V2_0: language only (no whitelist, no country code on token).
/// V2_1_1: language and whitelist consistency (no country code on token).
/// V2_2/V2_2_1: language, whitelist consistency, and country code validation.
/// </summary>
public sealed class TokenValidator
    : IOcpiValidator<Models.V2_0.Token>,
        IOcpiValidator<Models.V2_1_1.Token>,
        IOcpiValidator<Models.V2_2.Token>,
        IOcpiValidator<Models.V2_2_1.Token>
{
    /// <inheritdoc />
    public OcpiValidationResult Validate(Models.V2_0.Token model)
    {
        var errors = new List<OcpiValidationError>();

        ValidationHelpers.ValidateLanguage(model.Language, "TOKEN_INVALID_LANGUAGE", errors);

        return errors.Count == 0 ? OcpiValidationResult.Valid() : OcpiValidationResult.Failed(errors);
    }

    /// <inheritdoc />
    public OcpiValidationResult Validate(Models.V2_1_1.Token model)
    {
        var errors = new List<OcpiValidationError>();

        ValidationHelpers.ValidateLanguage(model.Language, "TOKEN_INVALID_LANGUAGE", errors);
        ValidateWhitelistConsistency(model.Valid, model.Whitelist.ToString(), errors);

        return errors.Count == 0 ? OcpiValidationResult.Valid() : OcpiValidationResult.Failed(errors);
    }

    /// <inheritdoc />
    public OcpiValidationResult Validate(Models.V2_2.Token model)
    {
        var errors = new List<OcpiValidationError>();

        ValidationHelpers.ValidateCountryAlpha2(
            (string)model.CountryCode,
            "TOKEN_INVALID_COUNTRY_CODE",
            "CountryCode",
            errors
        );
        ValidationHelpers.ValidateLanguage(model.Language, "TOKEN_INVALID_LANGUAGE", errors);
        ValidateWhitelistConsistency(model.Valid, model.Whitelist.ToString(), errors);

        return errors.Count == 0 ? OcpiValidationResult.Valid() : OcpiValidationResult.Failed(errors);
    }

    /// <inheritdoc />
    public OcpiValidationResult Validate(Models.V2_2_1.Token model)
    {
        var errors = new List<OcpiValidationError>();

        ValidationHelpers.ValidateCountryAlpha2(
            (string)model.CountryCode,
            "TOKEN_INVALID_COUNTRY_CODE",
            "CountryCode",
            errors
        );
        ValidationHelpers.ValidateLanguage(model.Language, "TOKEN_INVALID_LANGUAGE", errors);
        ValidateWhitelistConsistency(model.Valid, model.Whitelist.ToString(), errors);

        return errors.Count == 0 ? OcpiValidationResult.Valid() : OcpiValidationResult.Failed(errors);
    }

    OcpiValidationResult IOcpiValidator.Validate(object model) =>
        model switch
        {
            Models.V2_0.Token m => Validate(m),
            Models.V2_1_1.Token m => Validate(m),
            Models.V2_2.Token m => Validate(m),
            Models.V2_2_1.Token m => Validate(m),
            _ => throw new ArgumentException($"Unsupported Token type: {model.GetType().Name}", nameof(model)),
        };

    private static void ValidateWhitelistConsistency(
        bool valid,
        string whitelistValue,
        List<OcpiValidationError> errors
    )
    {
        if (!valid && whitelistValue == "ALWAYS")
        {
            errors.Add(
                new OcpiValidationError(
                    "TOKEN_INVALID_ALWAYS_WHITELIST",
                    "Token is invalid but has ALWAYS whitelist. Invalid tokens should not be always whitelisted.",
                    "Either set Valid to true or change Whitelist from ALWAYS."
                )
                {
                    PropertyPath = "Whitelist",
                }
            );
        }
    }
}
