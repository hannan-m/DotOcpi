namespace DotOcpi.Registration;

/// <summary>
/// Negotiates the highest mutually supported OCPI version between
/// the eMSP and a CPO. Skips deprecated versions (2.1, 2.2) in
/// favor of their stable successors (2.1.1, 2.2.1).
/// </summary>
public static class VersionNegotiator
{
    // Ordered from highest to lowest preference
    private static readonly OcpiVersion[] PreferredOrder =
    [
        OcpiVersion.V2_2_1,
        OcpiVersion.V2_2,
        OcpiVersion.V2_1_1,
        OcpiVersion.V2_0,
    ];

    private static readonly Dictionary<string, OcpiVersion> VersionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["2.2.1"] = OcpiVersion.V2_2_1,
        ["2.2"] = OcpiVersion.V2_2,
        ["2.1.1"] = OcpiVersion.V2_1_1,
        ["2.1"] = OcpiVersion.V2_1_1, // Map deprecated 2.1 to 2.1.1
        ["2.0"] = OcpiVersion.V2_0,
    };

    /// <summary>
    /// Determines the highest mutually supported OCPI version.
    /// </summary>
    /// <param name="cpoVersionStrings">Version strings from the CPO's versions endpoint.</param>
    /// <param name="supportedVersions">Versions supported by this eMSP. If null, all versions are supported.</param>
    /// <returns>The negotiated version, or null if no mutual version exists.</returns>
    public static OcpiVersion? Negotiate(
        IEnumerable<string> cpoVersionStrings,
        IReadOnlySet<OcpiVersion>? supportedVersions = null
    )
    {
        var cpoVersions = new HashSet<OcpiVersion>();
        foreach (var versionString in cpoVersionStrings)
        {
            if (VersionMap.TryGetValue(versionString, out var version))
            {
                cpoVersions.Add(version);
            }
        }

        foreach (var preferred in PreferredOrder)
        {
            if (!cpoVersions.Contains(preferred))
            {
                continue;
            }

            if (supportedVersions is null || supportedVersions.Contains(preferred))
            {
                return preferred;
            }
        }

        return null;
    }

    /// <summary>
    /// Resolves a version string to an <see cref="OcpiVersion"/>.
    /// </summary>
    /// <param name="versionString">The version string (e.g., "2.2.1").</param>
    /// <param name="version">The resolved version.</param>
    /// <returns>True if the version string is recognized.</returns>
    public static bool TryResolve(string versionString, out OcpiVersion version) =>
        VersionMap.TryGetValue(versionString, out version);
}
