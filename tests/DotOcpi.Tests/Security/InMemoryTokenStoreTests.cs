using DotOcpi.Security;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests.Security;

[Trait("Category", "Security")]
public class InMemoryTokenStoreTests
{
    private readonly InMemoryTokenStore _store = new();

    [Fact]
    public async Task StoreAsync_AndFindAsync_ReturnsEntry()
    {
        await _store.StoreAsync("hash1", TokenPurpose.TokenB, "NL:TNM");

        var entry = await _store.FindAsync("hash1");

        entry.Should().NotBeNull();
        entry!.TokenHash.Should().Be("hash1");
        entry.Purpose.Should().Be(TokenPurpose.TokenB);
        entry.PartyId.Should().Be("NL:TNM");
    }

    [Fact]
    public async Task FindAsync_UnknownHash_ReturnsNull()
    {
        var entry = await _store.FindAsync("unknown");

        entry.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_ExistingHash_ReturnsTrue()
    {
        await _store.StoreAsync("hash1", TokenPurpose.TokenA, "NL:TNM");

        var removed = await _store.RemoveAsync("hash1");

        removed.Should().BeTrue();
        var entry = await _store.FindAsync("hash1");
        entry.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_UnknownHash_ReturnsFalse()
    {
        var removed = await _store.RemoveAsync("unknown");

        removed.Should().BeFalse();
    }

    [Fact]
    public async Task StoreAsync_OverwritesExistingEntry()
    {
        await _store.StoreAsync("hash1", TokenPurpose.TokenA, "NL:TNM");
        await _store.StoreAsync("hash1", TokenPurpose.TokenB, "NL:TNM");

        var entry = await _store.FindAsync("hash1");

        entry.Should().NotBeNull();
        entry!.Purpose.Should().Be(TokenPurpose.TokenB);
    }

    [Fact]
    public async Task StoreAsync_MultipleDifferentHashes_StoredIndependently()
    {
        await _store.StoreAsync("hash1", TokenPurpose.TokenB, "NL:TNM");
        await _store.StoreAsync("hash2", TokenPurpose.TokenB, "DE:ALL");

        var entry1 = await _store.FindAsync("hash1");
        var entry2 = await _store.FindAsync("hash2");

        entry1!.PartyId.Should().Be("NL:TNM");
        entry2!.PartyId.Should().Be("DE:ALL");
    }

    [Fact]
    public async Task ConcurrentAccess_IsThreadSafe()
    {
        var tasks = Enumerable
            .Range(0, 100)
            .Select(i => _store.StoreAsync($"hash{i}", TokenPurpose.TokenB, $"party{i}").AsTask());

        await Task.WhenAll(tasks);

        for (var i = 0; i < 100; i++)
        {
            var entry = await _store.FindAsync($"hash{i}");
            entry.Should().NotBeNull();
            entry!.PartyId.Should().Be($"party{i}");
        }
    }

    [Fact]
    public async Task ConcurrentStoreAndRemove_IsThreadSafe()
    {
        // Pre-store entries
        for (var i = 0; i < 50; i++)
        {
            await _store.StoreAsync($"hash{i}", TokenPurpose.TokenB, $"party{i}");
        }

        // Concurrently store new entries and remove existing ones
        var storeTasks = Enumerable
            .Range(50, 50)
            .Select(i => _store.StoreAsync($"hash{i}", TokenPurpose.TokenB, $"party{i}").AsTask());
        var removeTasks = Enumerable.Range(0, 50).Select(i => _store.RemoveAsync($"hash{i}").AsTask());

        await Task.WhenAll(storeTasks.Concat(removeTasks));

        // Original entries should be removed
        for (var i = 0; i < 50; i++)
        {
            var entry = await _store.FindAsync($"hash{i}");
            entry.Should().BeNull();
        }

        // New entries should exist
        for (var i = 50; i < 100; i++)
        {
            var entry = await _store.FindAsync($"hash{i}");
            entry.Should().NotBeNull();
        }
    }

    // ── CPO token (outbound) storage ────────────────────────────────

    [Fact]
    public async Task StoreCpoToken_AndGet_ReturnsToken()
    {
        await _store.StoreCpoTokenAsync("DE:ALL", "protected-token");

        var token = await _store.GetCpoTokenAsync("DE:ALL");

        token.Should().Be("protected-token");
    }

    [Fact]
    public async Task GetCpoToken_UnknownCpo_ReturnsNull()
    {
        var token = await _store.GetCpoTokenAsync("UNKNOWN");

        token.Should().BeNull();
    }

    [Fact]
    public async Task StoreCpoToken_Overwrite_ReturnsLatest()
    {
        await _store.StoreCpoTokenAsync("DE:ALL", "token-v1");
        await _store.StoreCpoTokenAsync("DE:ALL", "token-v2");

        var token = await _store.GetCpoTokenAsync("DE:ALL");

        token.Should().Be("token-v2");
    }

    [Fact]
    public async Task RemoveCpoToken_Existing_ReturnsTrue()
    {
        await _store.StoreCpoTokenAsync("DE:ALL", "protected-token");

        var removed = await _store.RemoveCpoTokenAsync("DE:ALL");

        removed.Should().BeTrue();
        var token = await _store.GetCpoTokenAsync("DE:ALL");
        token.Should().BeNull();
    }

    [Fact]
    public async Task RemoveCpoToken_Unknown_ReturnsFalse()
    {
        var removed = await _store.RemoveCpoTokenAsync("UNKNOWN");

        removed.Should().BeFalse();
    }

    [Fact]
    public async Task CpoTokenLookup_IsCaseInsensitive()
    {
        await _store.StoreCpoTokenAsync("DE:ALL", "protected-token");

        var token = await _store.GetCpoTokenAsync("de:all");

        token.Should().Be("protected-token");
    }
}
