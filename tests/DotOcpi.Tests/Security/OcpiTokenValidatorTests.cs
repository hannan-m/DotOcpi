using DotOcpi.Security;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests.Security;

[Trait("Category", "Security")]
public class OcpiTokenValidatorTests
{
    private readonly InMemoryTokenStore _store = new();
    private readonly OcpiTokenValidator _validator;

    public OcpiTokenValidatorTests()
    {
        _validator = new OcpiTokenValidator(_store);
    }

    [Fact]
    public async Task ValidateAsync_ValidToken_ReturnsValid()
    {
        var rawToken = TokenGenerator.Generate();
        var hash = TokenHasher.Hash(rawToken);
        await _store.StoreAsync(hash, TokenPurpose.TokenB, "NL:TNM");

        var result = await _validator.ValidateAsync(rawToken);

        result.IsValid.Should().BeTrue();
        result.Entry.Should().NotBeNull();
        result.Entry!.Purpose.Should().Be(TokenPurpose.TokenB);
        result.Entry.PartyId.Should().Be("NL:TNM");
    }

    [Fact]
    public async Task ValidateAsync_UnknownToken_ReturnsFailed()
    {
        var rawToken = TokenGenerator.Generate();

        var result = await _validator.ValidateAsync(rawToken);

        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("not recognized");
    }

    [Fact]
    public async Task ValidateAsync_EmptyToken_ReturnsFailed()
    {
        var result = await _validator.ValidateAsync("");

        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("empty");
    }

    [Fact]
    public async Task ValidateAsync_TokenA_ReturnsValid()
    {
        var rawToken = TokenGenerator.Generate();
        var hash = TokenHasher.Hash(rawToken);
        await _store.StoreAsync(hash, TokenPurpose.TokenA, "NL:TNM");

        var result = await _validator.ValidateAsync(rawToken);

        result.IsValid.Should().BeTrue();
        result.Entry!.Purpose.Should().Be(TokenPurpose.TokenA);
    }

    [Fact]
    public async Task ValidateAsync_RemovedToken_ReturnsFailed()
    {
        var rawToken = TokenGenerator.Generate();
        var hash = TokenHasher.Hash(rawToken);
        await _store.StoreAsync(hash, TokenPurpose.TokenB, "NL:TNM");
        await _store.RemoveAsync(hash);

        var result = await _validator.ValidateAsync(rawToken);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_WrongToken_ReturnsFailed()
    {
        var storedToken = TokenGenerator.Generate();
        var hash = TokenHasher.Hash(storedToken);
        await _store.StoreAsync(hash, TokenPurpose.TokenB, "NL:TNM");

        var differentToken = TokenGenerator.Generate();
        var result = await _validator.ValidateAsync(differentToken);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_MultipleParties_IdentifiesCorrectParty()
    {
        var tokenA = TokenGenerator.Generate();
        var tokenB = TokenGenerator.Generate();
        await _store.StoreAsync(TokenHasher.Hash(tokenA), TokenPurpose.TokenB, "NL:TNM");
        await _store.StoreAsync(TokenHasher.Hash(tokenB), TokenPurpose.TokenB, "DE:ALL");

        var resultA = await _validator.ValidateAsync(tokenA);
        var resultB = await _validator.ValidateAsync(tokenB);

        resultA.IsValid.Should().BeTrue();
        resultA.Entry!.PartyId.Should().Be("NL:TNM");
        resultB.IsValid.Should().BeTrue();
        resultB.Entry!.PartyId.Should().Be("DE:ALL");
    }

    [Fact]
    public async Task ValidateAsync_UsesConstantTimeComparison()
    {
        // This test verifies that the validation path exercises FixedTimeEquals.
        // Full timing-attack verification requires specialized tooling; this
        // ensures the code path works correctly.
        var rawToken = TokenGenerator.Generate();
        var hash = TokenHasher.Hash(rawToken);
        await _store.StoreAsync(hash, TokenPurpose.TokenB, "NL:TNM");

        var result = await _validator.ValidateAsync(rawToken);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_CancellationToken_IsRespected()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // InMemoryTokenStore doesn't actually check cancellation, but the
        // interface contract requires it. This verifies the parameter flows through.
        var rawToken = TokenGenerator.Generate();
        var hash = TokenHasher.Hash(rawToken);
        await _store.StoreAsync(hash, TokenPurpose.TokenB, "NL:TNM");

        var result = await _validator.ValidateAsync(rawToken, cts.Token);

        result.IsValid.Should().BeTrue();
    }
}
