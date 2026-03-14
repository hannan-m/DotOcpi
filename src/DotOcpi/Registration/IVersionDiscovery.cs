namespace DotOcpi.Registration;

/// <summary>
/// Discovers available OCPI versions and their endpoint details
/// from a CPO's versions endpoint.
/// </summary>
public interface IVersionDiscovery
{
    /// <summary>
    /// Retrieves the list of available versions from the CPO's versions endpoint.
    /// </summary>
    /// <param name="versionsUrl">The CPO's versions endpoint URL.</param>
    /// <param name="token">The authentication token to use.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The list of available versions.</returns>
    Task<IReadOnlyList<VersionInfo>> GetVersionsAsync(
        string versionsUrl,
        string token,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Retrieves the version detail (module endpoints) for a specific version.
    /// </summary>
    /// <param name="versionDetailUrl">The URL to the version detail endpoint.</param>
    /// <param name="token">The authentication token to use.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The version detail with module endpoints.</returns>
    Task<VersionDetailInfo> GetVersionDetailAsync(
        string versionDetailUrl,
        string token,
        CancellationToken cancellationToken = default
    );
}
