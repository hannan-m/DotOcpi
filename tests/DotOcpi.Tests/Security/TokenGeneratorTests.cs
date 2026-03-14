using DotOcpi.Security;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests.Security;

[Trait("Category", "Security")]
public class TokenGeneratorTests
{
    [Fact]
    public void Generate_ProducesBase64UrlString()
    {
        var token = TokenGenerator.Generate();

        token.Should().NotBeNullOrEmpty();
        token.Should().NotContain("+");
        token.Should().NotContain("/");
        token.Should().NotContain("=");
    }

    [Fact]
    public void Generate_ProducesMinimum64ByteEquivalent()
    {
        var token = TokenGenerator.Generate();

        // base64url without padding: 64 bytes -> ceil(64*4/3) = 86 chars
        token.Length.Should().BeGreaterThanOrEqualTo(85);
    }

    [Fact]
    public void Generate_ProducesUniqueTokens()
    {
        var tokens = Enumerable.Range(0, 100).Select(_ => TokenGenerator.Generate()).ToHashSet();

        tokens.Should().HaveCount(100);
    }

    [Fact]
    public void Generate_CustomLength_Produces128Bytes()
    {
        var token = TokenGenerator.Generate(128);

        // 128 bytes -> ceil(128*4/3) = 172 chars (base64url, no padding)
        token.Length.Should().BeGreaterThanOrEqualTo(170);
    }

    [Fact]
    public void Generate_LessThanMinimum_Throws()
    {
        var act = () => TokenGenerator.Generate(32);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Generate_ExactMinimum_Succeeds()
    {
        var token = TokenGenerator.Generate(TokenGenerator.MinTokenBytes);

        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Generate_OutputIsDecodable()
    {
        var token = TokenGenerator.Generate();

        // Reverse base64url to standard base64
        var base64 = token.Replace('-', '+').Replace('_', '/');
        var padding = (4 - base64.Length % 4) % 4;
        base64 += new string('=', padding);

        var bytes = Convert.FromBase64String(base64);
        bytes.Length.Should().Be(TokenGenerator.MinTokenBytes);
    }
}
