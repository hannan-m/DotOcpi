using DotOcpi.Simulator.Charging;
using DotOcpi.Simulator.State;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Simulator.Tests;

public class ChargingSimulationTests
{
    [Fact]
    public void Tick_AcFlat_DeliversConstantPower()
    {
        var session = CreateSession();
        var profile = ChargingProfileSpec.AcStandard; // 11 kW flat
        var tick = TimeSpan.FromMinutes(30);

        ChargingSimulation.Tick(session, profile, tick);

        // 11 kW * 0.5 hours = 5.5 kWh
        session.KwhDelivered.Should().BeApproximately(5.5m, 0.1m);
        session.TotalCostExclVat.Should().BeGreaterThan(0);
        session.ChargingPeriods.Should().HaveCount(1);
    }

    [Fact]
    public void Tick_DcWithTapering_ReducesPowerAfterThreshold()
    {
        var session = CreateSession();
        session.KwhDelivered = 50m; // Already at ~83% of 60 kWh target (past 80% taper point)
        var profile = ChargingProfileSpec.DcFast; // 50 kW, taper at 80%
        var tick = TimeSpan.FromMinutes(10);

        ChargingSimulation.Tick(session, profile, tick);

        // Power should be reduced (< 50 kW) due to tapering
        var energyAdded = session.KwhDelivered - 50m;
        var effectivePower = energyAdded / (decimal)tick.TotalHours;
        effectivePower.Should().BeLessThan(50m);
    }

    [Fact]
    public void Tick_ReturnsTrue_WhenTargetReached()
    {
        var session = CreateSession();
        session.KwhDelivered = 59.5m; // Almost at 60 kWh target
        var profile = ChargingProfileSpec.DcFast;
        var tick = TimeSpan.FromHours(1); // Way more than enough

        var reachedTarget = ChargingSimulation.Tick(session, profile, tick);

        reachedTarget.Should().BeTrue();
        session.KwhDelivered.Should().Be(60m);
    }

    [Fact]
    public void Tick_ReturnsFalse_WhenTargetNotReached()
    {
        var session = CreateSession();
        var profile = ChargingProfileSpec.DcFast;
        var tick = TimeSpan.FromMinutes(1);

        var reachedTarget = ChargingSimulation.Tick(session, profile, tick);

        reachedTarget.Should().BeFalse();
        session.KwhDelivered.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Tick_UpdatesCost()
    {
        var session = CreateSession();
        var profile = new ChargingProfileSpec
        {
            MaxPowerKw = 50m,
            TaperStartFraction = 1.0m,
            TargetKwh = 100m,
            PricePerKwh = 0.30m,
            VatRate = 0.20m,
        };
        var tick = TimeSpan.FromHours(1); // 50 kWh delivered

        ChargingSimulation.Tick(session, profile, tick);

        session.TotalCostExclVat.Should().Be(15.00m); // 50 * 0.30
        session.TotalCostInclVat.Should().Be(18.00m); // 15 * 1.20
    }

    [Fact]
    public void MultipleTicks_AccumulateEnergy()
    {
        var session = CreateSession();
        var profile = ChargingProfileSpec.AcStandard; // 11 kW
        var tick = TimeSpan.FromMinutes(10);

        ChargingSimulation.Tick(session, profile, tick);
        var afterFirst = session.KwhDelivered;

        ChargingSimulation.Tick(session, profile, tick);
        session.KwhDelivered.Should().BeGreaterThan(afterFirst);
    }

    private static SessionState CreateSession() =>
        new()
        {
            SessionId = "SES-001",
            LocationId = "LOC1",
            EvseUid = "EVSE001",
            ConnectorId = "1",
            TokenUid = "TOKEN001",
            TokenContractId = "CONTRACT001",
            StartTime = DateTimeOffset.UtcNow,
            LastUpdated = DateTimeOffset.UtcNow,
            ConnectionId = "default",
        };
}
