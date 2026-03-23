namespace DotOcpi;

/// <summary>
/// OCPI sentinel values for special protocol cases.
/// </summary>
public static class OcpiSentinel
{
    /// <summary>
    /// The "#NA" sentinel value used in OCPI to indicate a field value is "not available".
    /// Per the OCPI spec, a required string field can carry this value when the data is
    /// genuinely unavailable. This is distinct from null (field not set) and empty string.
    /// </summary>
    public const string NotAvailable = "#NA";

    /// <summary>
    /// Returns true if the given value is the OCPI "not available" sentinel.
    /// </summary>
    public static bool IsNotAvailable(string? value) => string.Equals(value, NotAvailable, StringComparison.Ordinal);
}
