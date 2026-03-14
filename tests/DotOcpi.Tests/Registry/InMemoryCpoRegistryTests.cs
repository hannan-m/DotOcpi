using DotOcpi.Registry;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests.Registry;

public class InMemoryCpoRegistryTests
{
    private readonly InMemoryCpoRegistry _registry = new();

    private static CpoConnection CreateConnection(
        string cpoCountryCode = "DE",
        string cpoPartyId = "ALL",
        string emspCountryCode = "NL",
        string emspPartyId = "TNM",
        string tokenBHash = "hash1",
        long concurrencyVersion = 0
    ) =>
        new()
        {
            CpoCountryCode = cpoCountryCode,
            CpoPartyId = cpoPartyId,
            EmspCountryCode = emspCountryCode,
            EmspPartyId = emspPartyId,
            Version = OcpiVersion.V2_2_1,
            ModuleEndpoints = new Dictionary<string, string>
            {
                ["locations"] = "https://cpo.example.com/ocpi/2.2.1/locations",
            },
            TokenBHash = tokenBHash,
            Status = ConnectionStatus.Connected,
            CreatedAt = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero),
            ConcurrencyVersion = concurrencyVersion,
        };

    [Fact]
    public void AddOrUpdate_NewConnection_Succeeds()
    {
        var connection = CreateConnection();

        var result = _registry.AddOrUpdate(connection);

        result.Should().BeTrue();
    }

    [Fact]
    public void FindByConnectionKey_AfterAdd_ReturnsConnection()
    {
        _registry.AddOrUpdate(CreateConnection());

        var found = _registry.FindByConnectionKey("DE:ALL");

        found.Should().NotBeNull();
        found!.CpoCountryCode.Should().Be("DE");
        found.CpoPartyId.Should().Be("ALL");
    }

    [Fact]
    public void FindByConnectionKey_CaseInsensitive()
    {
        _registry.AddOrUpdate(CreateConnection());

        var found = _registry.FindByConnectionKey("de:all");

        found.Should().NotBeNull();
    }

    [Fact]
    public void FindByConnectionKey_Unknown_ReturnsNull()
    {
        var found = _registry.FindByConnectionKey("XX:YYY");

        found.Should().BeNull();
    }

    [Fact]
    public void FindByTokenHash_AfterAdd_ReturnsConnection()
    {
        _registry.AddOrUpdate(CreateConnection(tokenBHash: "myhash"));

        var found = _registry.FindByTokenHash("myhash");

        found.Should().NotBeNull();
        found!.TokenBHash.Should().Be("myhash");
    }

    [Fact]
    public void FindByTokenHash_Unknown_ReturnsNull()
    {
        var found = _registry.FindByTokenHash("unknown");

        found.Should().BeNull();
    }

    [Fact]
    public void FindByEmspIdentity_AfterAdd_ReturnsConnections()
    {
        _registry.AddOrUpdate(CreateConnection(cpoCountryCode: "DE", cpoPartyId: "ALL"));
        _registry.AddOrUpdate(CreateConnection(cpoCountryCode: "FR", cpoPartyId: "EDF", tokenBHash: "hash2"));

        var connections = _registry.FindByEmspIdentity("NL", "TNM");

        connections.Should().HaveCount(2);
    }

    [Fact]
    public void FindByEmspIdentity_DifferentEmsp_ReturnsEmpty()
    {
        _registry.AddOrUpdate(CreateConnection());

        var connections = _registry.FindByEmspIdentity("US", "XYZ");

        connections.Should().BeEmpty();
    }

    [Fact]
    public void GetAll_ReturnsAllConnections()
    {
        _registry.AddOrUpdate(CreateConnection(cpoCountryCode: "DE", cpoPartyId: "ALL"));
        _registry.AddOrUpdate(CreateConnection(cpoCountryCode: "FR", cpoPartyId: "EDF", tokenBHash: "hash2"));

        var all = _registry.GetAll();

        all.Should().HaveCount(2);
    }

    [Fact]
    public void AddOrUpdate_ExistingConnection_MatchingVersion_Succeeds()
    {
        _registry.AddOrUpdate(CreateConnection());

        // After AddOrUpdate, the stored version is incremented to 1
        var updated = CreateConnection(tokenBHash: "newhash") with
        {
            Status = ConnectionStatus.Offline,
            ConcurrencyVersion = 1,
        };
        var result = _registry.AddOrUpdate(updated);

        result.Should().BeTrue();
        var found = _registry.FindByConnectionKey("DE:ALL");
        found!.Status.Should().Be(ConnectionStatus.Offline);
    }

    [Fact]
    public void AddOrUpdate_ExistingConnection_StaleVersion_Fails()
    {
        _registry.AddOrUpdate(CreateConnection());

        // Try to update with stale version (0, but stored is 1)
        var stale = CreateConnection(tokenBHash: "newhash") with
        {
            Status = ConnectionStatus.Offline,
            ConcurrencyVersion = 0,
        };
        var result = _registry.AddOrUpdate(stale);

        result.Should().BeFalse();
        var found = _registry.FindByConnectionKey("DE:ALL");
        found!.Status.Should().Be(ConnectionStatus.Connected);
    }

    [Fact]
    public void AddOrUpdate_UpdatesTokenHashIndex()
    {
        _registry.AddOrUpdate(CreateConnection(tokenBHash: "oldhash"));

        var updated = CreateConnection(tokenBHash: "newhash") with { ConcurrencyVersion = 1 };
        _registry.AddOrUpdate(updated);

        _registry.FindByTokenHash("oldhash").Should().BeNull();
        _registry.FindByTokenHash("newhash").Should().NotBeNull();
    }

    [Fact]
    public void AddOrUpdate_IncrementsConcurrencyVersion()
    {
        _registry.AddOrUpdate(CreateConnection());

        var found = _registry.FindByConnectionKey("DE:ALL");
        found!.ConcurrencyVersion.Should().Be(1);
    }

    [Fact]
    public void Remove_ExistingConnection_ReturnsTrue()
    {
        _registry.AddOrUpdate(CreateConnection());

        var result = _registry.Remove("DE:ALL");

        result.Should().BeTrue();
        _registry.FindByConnectionKey("DE:ALL").Should().BeNull();
    }

    [Fact]
    public void Remove_CleansUpAllIndexes()
    {
        _registry.AddOrUpdate(CreateConnection(tokenBHash: "myhash"));

        _registry.Remove("DE:ALL");

        _registry.FindByTokenHash("myhash").Should().BeNull();
        _registry.FindByEmspIdentity("NL", "TNM").Should().BeEmpty();
    }

    [Fact]
    public void Remove_Unknown_ReturnsFalse()
    {
        var result = _registry.Remove("XX:YYY");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ConcurrentAddOrUpdate_IsThreadSafe()
    {
        var tasks = Enumerable
            .Range(0, 100)
            .Select(i =>
                Task.Run(() =>
                    _registry.AddOrUpdate(
                        CreateConnection(cpoCountryCode: "DE", cpoPartyId: $"P{i:D3}", tokenBHash: $"hash{i}")
                    )
                )
            );

        await Task.WhenAll(tasks);

        _registry.GetAll().Should().HaveCount(100);
    }
}
