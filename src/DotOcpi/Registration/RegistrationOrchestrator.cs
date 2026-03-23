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
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of <see cref="RegistrationOrchestrator"/>.
    /// </summary>
    public RegistrationOrchestrator(
        IVersionDiscovery versionDiscovery,
        ICredentialsClient credentialsClient,
        ICpoRegistry registry,
        ITokenStore tokenStore,
        TimeProvider timeProvider
    )
    {
        _versionDiscovery = versionDiscovery;
        _credentialsClient = credentialsClient;
        _registry = registry;
        _tokenStore = tokenStore;
        _timeProvider = timeProvider;
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

        var moduleEndpoints = ExtractAndValidateEndpoints(versionDetail);

        var now = _timeProvider.GetUtcNow();
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

        var updated = existing with { TokenBHash = tokenCHash, UpdatedAt = _timeProvider.GetUtcNow() };

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
    ) =>
        version switch
        {
            OcpiVersion.V2_2_1 => new Models.V2_2_1.Credentials
            {
                Token = token,
                Url = versionsUrl,
                Roles =
                [
                    new Models.V2_2_1.CredentialsRole
                    {
                        Role = Models.V2_2_1.Role.EMSP,
                        BusinessDetails = new Models.V2_2_1.BusinessDetails { Name = businessName },
                        PartyId = new CiString(partyId),
                        CountryCode = new CiString(countryCode),
                    },
                ],
            },
            OcpiVersion.V2_2 => new Models.V2_2.Credentials
            {
                Token = token,
                Url = versionsUrl,
                Roles =
                [
                    new Models.V2_2.CredentialsRole
                    {
                        Role = Models.V2_2.Role.EMSP,
                        BusinessDetails = new Models.V2_2.BusinessDetails { Name = businessName },
                        PartyId = new CiString(partyId),
                        CountryCode = new CiString(countryCode),
                    },
                ],
            },
            OcpiVersion.V2_1_1 => new Models.V2_1_1.Credentials
            {
                Token = token,
                Url = versionsUrl,
                BusinessName = businessName,
                PartyId = partyId,
                CountryCode = countryCode,
            },
            OcpiVersion.V2_0 => new Models.V2_0.Credentials
            {
                Token = token,
                Url = versionsUrl,
                BusinessName = businessName,
                PartyId = partyId,
                CountryCode = countryCode,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(version)),
        };

    /// <summary>
    /// Extracts module endpoints from version detail, validating each URL uses HTTPS.
    /// Rejects HTTP, non-absolute, or malformed URLs to prevent open-redirect and
    /// ensure the client-side SSRF guard (which validates post-DNS-resolution) has
    /// a valid HTTPS URL to work with.
    /// </summary>
    private static Dictionary<string, string> ExtractAndValidateEndpoints(VersionDetailInfo versionDetail)
    {
        var endpoints = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var ep in versionDetail.Endpoints)
        {
            if (string.Equals(ep.Identifier, "credentials", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!Uri.TryCreate(ep.Url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            {
                throw new OcpiRegistrationException(
                    $"CPO endpoint URL for module '{ep.Identifier}' must use HTTPS. Got: {ep.Url}"
                );
            }

            endpoints[ep.Identifier] = ep.Url;
        }

        return endpoints;
    }

    private static void ValidateHttps(string url, string name)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new OcpiRegistrationException($"{name} must use HTTPS. Got: {url}");
        }
    }
}
