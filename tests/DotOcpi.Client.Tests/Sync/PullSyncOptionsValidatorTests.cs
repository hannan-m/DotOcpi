using DotOcpi.Client.Sync;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Client.Tests.Sync;

public class PullSyncOptionsValidatorTests
{
    private readonly PullSyncOptionsValidator _validator = new();

    [Fact]
    public void Validate_DefaultOptions_Succeeds()
    {
        var result = _validator.Validate(null, new PullSyncOptions());

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_ZeroDefaultInterval_Fails()
    {
        var options = new PullSyncOptions { DefaultInterval = TimeSpan.Zero };

        var result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("DefaultInterval");
    }

    [Fact]
    public void Validate_NegativeDefaultInterval_Fails()
    {
        var options = new PullSyncOptions { DefaultInterval = TimeSpan.FromMinutes(-1) };

        var result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_NegativeMaxJitter_Fails()
    {
        var options = new PullSyncOptions { MaxJitter = TimeSpan.FromSeconds(-1) };

        var result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("MaxJitter");
    }

    [Fact]
    public void Validate_ZeroMaxJitter_Succeeds()
    {
        var options = new PullSyncOptions { MaxJitter = TimeSpan.Zero };

        var result = _validator.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_InvalidModuleInEnabledModules_Fails()
    {
        var options = new PullSyncOptions { EnabledModules = ["locations", "commands"] };

        var result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("commands");
    }

    [Fact]
    public void Validate_AllValidModules_Succeeds()
    {
        var options = new PullSyncOptions
        {
            EnabledModules = ["locations", "sessions", "cdrs", "tariffs"],
        };

        var result = _validator.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_InvalidModuleOverrideKey_Fails()
    {
        var options = new PullSyncOptions
        {
            ModuleOverrides = { ["invalid"] = new() { Interval = TimeSpan.FromHours(1) } },
        };

        var result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("invalid");
    }

    [Fact]
    public void Validate_ZeroModuleOverrideInterval_Fails()
    {
        var options = new PullSyncOptions
        {
            ModuleOverrides = { ["locations"] = new() { Interval = TimeSpan.Zero } },
        };

        var result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("locations");
    }

    [Fact]
    public void Validate_ZeroCpoDefaultInterval_Fails()
    {
        var options = new PullSyncOptions
        {
            CpoOverrides = { ["DE:ALL"] = new() { DefaultInterval = TimeSpan.Zero } },
        };

        var result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("DE:ALL");
    }

    [Fact]
    public void Validate_InvalidModuleInCpoEnabledModules_Fails()
    {
        var options = new PullSyncOptions
        {
            CpoOverrides = { ["DE:ALL"] = new() { EnabledModules = ["tokens"] } },
        };

        var result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("tokens");
    }

    [Fact]
    public void Validate_InvalidCpoModuleOverrideKey_Fails()
    {
        var options = new PullSyncOptions
        {
            CpoOverrides =
            {
                ["DE:ALL"] = new()
                {
                    ModuleOverrides = { ["bad"] = new() { Interval = TimeSpan.FromHours(1) } },
                },
            },
        };

        var result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("bad");
    }

    [Fact]
    public void Validate_ValidComplexConfig_Succeeds()
    {
        var options = new PullSyncOptions
        {
            DefaultInterval = TimeSpan.FromHours(1),
            MaxJitter = TimeSpan.FromMinutes(5),
            EnabledModules = ["locations", "tariffs"],
            ModuleOverrides = { ["locations"] = new() { Interval = TimeSpan.FromMinutes(30) } },
            CpoOverrides =
            {
                ["DE:ALL"] = new()
                {
                    DefaultInterval = TimeSpan.FromMinutes(15),
                    EnabledModules = ["locations", "sessions"],
                    ModuleOverrides = { ["sessions"] = new() { Interval = TimeSpan.FromHours(2) } },
                },
            },
        };

        var result = _validator.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }
}
