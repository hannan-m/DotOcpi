using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests.Configuration;

public class DotOcpiOptionsTests
{
    [Fact]
    public void Defaults_AreValid()
    {
        var options = new DotOcpiOptions();

        var errors = options.Validate();

        errors.Should().BeEmpty();
    }

    [Fact]
    public void DefaultSupportedVersions_ContainsV2_2_1()
    {
        var options = new DotOcpiOptions();

        options.SupportedVersions.Should().ContainSingle().Which.Should().Be(OcpiVersion.V2_2_1);
    }

    [Fact]
    public void EmptyVersions_ReturnsError()
    {
        var options = new DotOcpiOptions { SupportedVersions = [] };

        var errors = options.Validate();

        errors.Should().ContainSingle().Which.Should().Contain("version");
    }

    [Fact]
    public void HttpBaseUrl_ReturnsError()
    {
        var options = new DotOcpiOptions { BaseUrl = new Uri("http://insecure.example.com/ocpi") };

        var errors = options.Validate();

        errors.Should().ContainSingle().Which.Should().Contain("HTTPS");
    }

    [Fact]
    public void HttpsBaseUrl_NoError()
    {
        var options = new DotOcpiOptions { BaseUrl = new Uri("https://secure.example.com/ocpi") };

        var errors = options.Validate();

        errors.Should().BeEmpty();
    }

    [Fact]
    public void NullBaseUrl_NoError()
    {
        var options = new DotOcpiOptions { BaseUrl = null };

        var errors = options.Validate();

        errors.Should().BeEmpty();
    }

    [Fact]
    public void ZeroHealthInterval_ReturnsError()
    {
        var options = new DotOcpiOptions { HealthMonitoringInterval = TimeSpan.Zero };

        var errors = options.Validate();

        errors.Should().ContainSingle().Which.Should().Contain("HealthMonitoringInterval");
    }

    [Fact]
    public void NegativeStaleThreshold_ReturnsError()
    {
        var options = new DotOcpiOptions { StaleConnectionThreshold = TimeSpan.FromSeconds(-1) };

        var errors = options.Validate();

        errors.Should().ContainSingle().Which.Should().Contain("StaleConnectionThreshold");
    }

    [Fact]
    public void MultipleErrors_ReturnsAll()
    {
        var options = new DotOcpiOptions
        {
            SupportedVersions = [],
            BaseUrl = new Uri("http://bad.example.com"),
            HealthMonitoringInterval = TimeSpan.Zero,
        };

        var errors = options.Validate();

        errors.Should().HaveCount(3);
    }

    [Fact]
    public void DefaultLogging_HasSecureDefaults()
    {
        var options = new DotOcpiOptions();

        options.Logging.EnableRequestBodyLogging.Should().BeFalse();
        options.Logging.EnableResponseBodyLogging.Should().BeFalse();
        options.Logging.SanitizeTokensInLogs.Should().BeTrue();
        options.Logging.MaxBodyLogLength.Should().Be(4096);
    }

    [Fact]
    public void DefaultHealthMonitoring_IsEnabled()
    {
        var options = new DotOcpiOptions();

        options.EnableHealthMonitoring.Should().BeTrue();
        options.HealthMonitoringInterval.Should().Be(TimeSpan.FromMinutes(5));
        options.StaleConnectionThreshold.Should().Be(TimeSpan.FromHours(24));
    }
}
