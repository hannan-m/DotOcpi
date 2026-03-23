namespace DotOcpi.Validation;

/// <summary>
/// Validates Credentials models against OCPI protocol rules across all supported versions.
/// V2_0/V2_1_1: HTTPS URL, non-empty token, flat country code / party ID.
/// V2_2/V2_2_1: HTTPS URL, non-empty token, roles array with country code validation.
/// </summary>
public sealed class CredentialsValidator
    : IOcpiValidator<Models.V2_0.Credentials>,
        IOcpiValidator<Models.V2_1_1.Credentials>,
        IOcpiValidator<Models.V2_2.Credentials>,
        IOcpiValidator<Models.V2_2_1.Credentials>
{
    /// <inheritdoc />
    public OcpiValidationResult Validate(Models.V2_0.Credentials model)
    {
        var errors = new List<OcpiValidationError>();

        ValidateUrlAndToken(model.Url, model.Token, errors);
        ValidationHelpers.ValidateCountryAlpha2(
            model.CountryCode,
            "CREDENTIALS_INVALID_COUNTRY_CODE",
            "CountryCode",
            errors
        );

        return errors.Count == 0 ? OcpiValidationResult.Valid() : OcpiValidationResult.Failed(errors);
    }

    /// <inheritdoc />
    public OcpiValidationResult Validate(Models.V2_1_1.Credentials model)
    {
        var errors = new List<OcpiValidationError>();

        ValidateUrlAndToken(model.Url, model.Token, errors);
        ValidationHelpers.ValidateCountryAlpha2(
            model.CountryCode,
            "CREDENTIALS_INVALID_COUNTRY_CODE",
            "CountryCode",
            errors
        );

        return errors.Count == 0 ? OcpiValidationResult.Valid() : OcpiValidationResult.Failed(errors);
    }

    /// <inheritdoc />
    public OcpiValidationResult Validate(Models.V2_2.Credentials model)
    {
        var errors = new List<OcpiValidationError>();

        ValidateUrlAndToken(model.Url, model.Token, errors);
        ValidateRoles(model.Roles, errors);

        return errors.Count == 0 ? OcpiValidationResult.Valid() : OcpiValidationResult.Failed(errors);
    }

    /// <inheritdoc />
    public OcpiValidationResult Validate(Models.V2_2_1.Credentials model)
    {
        var errors = new List<OcpiValidationError>();

        ValidateUrlAndToken(model.Url, model.Token, errors);
        ValidateRoles(model.Roles, errors);

        return errors.Count == 0 ? OcpiValidationResult.Valid() : OcpiValidationResult.Failed(errors);
    }

    OcpiValidationResult IOcpiValidator.Validate(object model) =>
        model switch
        {
            Models.V2_0.Credentials m => Validate(m),
            Models.V2_1_1.Credentials m => Validate(m),
            Models.V2_2.Credentials m => Validate(m),
            Models.V2_2_1.Credentials m => Validate(m),
            _ => throw new ArgumentException($"Unsupported Credentials type: {model.GetType().Name}", nameof(model)),
        };

    private static void ValidateUrlAndToken(string url, string token, List<OcpiValidationError> errors)
    {
        ValidationHelpers.ValidateHttpsUrl(
            url,
            "CREDENTIALS_INVALID_URL",
            $"Versions URL '{url}' is not a valid HTTPS URL.",
            "Provide an absolute HTTPS URL pointing to the versions endpoint.",
            "Url",
            errors
        );

        if (string.IsNullOrWhiteSpace(token))
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

    private static void ValidateRoles(
        IReadOnlyList<Models.V2_2.CredentialsRole> roles,
        List<OcpiValidationError> errors
    )
    {
        if (roles.Count == 0)
        {
            AddNoRolesError(errors);
            return;
        }

        for (var i = 0; i < roles.Count; i++)
        {
            ValidationHelpers.ValidateCountryAlpha2(
                (string)roles[i].CountryCode,
                "CREDENTIALS_ROLE_INVALID_COUNTRY_CODE",
                $"Roles[{i}].CountryCode",
                errors
            );
        }
    }

    private static void ValidateRoles(
        IReadOnlyList<Models.V2_2_1.CredentialsRole> roles,
        List<OcpiValidationError> errors
    )
    {
        if (roles.Count == 0)
        {
            AddNoRolesError(errors);
            return;
        }

        for (var i = 0; i < roles.Count; i++)
        {
            ValidationHelpers.ValidateCountryAlpha2(
                (string)roles[i].CountryCode,
                "CREDENTIALS_ROLE_INVALID_COUNTRY_CODE",
                $"Roles[{i}].CountryCode",
                errors
            );
        }
    }

    private static void AddNoRolesError(List<OcpiValidationError> errors)
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
    }
}
