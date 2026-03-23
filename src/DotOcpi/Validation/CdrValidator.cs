namespace DotOcpi.Validation;

/// <summary>
/// Validates CDR models against OCPI protocol rules across all supported versions.
/// V2_0/V2_1_1: time range, energy, time, currency, charging periods, total cost (decimal).
/// V2_2/V2_2_1: all of the above plus credit consistency and total cost (Price object).
/// </summary>
public sealed class CdrValidator
    : IOcpiValidator<Models.V2_0.Cdr>,
        IOcpiValidator<Models.V2_1_1.Cdr>,
        IOcpiValidator<Models.V2_2.Cdr>,
        IOcpiValidator<Models.V2_2_1.Cdr>
{
    /// <inheritdoc />
    public OcpiValidationResult Validate(Models.V2_0.Cdr model)
    {
        var errors = new List<OcpiValidationError>();

        ValidateCore(
            model.StartDateTime,
            model.EndDateTime,
            model.TotalEnergy,
            model.TotalTime,
            model.TotalParkingTime,
            model.Currency,
            model.ChargingPeriods.Count,
            errors
        );
        ValidateTotalCostDecimal(model.TotalCost, errors);

        return errors.Count == 0 ? OcpiValidationResult.Valid() : OcpiValidationResult.Failed(errors);
    }

    /// <inheritdoc />
    public OcpiValidationResult Validate(Models.V2_1_1.Cdr model)
    {
        var errors = new List<OcpiValidationError>();

        ValidateCore(
            model.StartDateTime,
            model.EndDateTime,
            model.TotalEnergy,
            model.TotalTime,
            model.TotalParkingTime,
            model.Currency,
            model.ChargingPeriods.Count,
            errors
        );
        ValidateTotalCostDecimal(model.TotalCost, errors);

        return errors.Count == 0 ? OcpiValidationResult.Valid() : OcpiValidationResult.Failed(errors);
    }

    /// <inheritdoc />
    public OcpiValidationResult Validate(Models.V2_2.Cdr model)
    {
        var errors = new List<OcpiValidationError>();

        ValidateCore(
            model.StartDateTime,
            model.EndDateTime,
            model.TotalEnergy,
            model.TotalTime,
            model.TotalParkingTime,
            model.Currency,
            model.ChargingPeriods.Count,
            errors
        );
        ValidateCreditConsistency(model.Credit, model.CreditReferenceId is not null, errors);
        ValidateTotalCostPrice(model.TotalCost.ExclVat, errors);

        return errors.Count == 0 ? OcpiValidationResult.Valid() : OcpiValidationResult.Failed(errors);
    }

    /// <inheritdoc />
    public OcpiValidationResult Validate(Models.V2_2_1.Cdr model)
    {
        var errors = new List<OcpiValidationError>();

        ValidateCore(
            model.StartDateTime,
            model.EndDateTime,
            model.TotalEnergy,
            model.TotalTime,
            model.TotalParkingTime,
            model.Currency,
            model.ChargingPeriods.Count,
            errors
        );
        ValidateCreditConsistency(model.Credit, model.CreditReferenceId is not null, errors);
        ValidateTotalCostPrice(model.TotalCost.ExclVat, errors);

        return errors.Count == 0 ? OcpiValidationResult.Valid() : OcpiValidationResult.Failed(errors);
    }

    OcpiValidationResult IOcpiValidator.Validate(object model) =>
        model switch
        {
            Models.V2_0.Cdr m => Validate(m),
            Models.V2_1_1.Cdr m => Validate(m),
            Models.V2_2.Cdr m => Validate(m),
            Models.V2_2_1.Cdr m => Validate(m),
            _ => throw new ArgumentException($"Unsupported CDR type: {model.GetType().Name}", nameof(model)),
        };

    private static void ValidateCore(
        DateTimeOffset startDateTime,
        DateTimeOffset endDateTime,
        decimal totalEnergy,
        decimal totalTime,
        decimal? totalParkingTime,
        string currency,
        int chargingPeriodCount,
        List<OcpiValidationError> errors
    )
    {
        if (endDateTime < startDateTime)
        {
            errors.Add(
                new OcpiValidationError(
                    "CDR_END_BEFORE_START",
                    "EndDateTime is before StartDateTime.",
                    "EndDateTime must be equal to or after StartDateTime."
                )
                {
                    PropertyPath = "EndDateTime",
                }
            );
        }

        if (totalEnergy < 0)
        {
            errors.Add(
                new OcpiValidationError(
                    "CDR_NEGATIVE_ENERGY",
                    $"TotalEnergy value {totalEnergy} is negative.",
                    "Total energy must be zero or positive."
                )
                {
                    PropertyPath = "TotalEnergy",
                }
            );
        }

        if (totalTime < 0)
        {
            errors.Add(
                new OcpiValidationError(
                    "CDR_NEGATIVE_TIME",
                    $"TotalTime value {totalTime} is negative.",
                    "Total time must be zero or positive."
                )
                {
                    PropertyPath = "TotalTime",
                }
            );
        }

        if (totalParkingTime is { } parkingTime && parkingTime < 0)
        {
            errors.Add(
                new OcpiValidationError(
                    "CDR_NEGATIVE_PARKING_TIME",
                    $"TotalParkingTime value {parkingTime} is negative.",
                    "Total parking time must be zero or positive."
                )
                {
                    PropertyPath = "TotalParkingTime",
                }
            );
        }

        ValidationHelpers.ValidateCurrency(currency, "CDR_INVALID_CURRENCY", errors);

        if (chargingPeriodCount == 0)
        {
            errors.Add(
                new OcpiValidationError(
                    "CDR_NO_CHARGING_PERIODS",
                    "CDR has no charging periods.",
                    "A CDR must have at least one charging period."
                )
                {
                    PropertyPath = "ChargingPeriods",
                }
            );
        }
    }

    private static void ValidateCreditConsistency(
        bool? credit,
        bool hasCreditReferenceId,
        List<OcpiValidationError> errors
    )
    {
        if (credit == true && !hasCreditReferenceId)
        {
            errors.Add(
                new OcpiValidationError(
                    "CDR_CREDIT_MISSING_REFERENCE",
                    "CDR is marked as credit but CreditReferenceId is not set.",
                    "Provide a CreditReferenceId when Credit is true."
                )
                {
                    PropertyPath = "CreditReferenceId",
                }
            );
        }
    }

    private static void ValidateTotalCostDecimal(decimal totalCost, List<OcpiValidationError> errors)
    {
        if (totalCost < 0)
        {
            errors.Add(
                new OcpiValidationError(
                    "CDR_NEGATIVE_TOTAL_COST",
                    $"TotalCost value {totalCost} is negative.",
                    "Total cost must be zero or positive."
                )
                {
                    PropertyPath = "TotalCost",
                }
            );
        }
    }

    private static void ValidateTotalCostPrice(decimal exclVat, List<OcpiValidationError> errors)
    {
        if (exclVat < 0)
        {
            errors.Add(
                new OcpiValidationError(
                    "CDR_NEGATIVE_TOTAL_COST",
                    $"TotalCost.ExclVat value {exclVat} is negative.",
                    "Total cost must be zero or positive."
                )
                {
                    PropertyPath = "TotalCost.ExclVat",
                }
            );
        }
    }
}
