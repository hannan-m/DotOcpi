using System.Diagnostics.CodeAnalysis;

namespace DotOcpi;

/// <summary>
/// OCPI protocol versions supported by this library.
/// </summary>
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Underscores represent OCPI version numbers")]
public enum OcpiVersion
{
    /// <summary>OCPI 2.0</summary>
    V2_0,

    /// <summary>OCPI 2.1.1 (preferred over deprecated 2.1)</summary>
    V2_1_1,

    /// <summary>OCPI 2.2 (deprecated intermediary — use 2.2.1)</summary>
    V2_2,

    /// <summary>OCPI 2.2.1 (current stable)</summary>
    V2_2_1,
}
