namespace DotOcpi.Validation;

/// <summary>
/// Validates Tariff models against OCPI protocol rules across all supported versions.
/// V2_0/V2_1_1: currency, elements with price components.
/// V2_2/V2_2_1: all of the above plus time range and min/max price consistency.
/// </summary>
public sealed class TariffValidator
    : IOcpiValidator<Models.V2_0.Tariff>,
        IOcpiValidator<Models.V2_1_1.Tariff>,
        IOcpiValidator<Models.V2_2.Tariff>,
        IOcpiValidator<Models.V2_2_1.Tariff>
{
    /// <inheritdoc />
    public OcpiValidationResult Validate(Models.V2_0.Tariff model)
    {
        var errors = new List<OcpiValidationError>();

        ValidationHelpers.ValidateCurrency(model.Currency, "TARIFF_INVALID_CURRENCY", errors);
        ValidateElements(model.Elements, errors);

        return errors.Count == 0 ? OcpiValidationResult.Valid() : OcpiValidationResult.Failed(errors);
    }

    /// <inheritdoc />
    public OcpiValidationResult Validate(Models.V2_1_1.Tariff model)
    {
        var errors = new List<OcpiValidationError>();

        ValidationHelpers.ValidateCurrency(model.Currency, "TARIFF_INVALID_CURRENCY", errors);
        ValidateElements(model.Elements, errors);

        return errors.Count == 0 ? OcpiValidationResult.Valid() : OcpiValidationResult.Failed(errors);
    }

    /// <inheritdoc />
    public OcpiValidationResult Validate(Models.V2_2.Tariff model)
    {
        var errors = new List<OcpiValidationError>();

        ValidationHelpers.ValidateCurrency(model.Currency, "TARIFF_INVALID_CURRENCY", errors);
        ValidateElements(model.Elements, errors);
        ValidateTimeRange(model.StartDateTime, model.EndDateTime, errors);
        ValidateMinMaxPrice(model.MinPrice, model.MaxPrice, errors);

        return errors.Count == 0 ? OcpiValidationResult.Valid() : OcpiValidationResult.Failed(errors);
    }

    /// <inheritdoc />
    public OcpiValidationResult Validate(Models.V2_2_1.Tariff model)
    {
        var errors = new List<OcpiValidationError>();

        ValidationHelpers.ValidateCurrency(model.Currency, "TARIFF_INVALID_CURRENCY", errors);
        ValidateElements(model.Elements, errors);
        ValidateTimeRange(model.StartDateTime, model.EndDateTime, errors);
        ValidateMinMaxPrice(model.MinPrice, model.MaxPrice, errors);

        return errors.Count == 0 ? OcpiValidationResult.Valid() : OcpiValidationResult.Failed(errors);
    }

    OcpiValidationResult IOcpiValidator.Validate(object model) =>
        model switch
        {
            Models.V2_0.Tariff m => Validate(m),
            Models.V2_1_1.Tariff m => Validate(m),
            Models.V2_2.Tariff m => Validate(m),
            Models.V2_2_1.Tariff m => Validate(m),
            _ => throw new ArgumentException($"Unsupported Tariff type: {model.GetType().Name}", nameof(model)),
        };

    private static void ValidateElements(
        IReadOnlyList<Models.V2_0.TariffElement> elements,
        List<OcpiValidationError> errors
    )
    {
        if (elements.Count == 0)
        {
            AddNoElementsError(errors);
            return;
        }

        for (var i = 0; i < elements.Count; i++)
        {
            var element = elements[i];
            ValidatePriceComponents(element.PriceComponents.Count, i, errors);

            for (var j = 0; j < element.PriceComponents.Count; j++)
                ValidateStepSize(element.PriceComponents[j].StepSize, i, j, errors);
        }
    }

    private static void ValidateElements(
        IReadOnlyList<Models.V2_1_1.TariffElement> elements,
        List<OcpiValidationError> errors
    )
    {
        if (elements.Count == 0)
        {
            AddNoElementsError(errors);
            return;
        }

        for (var i = 0; i < elements.Count; i++)
        {
            var element = elements[i];
            ValidatePriceComponents(element.PriceComponents.Count, i, errors);

            for (var j = 0; j < element.PriceComponents.Count; j++)
                ValidateStepSize(element.PriceComponents[j].StepSize, i, j, errors);
        }
    }

    private static void ValidateElements(
        IReadOnlyList<Models.V2_2.TariffElement> elements,
        List<OcpiValidationError> errors
    )
    {
        if (elements.Count == 0)
        {
            AddNoElementsError(errors);
            return;
        }

        for (var i = 0; i < elements.Count; i++)
        {
            var element = elements[i];
            ValidatePriceComponents(element.PriceComponents.Count, i, errors);

            for (var j = 0; j < element.PriceComponents.Count; j++)
                ValidateStepSize(element.PriceComponents[j].StepSize, i, j, errors);
        }
    }

    private static void ValidateElements(
        IReadOnlyList<Models.V2_2_1.TariffElement> elements,
        List<OcpiValidationError> errors
    )
    {
        if (elements.Count == 0)
        {
            AddNoElementsError(errors);
            return;
        }

        for (var i = 0; i < elements.Count; i++)
        {
            var element = elements[i];
            ValidatePriceComponents(element.PriceComponents.Count, i, errors);

            for (var j = 0; j < element.PriceComponents.Count; j++)
                ValidateStepSize(element.PriceComponents[j].StepSize, i, j, errors);
        }
    }

    private static void AddNoElementsError(List<OcpiValidationError> errors)
    {
        errors.Add(
            new OcpiValidationError(
                "TARIFF_NO_ELEMENTS",
                "Tariff has no elements.",
                "A Tariff must have at least one TariffElement."
            )
            {
                PropertyPath = "Elements",
            }
        );
    }

    private static void ValidatePriceComponents(int count, int elementIndex, List<OcpiValidationError> errors)
    {
        if (count == 0)
        {
            errors.Add(
                new OcpiValidationError(
                    "TARIFF_ELEMENT_NO_PRICE_COMPONENTS",
                    $"TariffElement at index {elementIndex} has no price components.",
                    "Each TariffElement must have at least one PriceComponent."
                )
                {
                    PropertyPath = $"Elements[{elementIndex}].PriceComponents",
                }
            );
        }
    }

    private static void ValidateStepSize(
        int stepSize,
        int elementIndex,
        int componentIndex,
        List<OcpiValidationError> errors
    )
    {
        if (stepSize <= 0)
        {
            errors.Add(
                new OcpiValidationError(
                    "TARIFF_INVALID_STEP_SIZE",
                    $"PriceComponent step_size {stepSize} must be greater than 0.",
                    "Set step_size to a positive integer."
                )
                {
                    PropertyPath = $"Elements[{elementIndex}].PriceComponents[{componentIndex}].StepSize",
                }
            );
        }
    }

    private static void ValidateTimeRange(DateTimeOffset? start, DateTimeOffset? end, List<OcpiValidationError> errors)
    {
        if (start is { } s && end is { } e && e < s)
        {
            errors.Add(
                new OcpiValidationError(
                    "TARIFF_END_BEFORE_START",
                    "EndDateTime is before StartDateTime.",
                    "EndDateTime must be equal to or after StartDateTime."
                )
                {
                    PropertyPath = "EndDateTime",
                }
            );
        }
    }

    private static void ValidateMinMaxPrice(
        Models.V2_2.Price? minPrice,
        Models.V2_2.Price? maxPrice,
        List<OcpiValidationError> errors
    )
    {
        if (minPrice is { } min && maxPrice is { } max && min.ExclVat > max.ExclVat)
            AddMinExceedsMaxError(errors);
    }

    private static void ValidateMinMaxPrice(
        Models.V2_2_1.Price? minPrice,
        Models.V2_2_1.Price? maxPrice,
        List<OcpiValidationError> errors
    )
    {
        if (minPrice is { } min && maxPrice is { } max && min.ExclVat > max.ExclVat)
            AddMinExceedsMaxError(errors);
    }

    private static void AddMinExceedsMaxError(List<OcpiValidationError> errors)
    {
        errors.Add(
            new OcpiValidationError(
                "TARIFF_MIN_EXCEEDS_MAX",
                "MinPrice.ExclVat exceeds MaxPrice.ExclVat.",
                "MinPrice must be less than or equal to MaxPrice."
            )
            {
                PropertyPath = "MinPrice",
            }
        );
    }
}
