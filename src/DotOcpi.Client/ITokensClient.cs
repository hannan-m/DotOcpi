using System.Text.Json;

namespace DotOcpi.Client;

/// <summary>
/// Pushes Token data to a CPO's tokens module endpoint.
/// The eMSP uses this to synchronize token data with CPOs.
/// </summary>
public interface ITokensClient
{
    /// <summary>
    /// Pushes a full token to a CPO (PUT). Creates or replaces the token.
    /// </summary>
    /// <param name="cpoId">The CPO connection key (e.g., "DE:ALL").</param>
    /// <param name="tokenUid">The unique token identifier.</param>
    /// <param name="token">The token object (version-specific model type).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<OcpiResult> PushTokenAsync(
        string cpoId,
        string tokenUid,
        object token,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Patches a token on a CPO (PATCH). Updates specific fields.
    /// </summary>
    /// <param name="cpoId">The CPO connection key (e.g., "DE:ALL").</param>
    /// <param name="tokenUid">The unique token identifier.</param>
    /// <param name="patch">The partial update as a JsonElement.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<OcpiResult> PatchTokenAsync(
        string cpoId,
        string tokenUid,
        JsonElement patch,
        CancellationToken cancellationToken = default
    );
}
