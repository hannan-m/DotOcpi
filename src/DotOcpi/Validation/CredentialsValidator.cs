using DotOcpi.Models.V2_2_1;

namespace DotOcpi.Validation;

/// <summary>
/// Validates Credentials models against OCPI 2.2.1 protocol rules.
/// </summary>
public sealed class CredentialsValidator : IOcpiValidator<Credentials>
{
    /// <inheritdoc />
    public OcpiValidationResult Validate(Credentials model)
    {
        var errors = new List<OcpiValidationError>();

        ValidateUrl(model, errors);
        ValidateToken(model, errors);
        ValidateRoles(model, errors);

        return errors.Count == 0 ? OcpiValidationResult.Valid() : OcpiValidationResult.Failed(errors);
    }

    private static void ValidateUrl(Credentials model, List<OcpiValidationError> errors)
    {
        if (!Uri.TryCreate(model.Url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            errors.Add(
                new OcpiValidationError(
                    "CREDENTIALS_INVALID_URL",
                    $"Versions URL '{model.Url}' is not a valid HTTPS URL.",
                    "Provide an absolute HTTPS URL pointing to the versions endpoint."
                )
                {
                    PropertyPath = "Url",
                }
            );
        }
    }

    private static void ValidateToken(Credentials model, List<OcpiValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(model.Token))
        {
            errors.Add(
                new OcpiValidationError(
                    "CREDENTIALS_EMPTY_TOKEN",
                    "Credentials token is empty or whitespace.",
                    "Provide a non-empty token string."
                )
                {
                    PropertyPath = "Token",
                }
            );
        }
    }

    private static void ValidateRoles(Credentials model, List<OcpiValidationError> errors)
    {
        if (model.Roles.Count == 0)
        {
            errors.Add(
                new OcpiValidationError(
                    "CREDENTIALS_NO_ROLES",
                    "Credentials must have at least one role.",
                    "Provide at least one CredentialsRole."
                )
                {
                    PropertyPath = "Roles",
                }
            );
            return;
        }

        for (var i = 0; i < model.Roles.Count; i++)
        {
            var role = model.Roles[i];
            var cc = (string)role.CountryCode;
            if (cc.Length != 2 || !cc.All(char.IsLetter))
            {
                errors.Add(
                    new OcpiValidationError(
                        "CREDENTIALS_ROLE_INVALID_COUNTRY_CODE",
                        $"Role[{i}] CountryCode '{cc}' is not a valid ISO 3166-1 alpha-2 code.",
                        "Provide a 2-letter country code."
                    )
                    {
                        PropertyPath = $"Roles[{i}].CountryCode",
                    }
                );
            }
        }
    }
}
