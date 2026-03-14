using DotOcpi.Models.V2_2_1;

namespace DotOcpi.Validation;

/// <summary>
/// Validates Location models against OCPI 2.2.1 protocol rules.
/// </summary>
public sealed class LocationValidator : IOcpiValidator<Location>
{
    /// <inheritdoc />
    public OcpiValidationResult Validate(Location model)
    {
        var errors = new List<OcpiValidationError>();

        ValidateCoordinates(model.Coordinates, "Coordinates", errors);
        ValidateCountryCode(model.CountryCode, errors);
        ValidateCountry(model.Country, errors);
        ValidatePublishConsistency(model, errors);
        ValidateEvses(model.Evses, errors);

        return errors.Count == 0 ? OcpiValidationResult.Valid() : OcpiValidationResult.Failed(errors);
    }

    private static void ValidateCoordinates(
        GeoLocation coordinates,
        string propertyPath,
        List<OcpiValidationError> errors
    )
    {
        if (
            !decimal.TryParse(
                coordinates.Latitude,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var lat
            )
            || lat < -90m
            || lat > 90m
        )
        {
            errors.Add(
                new OcpiValidationError(
                    "LOCATION_INVALID_LATITUDE",
                    $"Latitude '{coordinates.Latitude}' is not a valid decimal between -90 and 90.",
                    "Provide a decimal latitude in the range [-90, 90]."
                )
                {
                    PropertyPath = $"{propertyPath}.Latitude",
                }
            );
        }

        if (
            !decimal.TryParse(
                coordinates.Longitude,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var lon
            )
            || lon < -180m
            || lon > 180m
        )
        {
            errors.Add(
                new OcpiValidationError(
                    "LOCATION_INVALID_LONGITUDE",
                    $"Longitude '{coordinates.Longitude}' is not a valid decimal between -180 and 180.",
                    "Provide a decimal longitude in the range [-180, 180]."
                )
                {
                    PropertyPath = $"{propertyPath}.Longitude",
                }
            );
        }
    }

    private static void ValidateCountryCode(CiString countryCode, List<OcpiValidationError> errors)
    {
        var value = (string)countryCode;
        if (value.Length != 2 || !value.All(char.IsLetter))
        {
            errors.Add(
                new OcpiValidationError(
                    "LOCATION_INVALID_COUNTRY_CODE",
                    $"CountryCode '{value}' is not a valid ISO 3166-1 alpha-2 code.",
                    "Provide a 2-letter country code (e.g. 'NL', 'DE')."
                )
                {
                    PropertyPath = "CountryCode",
                }
            );
        }
    }

    private static void ValidateCountry(string country, List<OcpiValidationError> errors)
    {
        if (country.Length != 3 || !country.All(char.IsLetter))
        {
            errors.Add(
                new OcpiValidationError(
                    "LOCATION_INVALID_COUNTRY",
                    $"Country '{country}' is not a valid ISO 3166-1 alpha-3 code.",
                    "Provide a 3-letter country code (e.g. 'NLD', 'DEU')."
                )
                {
                    PropertyPath = "Country",
                }
            );
        }
    }

    private static void ValidatePublishConsistency(Location model, List<OcpiValidationError> errors)
    {
        if (!model.Publish && (model.PublishAllowedTo is null || model.PublishAllowedTo.Count == 0))
        {
            errors.Add(
                new OcpiValidationError(
                    "LOCATION_PUBLISH_MISSING_ALLOWED_TO",
                    "Location has Publish=false but PublishAllowedTo is empty.",
                    "Either set Publish to true or provide PublishAllowedTo entries."
                )
                {
                    PropertyPath = "PublishAllowedTo",
                }
            );
        }
    }

    private static void ValidateEvses(IReadOnlyList<Evse>? evses, List<OcpiValidationError> errors)
    {
        if (evses is null || evses.Count == 0)
        {
            return;
        }

        var evseUids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < evses.Count; i++)
        {
            var evse = evses[i];
            var uid = (string)evse.Uid;

            if (!evseUids.Add(uid))
            {
                errors.Add(
                    new OcpiValidationError(
                        "LOCATION_DUPLICATE_EVSE_UID",
                        $"Duplicate EVSE UID '{uid}' found.",
                        "Each EVSE within a Location must have a unique UID."
                    )
                    {
                        PropertyPath = $"Evses[{i}].Uid",
                    }
                );
            }

            if (evse.Connectors.Count == 0)
            {
                errors.Add(
                    new OcpiValidationError(
                        "EVSE_NO_CONNECTORS",
                        $"EVSE '{uid}' has no connectors.",
                        "Each EVSE must have at least one connector."
                    )
                    {
                        PropertyPath = $"Evses[{i}].Connectors",
                    }
                );
            }

            ValidateConnectorUniqueness(evse, i, errors);

            if (evse.Coordinates is { } evseCoords)
            {
                ValidateCoordinates(evseCoords, $"Evses[{i}].Coordinates", errors);
            }
        }
    }

    private static void ValidateConnectorUniqueness(Evse evse, int evseIndex, List<OcpiValidationError> errors)
    {
        var connectorIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var j = 0; j < evse.Connectors.Count; j++)
        {
            var connectorId = (string)evse.Connectors[j].Id;
            if (!connectorIds.Add(connectorId))
            {
                errors.Add(
                    new OcpiValidationError(
                        "EVSE_DUPLICATE_CONNECTOR_ID",
                        $"Duplicate Connector ID '{connectorId}' in EVSE '{(string)evse.Uid}'.",
                        "Each Connector within an EVSE must have a unique ID."
                    )
                    {
                        PropertyPath = $"Evses[{evseIndex}].Connectors[{j}].Id",
                    }
                );
            }
        }
    }
}
