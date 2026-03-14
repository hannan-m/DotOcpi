using DotOcpi.Security;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests.Security;

[Trait("Category", "Security")]
public class AuthorizationHeaderParserTests
{
    [Fact]
    public void TryParse_ValidHeader_ReturnsTrue()
    {
        var result = AuthorizationHeaderParser.TryParse("Token abc123", out var token);

        result.Should().BeTrue();
        token.Should().Be("abc123");
    }

    [Fact]
    public void TryParse_ValidHeaderWithBase64Url_ExtractsFullToken()
    {
        var tokenValue = "dGhpcyBpcyBhIHRlc3QgdG9rZW4-_w";
        var result = AuthorizationHeaderParser.TryParse($"Token {tokenValue}", out var token);

        result.Should().BeTrue();
        token.Should().Be(tokenValue);
    }

    [Fact]
    public void TryParse_CaseInsensitivePrefix_ReturnsTrue()
    {
        var result = AuthorizationHeaderParser.TryParse("token abc123", out var token);

        result.Should().BeTrue();
        token.Should().Be("abc123");
    }

    [Fact]
    public void TryParse_UppercasePrefix_ReturnsTrue()
    {
        var result = AuthorizationHeaderParser.TryParse("TOKEN abc123", out var token);

        result.Should().BeTrue();
        token.Should().Be("abc123");
    }

    [Fact]
    public void TryParse_LeadingWhitespace_Trims()
    {
        var result = AuthorizationHeaderParser.TryParse("  Token abc123  ", out var token);

        result.Should().BeTrue();
        token.Should().Be("abc123");
    }

    [Fact]
    public void TryParse_EmptyString_ReturnsFalse()
    {
        var result = AuthorizationHeaderParser.TryParse("", out var token);

        result.Should().BeFalse();
        token.Should().BeEmpty();
    }

    [Fact]
    public void TryParse_OnlyPrefix_ReturnsFalse()
    {
        var result = AuthorizationHeaderParser.TryParse("Token ", out var token);

        result.Should().BeFalse();
        token.Should().BeEmpty();
    }

    [Fact]
    public void TryParse_PrefixOnly_ReturnsFalse()
    {
        var result = AuthorizationHeaderParser.TryParse("Token", out var token);

        result.Should().BeFalse();
        token.Should().BeEmpty();
    }

    [Fact]
    public void TryParse_BearerScheme_ReturnsFalse()
    {
        var result = AuthorizationHeaderParser.TryParse("Bearer abc123", out var token);

        result.Should().BeFalse();
        token.Should().BeEmpty();
    }

    [Fact]
    public void TryParse_BasicScheme_ReturnsFalse()
    {
        var result = AuthorizationHeaderParser.TryParse("Basic abc123", out var token);

        result.Should().BeFalse();
        token.Should().BeEmpty();
    }

    [Fact]
    public void TryParse_TokenWithSpaces_ExtractsFromPrefix()
    {
        // The token value itself should not contain spaces, but the parser
        // extracts everything after "Token " as the token value
        var result = AuthorizationHeaderParser.TryParse("Token abc 123", out var token);

        result.Should().BeTrue();
        token.Should().Be("abc 123");
    }

    [Fact]
    public void TryParse_LongToken_Succeeds()
    {
        var longToken = new string('A', 512);
        var result = AuthorizationHeaderParser.TryParse($"Token {longToken}", out var token);

        result.Should().BeTrue();
        token.Should().Be(longToken);
    }
}
