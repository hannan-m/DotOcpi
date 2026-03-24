using DotOcpi.Security;

namespace DotOcpi.Client.Internal;

/// <summary>
/// Default <see cref="IOutboundTokenProvider"/> that retrieves outbound CPO tokens
/// from <see cref="ITokenStore"/> and unprotects them via <see cref="ITokenProtector"/>.
/// Registered automatically by <see cref="DotOcpiClientExtensions.AddClient"/> unless
/// the consumer has already registered a custom <see cref="IOutboundTokenProvider"/>.
/// </summary>
internal sealed class TokenStoreOutboundTokenProvider : IOutboundTokenProvider
{
    private readonly ITokenStore _tokenStore;
    private readonly ITokenProtector _protector;

    internal TokenStoreOutboundTokenProvider(ITokenStore tokenStore, ITokenProtector protector)
    {
        _tokenStore = tokenStore;
        _protector = protector;
    }

    public async ValueTask<string> GetTokenAsync(string cpoId, CancellationToken cancellationToken = default)
    {
        var protectedToken = await _tokenStore.GetCpoTokenAsync(cpoId, cancellationToken).ConfigureAwait(false);

        if (protectedToken is null)
        {
            throw new InvalidOperationException(
                $"No CPO token found for '{cpoId}'. "
                    + "Ensure registration completed successfully, or implement "
                    + "ITokenStore.StoreCpoTokenAsync/GetCpoTokenAsync for your token store."
            );
        }

        return _protector.Unprotect(protectedToken);
    }
}
