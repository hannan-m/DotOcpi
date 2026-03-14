using System.Diagnostics.CodeAnalysis;

namespace DotOcpi.Models.V2_0;

/// <summary>
/// The capabilities of an EVSE.
/// </summary>
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "OCPI spec names")]
public enum Capability
{
    CHARGING_PROFILE_CAPABLE,
    CHIP_CARD_SUPPORT,
    CONTACTLESS_CARD_SUPPORT,
    CREDIT_CARD_PAYABLE,
    PED_TERMINAL,
    REMOTE_START_STOP_CAPABLE,
    RESERVABLE,
    RFID_READER,
    UNLOCK_CAPABLE,
}
