namespace DotOcpi.Models.V2_2;

/// <summary>
/// The status of an EVSE.
/// </summary>
public enum Status
{
    AVAILABLE,
    BLOCKED,
    CHARGING,
    INOPERATIVE,
    OUTOFORDER,
    PLANNED,
    REMOVED,
    RESERVED,
    UNKNOWN,
}
