using DotOcpi.Models.V2_2_1;

namespace DotOcpi.Validation;

/// <summary>
/// Validates SetChargingProfile and ChargingProfile models against OCPI 2.2.1 protocol rules.
/// </summary>
public sealed class ChargingProfileValidator : IOcpiValidator<SetChargingProfile>
{
    /// <inheritdoc />
    public OcpiValidationResult Validate(SetChargingProfile model)
    {
        var errors = new List<OcpiValidationError>();

        CommandValidator.ValidateResponseUrl(model.ResponseUrl, errors);
        ValidateProfile(model.ChargingProfile, errors);

        return errors.Count == 0 ? OcpiValidationResult.Valid() : OcpiValidationResult.Failed(errors);
    }

    private static void ValidateProfile(ChargingProfile profile, List<OcpiValidationError> errors)
    {
        if (profile.ChargingProfilePeriod.Count == 0)
        {
            errors.Add(
                new OcpiValidationError(
                    "CHARGING_PROFILE_NO_PERIODS",
                    "ChargingProfile must have at least one period.",
                    "Provide at least one ChargingProfilePeriod."
                )
                {
                    PropertyPath = "ChargingProfile.ChargingProfilePeriod",
                }
            );
        }

        if (profile.Duration is { } duration && duration < 0)
        {
            errors.Add(
                new OcpiValidationError(
                    "CHARGING_PROFILE_NEGATIVE_DURATION",
                    $"Duration {duration} is negative.",
                    "Duration must be zero or positive (in seconds)."
                )
                {
                    PropertyPath = "ChargingProfile.Duration",
                }
            );
        }

        if (profile.MinChargingRate is { } minRate && minRate < 0)
        {
            errors.Add(
                new OcpiValidationError(
                    "CHARGING_PROFILE_NEGATIVE_MIN_RATE",
                    $"MinChargingRate {minRate} is negative.",
                    "MinChargingRate must be zero or positive."
                )
                {
                    PropertyPath = "ChargingProfile.MinChargingRate",
                }
            );
        }
    }
}
