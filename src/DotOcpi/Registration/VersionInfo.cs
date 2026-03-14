namespace DotOcpi.Registration;

/// <summary>
/// Represents a version entry from the OCPI versions endpoint.
/// </summary>
/// <param name="Version">The OCPI version identifier (e.g., "2.2.1").</param>
/// <param name="Url">The URL to the version details endpoint.</param>
public sealed record VersionInfo(string Version, string Url);

/// <summary>
/// Represents a version detail with its module endpoints.
/// </summary>
/// <param name="Version">The OCPI version identifier.</param>
/// <param name="Endpoints">The module endpoints for this version.</param>
public sealed record VersionDetailInfo(string Version, IReadOnlyList<EndpointInfo> Endpoints);

/// <summary>
/// Represents a single module endpoint within a version.
/// </summary>
/// <param name="Identifier">The module identifier (e.g., "locations", "credentials").</param>
/// <param name="Role">The interface role ("SENDER" or "RECEIVER").</param>
/// <param name="Url">The module endpoint URL.</param>
public sealed record EndpointInfo(string Identifier, string Role, string Url);
