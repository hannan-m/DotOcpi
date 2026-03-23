using DotOcpi.Simulator.State;

namespace DotOcpi.Simulator.Charging;

/// <summary>
/// Advances a charging session by one tick. Models DC CC/CV curves
/// (full power then taper) and AC flat power delivery.
/// </summary>
internal static class ChargingSimulation
{
    /// <summary>
    /// Advances the session state by one tick interval.
    /// Updates kWh, cost, and charging periods.
    /// Returns true if the session should auto-stop (target kWh reached).
    /// </summary>
    public static bool Tick(SessionState session, ChargingProfileSpec profile, TimeSpan tickInterval)
    {
        var tickHours = (decimal)tickInterval.TotalHours;
        if (tickHours <= 0)
            return false;

        return session.WithLock(s =>
        {
            var currentPower = CalculateCurrentPower(s.KwhDelivered, profile);
            var energyThisTick = currentPower * tickHours;
            var newKwh = s.KwhDelivered + energyThisTick;

            // Clamp to target
            var reachedTarget = false;
            if (newKwh >= profile.TargetKwh)
            {
                energyThisTick = profile.TargetKwh - s.KwhDelivered;
                newKwh = profile.TargetKwh;
                reachedTarget = true;
            }

            s.KwhDelivered = newKwh;

            // Cost
            var costExcl = Math.Round(newKwh * profile.PricePerKwh, 2);
            var costIncl = Math.Round(costExcl * (1 + profile.VatRate), 2);
            s.TotalCostExclVat = costExcl;
            s.TotalCostInclVat = costIncl;

            // Charging period: extend current or start new one if power changed significantly
            var now = DateTimeOffset.UtcNow;
            if (s.ChargingPeriods.Count == 0)
            {
                s.ChargingPeriods.Add(
                    new ChargingPeriodState
                    {
                        StartTime = s.StartTime,
                        EnergyKwh = energyThisTick,
                        TimeHours = tickHours,
                        MaxPowerKw = currentPower,
                    }
                );
            }
            else
            {
                var current = s.ChargingPeriods[^1];
                var powerChanged =
                    current.MaxPowerKw.HasValue && Math.Abs(currentPower - current.MaxPowerKw.Value) > 1m;

                if (powerChanged)
                {
                    s.ChargingPeriods.Add(
                        new ChargingPeriodState
                        {
                            StartTime = now,
                            EnergyKwh = energyThisTick,
                            TimeHours = tickHours,
                            MaxPowerKw = currentPower,
                        }
                    );
                }
                else
                {
                    current.EnergyKwh += energyThisTick;
                    current.TimeHours += tickHours;
                    current.MaxPowerKw = currentPower;
                }
            }

            s.LastUpdated = now;
            return reachedTarget;
        });
    }

    /// <summary>
    /// Calculates current charging power based on the CC/CV or flat profile.
    /// </summary>
    private static decimal CalculateCurrentPower(decimal currentKwh, ChargingProfileSpec profile)
    {
        if (!profile.IsDcTapering || profile.TargetKwh <= 0)
            return profile.MaxPowerKw;

        var soc = currentKwh / profile.TargetKwh;

        // Constant current phase: full power until taper point
        if (soc < profile.TaperStartFraction)
            return profile.MaxPowerKw;

        // Constant voltage phase: linear ramp-down from max to taper final
        var taperProgress = (soc - profile.TaperStartFraction) / (1.0m - profile.TaperStartFraction);
        taperProgress = Math.Clamp(taperProgress, 0m, 1m);

        return profile.MaxPowerKw - (profile.MaxPowerKw - profile.TaperFinalPowerKw) * taperProgress;
    }
}
