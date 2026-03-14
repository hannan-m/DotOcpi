namespace DotOcpi.Models.V2_2_1;

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
