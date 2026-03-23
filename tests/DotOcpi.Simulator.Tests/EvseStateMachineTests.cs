using DotOcpi.Simulator.Charging;
using DotOcpi.Simulator.State;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Simulator.Tests;

public class EvseStateMachineTests
{
    [Fact]
    public void Available_StartSession_TransitionsToCharging()
    {
        var (newStatus, allowed) = EvseStateMachine.Transition(EvseStatus.Available, EvseEvent.StartSession);
        allowed.Should().BeTrue();
        newStatus.Should().Be(EvseStatus.Charging);
    }

    [Fact]
    public void Available_ReserveNow_TransitionsToReserved()
    {
        var (newStatus, allowed) = EvseStateMachine.Transition(EvseStatus.Available, EvseEvent.ReserveNow);
        allowed.Should().BeTrue();
        newStatus.Should().Be(EvseStatus.Reserved);
    }

    [Fact]
    public void Reserved_StartSession_TransitionsToCharging()
    {
        var (newStatus, allowed) = EvseStateMachine.Transition(EvseStatus.Reserved, EvseEvent.StartSession);
        allowed.Should().BeTrue();
        newStatus.Should().Be(EvseStatus.Charging);
    }

    [Fact]
    public void Reserved_CancelReservation_TransitionsToAvailable()
    {
        var (newStatus, allowed) = EvseStateMachine.Transition(EvseStatus.Reserved, EvseEvent.CancelReservation);
        allowed.Should().BeTrue();
        newStatus.Should().Be(EvseStatus.Available);
    }

    [Fact]
    public void Reserved_ReservationExpired_TransitionsToAvailable()
    {
        var (newStatus, allowed) = EvseStateMachine.Transition(EvseStatus.Reserved, EvseEvent.ReservationExpired);
        allowed.Should().BeTrue();
        newStatus.Should().Be(EvseStatus.Available);
    }

    [Fact]
    public void Charging_StopSession_TransitionsToAvailable()
    {
        var (newStatus, allowed) = EvseStateMachine.Transition(EvseStatus.Charging, EvseEvent.StopSession);
        allowed.Should().BeTrue();
        newStatus.Should().Be(EvseStatus.Available);
    }

    [Fact]
    public void Charging_FaultDetected_TransitionsToInoperative()
    {
        var (newStatus, allowed) = EvseStateMachine.Transition(EvseStatus.Charging, EvseEvent.FaultDetected);
        allowed.Should().BeTrue();
        newStatus.Should().Be(EvseStatus.Inoperative);
    }

    [Fact]
    public void Inoperative_FaultCleared_TransitionsToAvailable()
    {
        var (newStatus, allowed) = EvseStateMachine.Transition(EvseStatus.Inoperative, EvseEvent.FaultCleared);
        allowed.Should().BeTrue();
        newStatus.Should().Be(EvseStatus.Available);
    }

    [Fact]
    public void Charging_ReserveNow_NotAllowed()
    {
        var (newStatus, allowed) = EvseStateMachine.Transition(EvseStatus.Charging, EvseEvent.ReserveNow);
        allowed.Should().BeFalse();
        newStatus.Should().Be(EvseStatus.Charging);
    }

    [Fact]
    public void Available_StopSession_NotAllowed()
    {
        var (newStatus, allowed) = EvseStateMachine.Transition(EvseStatus.Available, EvseEvent.StopSession);
        allowed.Should().BeFalse();
        newStatus.Should().Be(EvseStatus.Available);
    }
}
