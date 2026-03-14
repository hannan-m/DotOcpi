namespace DotOcpi.Client;

/// <summary>
/// Retrieves raw OCPI tokens for authenticating outbound requests to CPOs.
/// Consumers implement this to integrate with their secret store (KeyVault, database, etc.).
/// </summary>
public interface IOutboundTokenProvider
{
    /// <summary>
    /// Gets the raw Token B for authenticating requests to a CPO.
    /// </summary>
    /// <param name="cpoId">The CPO connection key (e.g., "DE:ALL").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The raw token string.</returns>
    ValueTask<string> GetTokenAsync(string cpoId, CancellationToken cancellationToken = default);
}
