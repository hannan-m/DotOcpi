using System.Diagnostics.CodeAnalysis;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// Reservation-related restrictions for tariff elements.
/// </summary>
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "OCPI spec names")]
public enum ReservationRestrictionType
{
    RESERVATION,
    RESERVATION_EXPIRES,
}
