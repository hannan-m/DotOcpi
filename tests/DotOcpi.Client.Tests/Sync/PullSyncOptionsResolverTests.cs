using DotOcpi.Client.Sync;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Client.Tests.Sync;

public class PullSyncOptionsResolverTests
{
    [Fact]
    public void ResolveInterval_NoOverrides_ReturnsGlobalDefault()
    {
        var options = new PullSyncOptions { DefaultInterval = TimeSpan.FromHours(2) };

        var result = PullSyncOptionsResolver.ResolveInterval(options, "DE:ALL", "locations");

        result.Should().Be(TimeSpan.FromHours(2));
    }

    [Fact]
    public void ResolveInterval_ModuleOverride_ReturnsModuleInterval()
    {
        var options = new PullSyncOptions
        {
            DefaultInterval = TimeSpan.FromHours(1),
            ModuleOverrides = { ["locations"] = new() { Interval = TimeSpan.FromMinutes(30) } },
        };

        var result = PullSyncOptionsResolver.ResolveInterval(options, "DE:ALL", "locations");

        result.Should().Be(TimeSpan.FromMinutes(30));
    }

    [Fact]
    public void ResolveInterval_ModuleOverrideNullInterval_ReturnsGlobalDefault()
    {
        var options = new PullSyncOptions
        {
            DefaultInterval = TimeSpan.FromHours(1),
            ModuleOverrides = { ["locations"] = new() { Interval = null } },
        };

        var result = PullSyncOptionsResolver.ResolveInterval(options, "DE:ALL", "locations");

        result.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public void ResolveInterval_CpoDefaultOverride_TakesPriorityOverModuleOverride()
    {
        var options = new PullSyncOptions
        {
            DefaultInterval = TimeSpan.FromHours(1),
            ModuleOverrides = { ["locations"] = new() { Interval = TimeSpan.FromMinutes(30) } },
            CpoOverrides = { ["DE:ALL"] = new() { DefaultInterval = TimeSpan.FromMinutes(15) } },
        };

        var result = PullSyncOptionsResolver.ResolveInterval(options, "DE:ALL", "locations");

        result.Should().Be(TimeSpan.FromMinutes(15));
    }

    [Fact]
    public void ResolveInterval_CpoModuleOverride_TakesPriorityOverAll()
    {
        var options = new PullSyncOptions
        {
            DefaultInterval = TimeSpan.FromHours(1),
            ModuleOverrides = { ["locations"] = new() { Interval = TimeSpan.FromMinutes(30) } },
            CpoOverrides =
            {
                ["DE:ALL"] = new()
                {
                    DefaultInterval = TimeSpan.FromMinutes(15),
                    ModuleOverrides = { ["locations"] = new() { Interval = TimeSpan.FromMinutes(5) } },
                },
            },
        };

        var result = PullSyncOptionsResolver.ResolveInterval(options, "DE:ALL", "locations");

        result.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void ResolveInterval_CpoModuleOverrideNullInterval_FallsToCpoDefault()
    {
        var options = new PullSyncOptions
        {
            DefaultInterval = TimeSpan.FromHours(1),
            CpoOverrides =
            {
                ["DE:ALL"] = new()
                {
                    DefaultInterval = TimeSpan.FromMinutes(15),
                    ModuleOverrides = { ["locations"] = new() { Interval = null } },
                },
            },
        };

        var result = PullSyncOptionsResolver.ResolveInterval(options, "DE:ALL", "locations");

        result.Should().Be(TimeSpan.FromMinutes(15));
    }

    [Fact]
    public void ResolveInterval_DifferentCpo_GetsOwnOverride()
    {
        var options = new PullSyncOptions
        {
            DefaultInterval = TimeSpan.FromHours(1),
            CpoOverrides =
            {
                ["DE:ALL"] = new() { DefaultInterval = TimeSpan.FromMinutes(15) },
                ["NL:TNM"] = new() { DefaultInterval = TimeSpan.FromMinutes(45) },
            },
        };

        var deResult = PullSyncOptionsResolver.ResolveInterval(options, "DE:ALL", "locations");
        var nlResult = PullSyncOptionsResolver.ResolveInterval(options, "NL:TNM", "locations");
        var otherResult = PullSyncOptionsResolver.ResolveInterval(options, "FR:ION", "locations");

        deResult.Should().Be(TimeSpan.FromMinutes(15));
        nlResult.Should().Be(TimeSpan.FromMinutes(45));
        otherResult.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public void ResolveInterval_DifferentModules_GetOwnOverrides()
    {
        var options = new PullSyncOptions
        {
            DefaultInterval = TimeSpan.FromHours(1),
            ModuleOverrides =
            {
                ["locations"] = new() { Interval = TimeSpan.FromMinutes(30) },
                ["cdrs"] = new() { Interval = TimeSpan.FromHours(4) },
            },
        };

        var locResult = PullSyncOptionsResolver.ResolveInterval(options, "DE:ALL", "locations");
        var cdrResult = PullSyncOptionsResolver.ResolveInterval(options, "DE:ALL", "cdrs");
        var sessionResult = PullSyncOptionsResolver.ResolveInterval(options, "DE:ALL", "sessions");

        locResult.Should().Be(TimeSpan.FromMinutes(30));
        cdrResult.Should().Be(TimeSpan.FromHours(4));
        sessionResult.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public void ResolveEnabledModules_NoOverride_ReturnsGlobalModules()
    {
        var options = new PullSyncOptions
        {
            EnabledModules = ["locations", "tariffs"],
        };

        var result = PullSyncOptionsResolver.ResolveEnabledModules(options, "DE:ALL");

        result.Should().BeEquivalentTo(["locations", "tariffs"]);
    }

    [Fact]
    public void ResolveEnabledModules_CpoOverride_ReturnsCpoModules()
    {
        var options = new PullSyncOptions
        {
            EnabledModules = ["locations", "tariffs"],
            CpoOverrides =
            {
                ["DE:ALL"] = new() { EnabledModules = ["locations", "sessions", "cdrs", "tariffs"] },
            },
        };

        var deResult = PullSyncOptionsResolver.ResolveEnabledModules(options, "DE:ALL");
        var otherResult = PullSyncOptionsResolver.ResolveEnabledModules(options, "NL:TNM");

        deResult.Should().BeEquivalentTo(["locations", "sessions", "cdrs", "tariffs"]);
        otherResult.Should().BeEquivalentTo(["locations", "tariffs"]);
    }

    [Fact]
    public void ResolveEnabledModules_CpoOverrideNull_ReturnsGlobalModules()
    {
        var options = new PullSyncOptions
        {
            EnabledModules = ["locations", "tariffs"],
            CpoOverrides = { ["DE:ALL"] = new() { EnabledModules = null } },
        };

        var result = PullSyncOptionsResolver.ResolveEnabledModules(options, "DE:ALL");

        result.Should().BeEquivalentTo(["locations", "tariffs"]);
    }
}
