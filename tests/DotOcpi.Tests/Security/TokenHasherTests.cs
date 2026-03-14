using DotOcpi.Security;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests.Security;

[Trait("Category", "Security")]
public class TokenHasherTests
{
    [Fact]
    public void Hash_ProducesConsistentResult()
    {
        var token = "test-token-value";

        var hash1 = TokenHasher.Hash(token);
        var hash2 = TokenHasher.Hash(token);

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void Hash_ProducesHex64Characters()
    {
        var hash = TokenHasher.Hash("any-token");

        hash.Should().HaveLength(64);
        hash.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void Hash_DifferentTokens_ProduceDifferentHashes()
    {
        var hash1 = TokenHasher.Hash("token-a");
        var hash2 = TokenHasher.Hash("token-b");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Hash_KnownValue_MatchesExpected()
    {
        // SHA-256 of "hello" is well-known
        var hash = TokenHasher.Hash("hello");

        hash.Should().Be("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824");
    }

    [Fact]
    public void Hash_EmptyString_ProducesValidHash()
    {
        // SHA-256 of empty string
        var hash = TokenHasher.Hash("");

        hash.Should().Be("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
    }

    [Fact]
    public void Hash_LongToken_Succeeds()
    {
        var longToken = new string('A', 1024);

        var hash = TokenHasher.Hash(longToken);

        hash.Should().HaveLength(64);
    }

    [Fact]
    public void Hash_GeneratedToken_ProducesValidHash()
    {
        var token = TokenGenerator.Generate();

        var hash = TokenHasher.Hash(token);

        hash.Should().HaveLength(64);
        hash.Should().MatchRegex("^[0-9a-f]{64}$");
    }
}
