using System.Globalization;

namespace DotOcpi;

/// <summary>
/// Shared OCPI date-time formatting. All OCPI timestamps use UTC with
/// second precision in ISO 8601 / RFC 3339 format.
/// </summary>
public static class OcpiDateTime
{
    private const string OcpiFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    /// <summary>
    /// Formats a <see cref="DateTimeOffset"/> as an OCPI UTC timestamp string
    /// (e.g., <c>2024-03-18T14:30:00Z</c>).
    /// </summary>
    public static string Format(DateTimeOffset value) =>
        value.UtcDateTime.ToString(OcpiFormat, CultureInfo.InvariantCulture);
}
