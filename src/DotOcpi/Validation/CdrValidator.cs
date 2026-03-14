using DotOcpi.Models.V2_2_1;

namespace DotOcpi.Validation;

/// <summary>
/// Validates CDR models against OCPI 2.2.1 protocol rules.
/// </summary>
public sealed class CdrValidator : IOcpiValidator<Cdr>
{
    /// <inheritdoc />
    public OcpiValidationResult Validate(Cdr model)
    {
        var errors = new List<OcpiValidationError>();

        ValidateTimeRange(model, errors);
        ValidateEnergy(model, errors);
        ValidateTime(model, errors);
        ValidateCurrency(model, errors);
        ValidateChargingPeriods(model, errors);
        ValidateCreditConsistency(model, errors);
        ValidateTotalCost(model, errors);

        return errors.Count == 0 ? OcpiValidationResult.Valid() : OcpiValidationResult.Failed(errors);
    }

    private static void ValidateTimeRange(Cdr model, List<OcpiValidationError> errors)
    {
        if (model.EndDateTime < model.StartDateTime)
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
    }

    private static void ValidateEnergy(Cdr model, List<OcpiValidationError> errors)
    {
        if (model.TotalEnergy < 0)
        {
            errors.Add(
                new OcpiValidationError(
                    "CDR_NEGATIVE_ENERGY",
                    $"TotalEnergy value {model.TotalEnergy} is negative.",
                    "Total energy must be zero or positive."
                )
                {
                    PropertyPath = "TotalEnergy",
                }
            );
        }
    }

    private static void ValidateTime(Cdr model, List<OcpiValidationError> errors)
    {
        if (model.TotalTime < 0)
        {
            errors.Add(
                new OcpiValidationError(
                    "CDR_NEGATIVE_TIME",
                    $"TotalTime value {model.TotalTime} is negative.",
                    "Total time must be zero or positive."
                )
                {
                    PropertyPath = "TotalTime",
                }
            );
        }

        if (model.TotalParkingTime is { } parkingTime && parkingTime < 0)
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
    }

    private static void ValidateCurrency(Cdr model, List<OcpiValidationError> errors)
    {
        if (model.Currency.Length != 3 || !model.Currency.All(char.IsLetter))
        {
            errors.Add(
                new OcpiValidationError(
                    "CDR_INVALID_CURRENCY",
                    $"Currency '{model.Currency}' is not a valid ISO 4217 code.",
                    "Provide a 3-letter ISO 4217 currency code (e.g. 'EUR', 'USD')."
                )
                {
                    PropertyPath = "Currency",
                }
            );
        }
    }

    private static void ValidateChargingPeriods(Cdr model, List<OcpiValidationError> errors)
    {
        if (model.ChargingPeriods.Count == 0)
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

    private static void ValidateCreditConsistency(Cdr model, List<OcpiValidationError> errors)
    {
        if (model.Credit == true && model.CreditReferenceId is null)
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

    private static void ValidateTotalCost(Cdr model, List<OcpiValidationError> errors)
    {
        if (model.TotalCost.ExclVat < 0)
        {
            errors.Add(
                new OcpiValidationError(
                    "CDR_NEGATIVE_TOTAL_COST",
                    $"TotalCost.ExclVat value {model.TotalCost.ExclVat} is negative.",
                    "Total cost must be zero or positive."
                )
                {
                    PropertyPath = "TotalCost.ExclVat",
                }
            );
        }
    }
}
