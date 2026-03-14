using DotOcpi.Exceptions;
using DotOcpi.Registry;
using DotOcpi.Security;

namespace DotOcpi.Registration;

/// <summary>
/// Orchestrates the OCPI registration handshake:
/// 1. Discover CPO versions
/// 2. Negotiate highest mutual version
/// 3. Fetch version detail (module endpoints)
/// 4. Generate Token B, POST credentials with Token A
/// 5. Store Token B hash and CPO connection in registry
/// </summary>
public sealed class RegistrationOrchestrator : IRegistrationClient
{
    private readonly IVersionDiscovery _versionDiscovery;
    private readonly ICredentialsClient _credentialsClient;
    private readonly ICpoRegistry _registry;
    private readonly ITokenStore _tokenStore;

    /// <summary>
    /// Initializes a new instance of <see cref="RegistrationOrchestrator"/>.
    /// </summary>
    public RegistrationOrchestrator(
        IVersionDiscovery versionDiscovery,
        ICredentialsClient credentialsClient,
        ICpoRegistry registry,
        ITokenStore tokenStore
    )
    {
        _versionDiscovery = versionDiscovery;
        _credentialsClient = credentialsClient;
        _registry = registry;
        _tokenStore = tokenStore;
    }

    /// <inheritdoc />
    public async Task<RegistrationResult> RegisterAsync(
        RegistrationRequest request,
        CancellationToken cancellationToken = default
    )
    {
        ValidateHttps(request.VersionsUrl, "CPO versions URL");
        ValidateHttps(request.EmspVersionsUrl, "eMSP versions URL");

        // Step 1: Discover available versions from the CPO
        var versions = await _versionDiscovery
            .GetVersionsAsync(request.VersionsUrl, request.TokenA, cancellationToken)
            .ConfigureAwait(false);

        // Step 2: Negotiate the highest mutual version
        var versionStrings = versions.Select(v => v.Version);
        var negotiated =
            VersionNegotiator.Negotiate(versionStrings, request.SupportedVersions)
            ?? throw new OcpiRegistrationException("No mutually supported OCPI version found with CPO.");

        // Find the version URL for the negotiated version
        var negotiatedVersionString = negotiated.ToVersionString();
        var versionEntry =
            versions.FirstOrDefault(v => v.Version == negotiatedVersionString)
            ?? throw new OcpiRegistrationException(
                $"CPO does not provide endpoint URL for negotiated version {negotiatedVersionString}."
            );

        // Step 3: Fetch version detail to get module endpoints
        var versionDetail = await _versionDiscovery
            .GetVersionDetailAsync(versionEntry.Url, request.TokenA, cancellationToken)
            .ConfigureAwait(false);

        var credentialsUrl = FindCredentialsEndpoint(versionDetail);

        // Step 4: Generate Token B and build our credentials
        var tokenB = TokenGenerator.Generate();
        var tokenBHash = TokenHasher.Hash(tokenB);

        var ourCredentials = BuildCredentials(
            negotiated,
            tokenB,
            request.EmspVersionsUrl,
            request.EmspCountryCode,
            request.EmspPartyId,
            request.EmspBusinessName
        );

        // POST credentials with Token A — CPO responds with their token for us
        var cpoResponse = await _credentialsClient
            .PostCredentialsAsync(credentialsUrl, request.TokenA, negotiated, ourCredentials, cancellationToken)
            .ConfigureAwait(false);

        // Step 5: Store our Token B hash (for validating incoming CPO requests)
        var partyId = $"{request.EmspCountryCode}:{request.EmspPartyId}";
        await _tokenStore.StoreAsync(tokenBHash, TokenPurpose.TokenB, partyId, cancellationToken).ConfigureAwait(false);

        var moduleEndpoints = versionDetail
            .Endpoints.Where(e => !string.Equals(e.Identifier, "credentials", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(e => e.Identifier, e => e.Url, StringComparer.OrdinalIgnoreCase);

        var now = DateTimeOffset.UtcNow;
        var connection = new CpoConnection
        {
            CpoCountryCode = cpoResponse.CountryCode,
            CpoPartyId = cpoResponse.PartyId,
            EmspCountryCode = request.EmspCountryCode,
            EmspPartyId = request.EmspPartyId,
            Version = negotiated,
            ModuleEndpoints = moduleEndpoints,
            TokenBHash = tokenBHash,
            CpoVersionsUrl = request.VersionsUrl,
            EmspVersionsUrl = request.EmspVersionsUrl,
            Status = ConnectionStatus.Connected,
            CreatedAt = now,
            UpdatedAt = now,
        };

        if (!_registry.AddOrUpdate(connection))
        {
            throw new OcpiRegistrationException(
                $"Failed to register CPO connection {connection.ConnectionKey} due to concurrency conflict."
            );
        }

        return new RegistrationResult(connection, cpoResponse.Token);
    }

    /// <inheritdoc />
    public async Task<RegistrationResult> RotateCredentialsAsync(
        CredentialRotationRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var existing =
            _registry.FindByConnectionKey(request.ConnectionKey)
            ?? throw new OcpiRegistrationException($"No CPO connection found for key '{request.ConnectionKey}'.");

        if (string.IsNullOrEmpty(existing.CpoVersionsUrl))
        {
            throw new OcpiRegistrationException(
                $"CPO connection '{request.ConnectionKey}' has no stored versions URL."
            );
        }

        // Re-discover version detail to get current credentials endpoint
        var versionDetail = await _versionDiscovery
            .GetVersionDetailAsync(existing.CpoVersionsUrl, request.CurrentCpoToken, cancellationToken)
            .ConfigureAwait(false);

        var credentialsUrl = FindCredentialsEndpoint(versionDetail);

        // Generate new Token C (becomes new Token B)
        var tokenC = TokenGenerator.Generate();
        var tokenCHash = TokenHasher.Hash(tokenC);

        var ourCredentials = BuildCredentials(
            existing.Version,
            tokenC,
            existing.EmspVersionsUrl
                ?? throw new OcpiRegistrationException(
                    $"CPO connection '{request.ConnectionKey}' has no stored eMSP versions URL."
                ),
            existing.EmspCountryCode,
            existing.EmspPartyId,
            request.EmspBusinessName
        );

        // PUT credentials with current CPO token
        var cpoResponse = await _credentialsClient
            .PutCredentialsAsync(
                credentialsUrl,
                request.CurrentCpoToken,
                existing.Version,
                ourCredentials,
                cancellationToken
            )
            .ConfigureAwait(false);

        // Store new token hash, remove old
        var partyId = $"{existing.EmspCountryCode}:{existing.EmspPartyId}";
        await _tokenStore.StoreAsync(tokenCHash, TokenPurpose.TokenB, partyId, cancellationToken).ConfigureAwait(false);
        await _tokenStore.RemoveAsync(existing.TokenBHash, cancellationToken).ConfigureAwait(false);

        var updated = existing with { TokenBHash = tokenCHash, UpdatedAt = DateTimeOffset.UtcNow };

        if (!_registry.AddOrUpdate(updated))
        {
            throw new OcpiRegistrationException(
                $"Failed to update CPO connection {request.ConnectionKey} due to concurrency conflict."
            );
        }

        return new RegistrationResult(updated, cpoResponse.Token);
    }

    /// <inheritdoc />
    public async Task UnregisterAsync(UnregisterRequest request, CancellationToken cancellationToken = default)
    {
        var existing =
            _registry.FindByConnectionKey(request.ConnectionKey)
            ?? throw new OcpiRegistrationException($"No CPO connection found for key '{request.ConnectionKey}'.");

        if (string.IsNullOrEmpty(existing.CpoVersionsUrl))
        {
            throw new OcpiRegistrationException(
                $"CPO connection '{request.ConnectionKey}' has no stored versions URL."
            );
        }

        var versionDetail = await _versionDiscovery
            .GetVersionDetailAsync(existing.CpoVersionsUrl, request.CurrentCpoToken, cancellationToken)
            .ConfigureAwait(false);

        var credentialsUrl = FindCredentialsEndpoint(versionDetail);

        await _credentialsClient
            .DeleteCredentialsAsync(credentialsUrl, request.CurrentCpoToken, cancellationToken)
            .ConfigureAwait(false);

        await _tokenStore.RemoveAsync(existing.TokenBHash, cancellationToken).ConfigureAwait(false);
        _registry.Remove(request.ConnectionKey);
    }

    private static string FindCredentialsEndpoint(VersionDetailInfo versionDetail)
    {
        var endpoint = versionDetail.Endpoints.FirstOrDefault(e =>
            string.Equals(e.Identifier, "credentials", StringComparison.OrdinalIgnoreCase)
        );
        return endpoint?.Url
            ?? throw new OcpiRegistrationException("CPO version detail does not include a credentials endpoint.");
    }

    private static object BuildCredentials(
        OcpiVersion version,
        string token,
        string versionsUrl,
        string countryCode,
        string partyId,
        string businessName
    )
    {
        if (version.UsesPartyIdInUrls())
        {
            return new
            {
                token,
                url = versionsUrl,
                roles = new[]
                {
                    new
                    {
                        role = "EMSP",
                        business_details = new { name = businessName },
                        party_id = partyId,
                        country_code = countryCode,
                    },
                },
            };
        }

        return new
        {
            token,
            url = versionsUrl,
            business_name = businessName,
            party_id = partyId,
            country_code = countryCode,
        };
    }

    private static void ValidateHttps(string url, string name)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new OcpiRegistrationException($"{name} must use HTTPS. Got: {url}");
        }
    }
}
