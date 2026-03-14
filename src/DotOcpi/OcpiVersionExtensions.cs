using System.Diagnostics.CodeAnalysis;

namespace DotOcpi;

/// <summary>
/// Extension methods for <see cref="OcpiVersion"/>.
/// </summary>
public static class OcpiVersionExtensions
{
    /// <summary>
    /// Returns the OCPI wire format string (e.g., "2.0", "2.1.1", "2.2", "2.2.1").
    /// </summary>
    public static string ToVersionString(this OcpiVersion version) =>
        version switch
        {
            OcpiVersion.V2_0 => "2.0",
            OcpiVersion.V2_1_1 => "2.1.1",
            OcpiVersion.V2_2 => "2.2",
            OcpiVersion.V2_2_1 => "2.2.1",
            _ => throw new ArgumentOutOfRangeException(nameof(version), version, $"Unknown OCPI version: {version}"),
        };

    /// <summary>
    /// Tries to parse a wire format version string into an <see cref="OcpiVersion"/>.
    /// Accepts deprecated versions "2.1" (maps to V2_1_1) for compatibility during version discovery.
    /// </summary>
    public static bool TryParse(string? value, [NotNullWhen(true)] out OcpiVersion? version)
    {
        switch (value)
        {
            case "2.0":
                version = OcpiVersion.V2_0;
                return true;
            case "2.1":
            case "2.1.1":
                version = OcpiVersion.V2_1_1;
                return true;
            case "2.2":
                version = OcpiVersion.V2_2;
                return true;
            case "2.2.1":
                version = OcpiVersion.V2_2_1;
                return true;
            default:
                version = null;
                return false;
        }
    }

    /// <summary>
    /// Returns true if this version uses the 2.2+ URL pattern with country_code/party_id in paths.
    /// </summary>
    public static bool UsesPartyIdInUrls(this OcpiVersion version) =>
        version is OcpiVersion.V2_2 or OcpiVersion.V2_2_1;

    /// <summary>
    /// Returns true if this is a deprecated intermediary version (2.2).
    /// </summary>
    public static bool IsDeprecated(this OcpiVersion version) => version is OcpiVersion.V2_2;
}
