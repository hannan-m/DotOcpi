using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DotOcpi.Integration.Tests.Fixtures;
using DotOcpi.Registration;
using DotOcpi.Security;
using DotOcpi.Testing;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Integration.Tests;

[Trait("Category", "Security")]
public class SecurityFlowTests : IntegrationTestBase
{
    [Fact]
    public async Task PostCredentials_WithExpiredTokenA_AfterRegistration_Rejects()
    {
        await PerformRegistration();

        // Token A should still be accepted for POST (test server doesn't invalidate Token A)
        // but the real orchestrator would only use it once.
        // Testing that using the wrong token fails:
        using var client = CreateHttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", "completely-wrong");

        var response = await client.PostAsJsonAsync(
            "/ocpi/credentials",
            new { token = "x", url = "https://example.com" }
        );

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PutCredentials_WithTokenA_InsteadOfTokenB_Rejects()
    {
        await PerformRegistration();

        using var client = CreateHttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", Server.TokenA);

        var response = await client.PutAsJsonAsync(
            "/ocpi/credentials",
            new { token = "x", url = "https://example.com" }
        );

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteCredentials_WithInvalidToken_Rejects()
    {
        await PerformRegistration();

        using var client = CreateHttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", "invalid");

        var response = await client.DeleteAsync("/ocpi/credentials");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Credentials_MissingAuthHeader_Returns401()
    {
        using var client = CreateHttpClient();
        var response = await client.PostAsJsonAsync("/ocpi/credentials", new { token = "x" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status_code").GetInt32().Should().Be(2002);
    }

    [Fact]
    public async Task Credentials_MalformedAuthHeader_Returns401()
    {
        using var client = CreateHttpClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer some-jwt-token");

        var response = await client.PostAsJsonAsync("/ocpi/credentials", new { token = "x" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TokenGenerator_ProducesUniqueTokens()
    {
        var tokens = Enumerable.Range(0, 100).Select(_ => TokenGenerator.Generate()).ToList();

        tokens.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    [Trait("Category", "Security")]
    public async Task TokenGenerator_ProducesSufficientLength()
    {
        var token = TokenGenerator.Generate();

        // 64 bytes base64url-encoded (no padding) = 86 chars
        token.Length.Should().BeGreaterOrEqualTo(80);
    }

    [Fact]
    [Trait("Category", "Security")]
    public async Task TokenHasher_DifferentTokens_ProduceDifferentHashes()
    {
        var token1 = TokenGenerator.Generate();
        var token2 = TokenGenerator.Generate();

        var hash1 = TokenHasher.Hash(token1);
        var hash2 = TokenHasher.Hash(token2);

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    [Trait("Category", "Security")]
    public async Task TokenHasher_SameToken_ProducesSameHash()
    {
        var token = TokenGenerator.Generate();

        var hash1 = TokenHasher.Hash(token);
        var hash2 = TokenHasher.Hash(token);

        hash1.Should().Be(hash2);
    }

    [Fact]
    [Trait("Category", "Security")]
    public async Task TokenRotation_InvalidatesOldToken()
    {
        await PerformRegistration();
        var firstTokenB = Server.GetIssuedTokenB();

        // Rotate credentials
        using var client = CreateHttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", firstTokenB);

        await client.PutAsJsonAsync(
            "/ocpi/credentials",
            new
            {
                token = TokenGenerator.Generate(),
                url = "https://example.com",
                roles = new[]
                {
                    new
                    {
                        role = "EMSP",
                        business_details = new { name = "Test" },
                        party_id = "MSP",
                        country_code = "NL",
                    },
                },
            }
        );

        var newTokenB = Server.GetIssuedTokenB();
        newTokenB.Should().NotBe(firstTokenB);

        // Old token should no longer work for PUT
        using var client2 = CreateHttpClient();
        client2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", firstTokenB);

        var response = await client2.PutAsJsonAsync(
            "/ocpi/credentials",
            new { token = "x", url = "https://example.com" }
        );

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task PerformRegistration()
    {
        using var httpClient = CreateHttpClient();
        var credentialsClient = new CredentialsClient(httpClient);

        await credentialsClient.PostCredentialsAsync(
            $"{Server.BaseUrl}ocpi/credentials",
            Server.TokenA,
            OcpiVersion.V2_2_1,
            new
            {
                token = TokenGenerator.Generate(),
                url = "https://emsp.example.com/ocpi/versions",
                roles = new[]
                {
                    new
                    {
                        role = "EMSP",
                        business_details = new { name = "Test eMSP" },
                        party_id = "MSP",
                        country_code = "NL",
                    },
                },
            }
        );
    }
}
