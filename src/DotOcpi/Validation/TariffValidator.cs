using DotOcpi.Models.V2_2_1;

namespace DotOcpi.Validation;

/// <summary>
/// Validates Tariff models against OCPI 2.2.1 protocol rules.
/// </summary>
public sealed class TariffValidator : IOcpiValidator<Tariff>
{
    /// <inheritdoc />
    public OcpiValidationResult Validate(Tariff model)
    {
        var errors = new List<OcpiValidationError>();

        ValidateCurrency(model, errors);
        ValidateElements(model, errors);
        ValidateTimeRange(model, errors);
        ValidateMinMaxPrice(model, errors);

        return errors.Count == 0 ? OcpiValidationResult.Valid() : OcpiValidationResult.Failed(errors);
    }

    private static void ValidateCurrency(Tariff model, List<OcpiValidationError> errors)
    {
        if (model.Currency.Length != 3 || !model.Currency.All(char.IsLetter))
        {
            errors.Add(
                new OcpiValidationError(
                    "TARIFF_INVALID_CURRENCY",
                    $"Currency '{model.Currency}' is not a valid ISO 4217 code.",
                    "Provide a 3-letter ISO 4217 currency code (e.g. 'EUR', 'USD')."
                )
                {
                    PropertyPath = "Currency",
                }
            );
        }
    }

    private static void ValidateElements(Tariff model, List<OcpiValidationError> errors)
    {
        if (model.Elements.Count == 0)
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
            return;
        }

        for (var i = 0; i < model.Elements.Count; i++)
        {
            var element = model.Elements[i];
            if (element.PriceComponents.Count == 0)
            {
                errors.Add(
                    new OcpiValidationError(
                        "TARIFF_ELEMENT_NO_PRICE_COMPONENTS",
                        $"TariffElement at index {i} has no price components.",
                        "Each TariffElement must have at least one PriceComponent."
                    )
                    {
                        PropertyPath = $"Elements[{i}].PriceComponents",
                    }
                );
            }

            for (var j = 0; j < element.PriceComponents.Count; j++)
            {
                var component = element.PriceComponents[j];
                if (component.StepSize <= 0)
                {
                    errors.Add(
                        new OcpiValidationError(
                            "TARIFF_INVALID_STEP_SIZE",
                            $"PriceComponent step_size {component.StepSize} must be greater than 0.",
                            "Set step_size to a positive integer."
                        )
                        {
                            PropertyPath = $"Elements[{i}].PriceComponents[{j}].StepSize",
                        }
                    );
                }
            }
        }
    }

    private static void ValidateTimeRange(Tariff model, List<OcpiValidationError> errors)
    {
        if (model.StartDateTime is { } start && model.EndDateTime is { } end && end < start)
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

    private static void ValidateMinMaxPrice(Tariff model, List<OcpiValidationError> errors)
    {
        if (model.MinPrice is { } minPrice && model.MaxPrice is { } maxPrice && minPrice.ExclVat > maxPrice.ExclVat)
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
}
