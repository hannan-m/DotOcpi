namespace DotOcpi.Registration;

/// <summary>
/// Normalized credentials response from a CPO, independent of OCPI version.
/// Extracts the common fields needed by the registration orchestrator.
/// </summary>
/// <param name="Token">The token issued by the CPO (Token B on initial registration, Token C on rotation).</param>
/// <param name="VersionsUrl">The CPO's versions endpoint URL.</param>
/// <param name="CountryCode">The CPO's country code.</param>
/// <param name="PartyId">The CPO's party ID.</param>
public sealed record CredentialsResponse(string Token, string VersionsUrl, string CountryCode, string PartyId);
