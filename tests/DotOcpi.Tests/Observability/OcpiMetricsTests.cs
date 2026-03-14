using System.Diagnostics.Metrics;
using DotOcpi.Observability;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Xunit;

namespace DotOcpi.Tests.Observability;

public sealed class OcpiMetricsTests : IDisposable
{
    private readonly TestMeterFactory _meterFactory;
    private readonly OcpiMetrics _metrics;

    public OcpiMetricsTests()
    {
        _meterFactory = new TestMeterFactory();
        _metrics = new OcpiMetrics(_meterFactory);
    }

    public void Dispose() => _meterFactory.Dispose();

    [Fact]
    public void RecordRequest_IncrementsCounter()
    {
        var collector = new MetricCollector<long>(_meterFactory, OcpiMetrics.MeterName, "dotocpi.requests.total");

        _metrics.RecordRequest("inbound", "locations", "2.2.1", 1000);

        var measurements = collector.GetMeasurementSnapshot();
        measurements.Should().HaveCount(1);
        measurements[0].Value.Should().Be(1);
        measurements[0].Tags["direction"].Should().Be("inbound");
        measurements[0].Tags["module"].Should().Be("locations");
        measurements[0].Tags["version"].Should().Be("2.2.1");
        measurements[0].Tags["status"].Should().Be(1000);
    }

    [Fact]
    public void RecordRequest_MultipleCalls_Accumulates()
    {
        var collector = new MetricCollector<long>(_meterFactory, OcpiMetrics.MeterName, "dotocpi.requests.total");

        _metrics.RecordRequest("outbound", "sessions", "2.1.1", 1000);
        _metrics.RecordRequest("outbound", "sessions", "2.1.1", 2001);

        collector.GetMeasurementSnapshot().Should().HaveCount(2);
    }

    [Fact]
    public void RecordRequestDuration_RecordsHistogram()
    {
        var collector = new MetricCollector<double>(_meterFactory, OcpiMetrics.MeterName, "dotocpi.request.duration");

        _metrics.RecordRequestDuration(0.150, "outbound", "locations", "2.2.1");

        var measurements = collector.GetMeasurementSnapshot();
        measurements.Should().HaveCount(1);
        measurements[0].Value.Should().BeApproximately(0.150, 0.001);
        measurements[0].Tags["direction"].Should().Be("outbound");
        measurements[0].Tags["module"].Should().Be("locations");
        measurements[0].Tags["version"].Should().Be("2.2.1");
    }

    [Fact]
    public void ConnectionActivated_IncrementsGauge()
    {
        var collector = new MetricCollector<long>(_meterFactory, OcpiMetrics.MeterName, "dotocpi.connections.active");

        _metrics.ConnectionActivated("connected");

        var measurements = collector.GetMeasurementSnapshot();
        measurements.Should().HaveCount(1);
        measurements[0].Value.Should().Be(1);
        measurements[0].Tags["status"].Should().Be("connected");
    }

    [Fact]
    public void ConnectionDeactivated_DecrementsGauge()
    {
        var collector = new MetricCollector<long>(_meterFactory, OcpiMetrics.MeterName, "dotocpi.connections.active");

        _metrics.ConnectionActivated("connected");
        _metrics.ConnectionDeactivated("connected");

        var measurements = collector.GetMeasurementSnapshot();
        measurements.Should().HaveCount(2);
        measurements[0].Value.Should().Be(1);
        measurements[1].Value.Should().Be(-1);
    }

    [Fact]
    public void RecordAuthFailure_IncrementsCounter()
    {
        var collector = new MetricCollector<long>(_meterFactory, OcpiMetrics.MeterName, "dotocpi.auth.failures");

        _metrics.RecordAuthFailure("invalid_token");

        var measurements = collector.GetMeasurementSnapshot();
        measurements.Should().HaveCount(1);
        measurements[0].Value.Should().Be(1);
        measurements[0].Tags["reason"].Should().Be("invalid_token");
    }

    [Fact]
    public void MeterName_IsCorrect()
    {
        OcpiMetrics.MeterName.Should().Be("DotOcpi");
    }

    private sealed class TestMeterFactory : IMeterFactory
    {
        private readonly List<Meter> _meters = [];

        public Meter Create(MeterOptions options)
        {
            var meter = new Meter(options);
            _meters.Add(meter);
            return meter;
        }

        public void Dispose()
        {
            foreach (var meter in _meters)
            {
                meter.Dispose();
            }
        }
    }
}
