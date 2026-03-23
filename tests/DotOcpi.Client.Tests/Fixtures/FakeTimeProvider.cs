namespace DotOcpi.Client.Tests.Fixtures;

/// <summary>
/// Deterministic <see cref="TimeProvider"/> for tests that depend on time progression.
/// Call <see cref="Advance"/> to move the clock forward without real delays.
/// </summary>
internal sealed class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    internal FakeTimeProvider(DateTimeOffset startTime)
    {
        _utcNow = startTime;
    }

    internal FakeTimeProvider()
        : this(new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero)) { }

    public override DateTimeOffset GetUtcNow() => _utcNow;

    internal void Advance(TimeSpan duration) => _utcNow += duration;
}
