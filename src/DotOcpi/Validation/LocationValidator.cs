namespace DotOcpi.Validation;

/// <summary>
/// Validates Location models against OCPI protocol rules across all supported versions.
/// V2_0/V2_1_1: coordinates, country (alpha-3), EVSE uniqueness.
/// V2_2/V2_2_1: all of the above plus country code (alpha-2) and publish consistency.
/// </summary>
public sealed class LocationValidator
    : IOcpiValidator<Models.V2_0.Location>,
        IOcpiValidator<Models.V2_1_1.Location>,
        IOcpiValidator<Models.V2_2.Location>,
        IOcpiValidator<Models.V2_2_1.Location>
{
    /// <inheritdoc />
    public OcpiValidationResult Validate(Models.V2_0.Location model)
    {
        var errors = new List<OcpiValidationError>();

        ValidationHelpers.ValidateCoordinates(model.Coordinates, "Coordinates", errors);
        ValidationHelpers.ValidateCountryAlpha3(model.Country, errors);
        ValidateEvses(model.Evses, errors);

        return errors.Count == 0 ? OcpiValidationResult.Valid() : OcpiValidationResult.Failed(errors);
    }

    /// <inheritdoc />
    public OcpiValidationResult Validate(Models.V2_1_1.Location model)
    {
        var errors = new List<OcpiValidationError>();

        ValidationHelpers.ValidateCoordinates(model.Coordinates, "Coordinates", errors);
        ValidationHelpers.ValidateCountryAlpha3(model.Country, errors);
        ValidateEvses(model.Evses, errors);

        return errors.Count == 0 ? OcpiValidationResult.Valid() : OcpiValidationResult.Failed(errors);
    }

    /// <inheritdoc />
    public OcpiValidationResult Validate(Models.V2_2.Location model)
    {
        var errors = new List<OcpiValidationError>();

        ValidationHelpers.ValidateCoordinates(model.Coordinates, "Coordinates", errors);
        ValidationHelpers.ValidateCountryAlpha2(
            (string)model.CountryCode,
            "LOCATION_INVALID_COUNTRY_CODE",
            "CountryCode",
            errors
        );
        ValidationHelpers.ValidateCountryAlpha3(model.Country, errors);
        ValidatePublishConsistency(model.Publish, model.PublishAllowedTo, errors);
        ValidateEvses(model.Evses, errors);

        return errors.Count == 0 ? OcpiValidationResult.Valid() : OcpiValidationResult.Failed(errors);
    }

    /// <inheritdoc />
    public OcpiValidationResult Validate(Models.V2_2_1.Location model)
    {
        var errors = new List<OcpiValidationError>();

        ValidationHelpers.ValidateCoordinates(model.Coordinates, "Coordinates", errors);
        ValidationHelpers.ValidateCountryAlpha2(
            (string)model.CountryCode,
            "LOCATION_INVALID_COUNTRY_CODE",
            "CountryCode",
            errors
        );
        ValidationHelpers.ValidateCountryAlpha3(model.Country, errors);
        ValidatePublishConsistency(model.Publish, model.PublishAllowedTo, errors);
        ValidateEvses(model.Evses, errors);

        return errors.Count == 0 ? OcpiValidationResult.Valid() : OcpiValidationResult.Failed(errors);
    }

    OcpiValidationResult IOcpiValidator.Validate(object model) =>
        model switch
        {
            Models.V2_0.Location m => Validate(m),
            Models.V2_1_1.Location m => Validate(m),
            Models.V2_2.Location m => Validate(m),
            Models.V2_2_1.Location m => Validate(m),
            _ => throw new ArgumentException($"Unsupported Location type: {model.GetType().Name}", nameof(model)),
        };

    private static void ValidatePublishConsistency(
        bool publish,
        IReadOnlyList<Models.V2_2_1.PublishTokenType>? publishAllowedTo,
        List<OcpiValidationError> errors
    )
    {
        if (!publish && (publishAllowedTo is null || publishAllowedTo.Count == 0))
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

    private static void ValidatePublishConsistency(
        bool publish,
        IReadOnlyList<Models.V2_2.PublishTokenType>? publishAllowedTo,
        List<OcpiValidationError> errors
    )
    {
        if (!publish && (publishAllowedTo is null || publishAllowedTo.Count == 0))
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

    // V2_0/V2_1_1: EVSE.Uid is string, Connector.Id is string
    private static void ValidateEvses(IReadOnlyList<Models.V2_0.Evse>? evses, List<OcpiValidationError> errors)
    {
        if (evses is null || evses.Count == 0)
            return;

        var evseUids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < evses.Count; i++)
        {
            var evse = evses[i];
            ValidateEvseCore(evse.Uid, evse.Connectors.Count, i, evseUids, errors);

            var connectorIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var j = 0; j < evse.Connectors.Count; j++)
            {
                if (!connectorIds.Add(evse.Connectors[j].Id))
                {
                    AddDuplicateConnectorError(evse.Connectors[j].Id, evse.Uid, i, j, errors);
                }
            }

            if (evse.Coordinates is { } evseCoords)
                ValidationHelpers.ValidateCoordinates(evseCoords, $"Evses[{i}].Coordinates", errors);
        }
    }

    // V2_1_1 overload (same shape as V2_0 for validation-relevant fields)
    private static void ValidateEvses(IReadOnlyList<Models.V2_1_1.Evse>? evses, List<OcpiValidationError> errors)
    {
        if (evses is null || evses.Count == 0)
            return;

        var evseUids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < evses.Count; i++)
        {
            var evse = evses[i];
            ValidateEvseCore(evse.Uid, evse.Connectors.Count, i, evseUids, errors);

            var connectorIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var j = 0; j < evse.Connectors.Count; j++)
            {
                if (!connectorIds.Add(evse.Connectors[j].Id))
                {
                    AddDuplicateConnectorError(evse.Connectors[j].Id, evse.Uid, i, j, errors);
                }
            }

            if (evse.Coordinates is { } evseCoords)
                ValidationHelpers.ValidateCoordinates(evseCoords, $"Evses[{i}].Coordinates", errors);
        }
    }

    // V2_2/V2_2_1: EVSE.Uid is CiString, Connector.Id is CiString
    private static void ValidateEvses(IReadOnlyList<Models.V2_2.Evse>? evses, List<OcpiValidationError> errors)
    {
        if (evses is null || evses.Count == 0)
            return;

        var evseUids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < evses.Count; i++)
        {
            var evse = evses[i];
            ValidateEvseCore((string)evse.Uid, evse.Connectors.Count, i, evseUids, errors);

            var connectorIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var j = 0; j < evse.Connectors.Count; j++)
            {
                if (!connectorIds.Add((string)evse.Connectors[j].Id))
                {
                    AddDuplicateConnectorError((string)evse.Connectors[j].Id, (string)evse.Uid, i, j, errors);
                }
            }

            if (evse.Coordinates is { } evseCoords)
                ValidationHelpers.ValidateCoordinates(evseCoords, $"Evses[{i}].Coordinates", errors);
        }
    }

    private static void ValidateEvses(IReadOnlyList<Models.V2_2_1.Evse>? evses, List<OcpiValidationError> errors)
    {
        if (evses is null || evses.Count == 0)
            return;

        var evseUids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < evses.Count; i++)
        {
            var evse = evses[i];
            ValidateEvseCore((string)evse.Uid, evse.Connectors.Count, i, evseUids, errors);

            var connectorIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var j = 0; j < evse.Connectors.Count; j++)
            {
                if (!connectorIds.Add((string)evse.Connectors[j].Id))
                {
                    AddDuplicateConnectorError((string)evse.Connectors[j].Id, (string)evse.Uid, i, j, errors);
                }
            }

            if (evse.Coordinates is { } evseCoords)
                ValidationHelpers.ValidateCoordinates(evseCoords, $"Evses[{i}].Coordinates", errors);
        }
    }

    private static void ValidateEvseCore(
        string uid,
        int connectorCount,
        int index,
        HashSet<string> evseUids,
        List<OcpiValidationError> errors
    )
    {
        if (!evseUids.Add(uid))
        {
            errors.Add(
                new OcpiValidationError(
                    "LOCATION_DUPLICATE_EVSE_UID",
                    $"Duplicate EVSE UID '{uid}' found.",
                    "Each EVSE within a Location must have a unique UID."
                )
                {
                    PropertyPath = $"Evses[{index}].Uid",
                }
            );
        }

        if (connectorCount == 0)
        {
            errors.Add(
                new OcpiValidationError(
                    "EVSE_NO_CONNECTORS",
                    $"EVSE '{uid}' has no connectors.",
                    "Each EVSE must have at least one connector."
                )
                {
                    PropertyPath = $"Evses[{index}].Connectors",
                }
            );
        }
    }

    private static void AddDuplicateConnectorError(
        string connectorId,
        string evseUid,
        int evseIndex,
        int connectorIndex,
        List<OcpiValidationError> errors
    )
    {
        errors.Add(
            new OcpiValidationError(
                "EVSE_DUPLICATE_CONNECTOR_ID",
                $"Duplicate Connector ID '{connectorId}' in EVSE '{evseUid}'.",
                "Each Connector within an EVSE must have a unique ID."
            )
            {
                PropertyPath = $"Evses[{evseIndex}].Connectors[{connectorIndex}].Id",
            }
        );
    }
}
