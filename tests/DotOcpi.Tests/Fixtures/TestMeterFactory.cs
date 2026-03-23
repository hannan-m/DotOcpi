using System.Diagnostics.Metrics;

namespace DotOcpi.Tests.Fixtures;

/// <summary>
/// Lightweight <see cref="IMeterFactory"/> for tests that need an <see cref="OcpiMetrics"/> instance.
/// Tracks created meters and disposes them on cleanup.
/// </summary>
internal sealed class TestMeterFactory : IMeterFactory
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
