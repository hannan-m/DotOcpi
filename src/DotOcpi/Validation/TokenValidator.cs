using DotOcpi.Models.V2_2_1;

namespace DotOcpi.Validation;

/// <summary>
/// Validates Token models against OCPI 2.2.1 protocol rules.
/// This is model validation, not authentication token validation.
/// </summary>
public sealed class TokenValidator : IOcpiValidator<Token>
{
    /// <inheritdoc />
    public OcpiValidationResult Validate(Token model)
    {
        var errors = new List<OcpiValidationError>();

        ValidateCountryCode(model, errors);
        ValidateLanguage(model, errors);
        ValidateWhitelistConsistency(model, errors);

        return errors.Count == 0 ? OcpiValidationResult.Valid() : OcpiValidationResult.Failed(errors);
    }

    private static void ValidateCountryCode(Token model, List<OcpiValidationError> errors)
    {
        var value = (string)model.CountryCode;
        if (value.Length != 2 || !value.All(char.IsLetter))
        {
            errors.Add(
                new OcpiValidationError(
                    "TOKEN_INVALID_COUNTRY_CODE",
                    $"CountryCode '{value}' is not a valid ISO 3166-1 alpha-2 code.",
                    "Provide a 2-letter country code (e.g. 'NL', 'DE')."
                )
                {
                    PropertyPath = "CountryCode",
                }
            );
        }
    }

    private static void ValidateLanguage(Token model, List<OcpiValidationError> errors)
    {
        if (model.Language is { } language && (language.Length != 2 || !language.All(char.IsLetter)))
        {
            errors.Add(
                new OcpiValidationError(
                    "TOKEN_INVALID_LANGUAGE",
                    $"Language '{language}' is not a valid ISO 639-1 code.",
                    "Provide a 2-letter language code (e.g. 'en', 'nl')."
                )
                {
                    PropertyPath = "Language",
                }
            );
        }
    }

    private static void ValidateWhitelistConsistency(Token model, List<OcpiValidationError> errors)
    {
        if (!model.Valid && model.Whitelist == WhitelistType.ALWAYS)
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
