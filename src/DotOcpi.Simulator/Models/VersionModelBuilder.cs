using DotOcpi.Simulator.Charging;
using DotOcpi.Simulator.State;
using V2_0 = DotOcpi.Models.V2_0;
using V2_1_1 = DotOcpi.Models.V2_1_1;
using V2_2 = DotOcpi.Models.V2_2;
using V2_2_1 = DotOcpi.Models.V2_2_1;

namespace DotOcpi.Simulator.Models;

/// <summary>
/// Builds version-specific OCPI model instances from version-neutral spec/state types.
/// This is the core version-adaptation layer: internal state is stored once,
/// and the correct model shape is constructed at serialization time.
/// </summary>
internal static class VersionModelBuilder
{
    // ── Location ──────────────────────────────────────────────

    public static object BuildLocation(
        OcpiVersion version,
        LocationSpec spec,
        PartyIdentity cpoIdentity,
        IReadOnlyDictionary<string, EvseState>? evseStates = null
    )
    {
        var now = DateTimeOffset.UtcNow;
        return version switch
        {
            OcpiVersion.V2_2_1 => new V2_2_1.Location
            {
                CountryCode = cpoIdentity.CountryCode,
                PartyId = cpoIdentity.PartyId,
                Id = spec.Id,
                Publish = true,
                Name = spec.Name,
                Address = spec.Address,
                City = spec.City,
                PostalCode = spec.PostalCode,
                Country = spec.Country,
                Coordinates = new GeoLocation(spec.Latitude, spec.Longitude),
                TimeZone = spec.TimeZone,
                Evses = spec.Evses.Select(e => BuildEvse_V2_2_1(e, evseStates, spec.Id, now)).ToList(),
                LastUpdated = now,
            },
            OcpiVersion.V2_2 => new V2_2.Location
            {
                CountryCode = cpoIdentity.CountryCode,
                PartyId = cpoIdentity.PartyId,
                Id = spec.Id,
                Publish = true,
                Name = spec.Name,
                Address = spec.Address,
                City = spec.City,
                PostalCode = spec.PostalCode,
                Country = spec.Country,
                Coordinates = new GeoLocation(spec.Latitude, spec.Longitude),
                TimeZone = spec.TimeZone,
                Evses = spec.Evses.Select(e => BuildEvse_V2_2(e, evseStates, spec.Id, now)).ToList(),
                LastUpdated = now,
            },
            OcpiVersion.V2_1_1 => new V2_1_1.Location
            {
                Id = spec.Id,
                Name = spec.Name,
                Address = spec.Address,
                City = spec.City,
                PostalCode = spec.PostalCode,
                Country = spec.Country,
                Coordinates = new GeoLocation(spec.Latitude, spec.Longitude),
                TimeZone = spec.TimeZone,
                Evses = spec.Evses.Select(e => BuildEvse_V2_1_1(e, evseStates, spec.Id, now)).ToList(),
                LastUpdated = now,
            },
            OcpiVersion.V2_0 => new V2_0.Location
            {
                Id = spec.Id,
                Name = spec.Name,
                Address = spec.Address,
                City = spec.City,
                PostalCode = spec.PostalCode,
                Country = spec.Country,
                Coordinates = new GeoLocation(spec.Latitude, spec.Longitude),
                Evses = spec.Evses.Select(e => BuildEvse_V2_0(e, evseStates, spec.Id, now)).ToList(),
            },
            _ => throw new ArgumentOutOfRangeException(nameof(version)),
        };
    }

    // ── EVSE (public entry point) ───────────────────────────────

    public static object BuildEvse(
        OcpiVersion version,
        EvseSpec spec,
        IReadOnlyDictionary<string, EvseState>? evseStates,
        string locationId
    )
    {
        var now = DateTimeOffset.UtcNow;
        return version switch
        {
            OcpiVersion.V2_2_1 => BuildEvse_V2_2_1(spec, evseStates, locationId, now),
            OcpiVersion.V2_2 => BuildEvse_V2_2(spec, evseStates, locationId, now),
            OcpiVersion.V2_1_1 => BuildEvse_V2_1_1(spec, evseStates, locationId, now),
            OcpiVersion.V2_0 => BuildEvse_V2_0(spec, evseStates, locationId, now),
            _ => throw new ArgumentOutOfRangeException(nameof(version)),
        };
    }

    public static object BuildConnector(OcpiVersion version, ConnectorProfile connector)
    {
        var now = DateTimeOffset.UtcNow;
        return version switch
        {
            OcpiVersion.V2_2_1 => BuildConnector_V2_2_1(connector, now),
            OcpiVersion.V2_2 => BuildConnector_V2_2(connector, now),
            OcpiVersion.V2_1_1 => BuildConnector_V2_1_1(connector, now),
            OcpiVersion.V2_0 => BuildConnector_V2_0(connector, now),
            _ => throw new ArgumentOutOfRangeException(nameof(version)),
        };
    }

    // ── EVSE (per-version) ────────────────────────────────────

    private static V2_2_1.Evse BuildEvse_V2_2_1(
        EvseSpec spec,
        IReadOnlyDictionary<string, EvseState>? states,
        string locationId,
        DateTimeOffset now
    )
    {
        var status = ResolveEvseStatus(states, locationId, spec.Uid);
        return new V2_2_1.Evse
        {
            Uid = spec.Uid,
            EvseId = spec.EvseId is not null ? new CiString(spec.EvseId) : (CiString?)null,
            Status = MapStatus_V2_2_1(status),
            Connectors = [BuildConnector_V2_2_1(spec.Connector, now)],
            Capabilities = spec.Capabilities.Select(c => Enum.Parse<V2_2_1.Capability>(c)).ToList(),
            LastUpdated = now,
        };
    }

    private static V2_2.Evse BuildEvse_V2_2(
        EvseSpec spec,
        IReadOnlyDictionary<string, EvseState>? states,
        string locationId,
        DateTimeOffset now
    )
    {
        var status = ResolveEvseStatus(states, locationId, spec.Uid);
        return new V2_2.Evse
        {
            Uid = spec.Uid,
            EvseId = spec.EvseId is not null ? new CiString(spec.EvseId) : (CiString?)null,
            Status = MapStatus_V2_2(status),
            Connectors = [BuildConnector_V2_2(spec.Connector, now)],
            Capabilities = spec.Capabilities.Select(c => Enum.Parse<V2_2.Capability>(c)).ToList(),
            LastUpdated = now,
        };
    }

    private static V2_1_1.Evse BuildEvse_V2_1_1(
        EvseSpec spec,
        IReadOnlyDictionary<string, EvseState>? states,
        string locationId,
        DateTimeOffset now
    )
    {
        var status = ResolveEvseStatus(states, locationId, spec.Uid);
        return new V2_1_1.Evse
        {
            Uid = spec.Uid,
            EvseId = spec.EvseId,
            Status = MapStatus_V2_1_1(status),
            Connectors = [BuildConnector_V2_1_1(spec.Connector, now)],
            Capabilities = spec.Capabilities.Select(c => Enum.Parse<V2_1_1.Capability>(c)).ToList(),
            LastUpdated = now,
        };
    }

    private static V2_0.Evse BuildEvse_V2_0(
        EvseSpec spec,
        IReadOnlyDictionary<string, EvseState>? states,
        string locationId,
        DateTimeOffset now
    )
    {
        var status = ResolveEvseStatus(states, locationId, spec.Uid);
        return new V2_0.Evse
        {
            Uid = spec.Uid,
            EvseId = spec.EvseId,
            Status = MapStatus_V2_0(status),
            Connectors = [BuildConnector_V2_0(spec.Connector, now)],
        };
    }

    // ── Connector (per-version) ───────────────────────────────

    private static V2_2_1.Connector BuildConnector_V2_2_1(ConnectorProfile c, DateTimeOffset now) =>
        new()
        {
            Id = c.Id,
            Standard = Enum.Parse<V2_2_1.ConnectorType>(c.Standard),
            Format = Enum.Parse<V2_2_1.ConnectorFormat>(c.Format),
            PowerType = Enum.Parse<V2_2_1.PowerType>(c.PowerType),
            MaxVoltage = c.MaxVoltage,
            MaxAmperage = c.MaxAmperage,
            MaxElectricPower = (int)(c.MaxPowerKw * 1000),
            TariffIds = c.TariffId is not null ? [(CiString)c.TariffId] : null,
            LastUpdated = now,
        };

    private static V2_2.Connector BuildConnector_V2_2(ConnectorProfile c, DateTimeOffset now) =>
        new()
        {
            Id = c.Id,
            Standard = Enum.Parse<V2_2.ConnectorType>(c.Standard),
            Format = Enum.Parse<V2_2.ConnectorFormat>(c.Format),
            PowerType = Enum.Parse<V2_2.PowerType>(c.PowerType),
            MaxVoltage = c.MaxVoltage,
            MaxAmperage = c.MaxAmperage,
            MaxElectricPower = (int)(c.MaxPowerKw * 1000),
            TariffIds = c.TariffId is not null ? [(CiString)c.TariffId] : null,
            LastUpdated = now,
        };

    private static V2_1_1.Connector BuildConnector_V2_1_1(ConnectorProfile c, DateTimeOffset now) =>
        new()
        {
            Id = c.Id,
            Standard = Enum.Parse<V2_1_1.ConnectorType>(c.Standard),
            Format = Enum.Parse<V2_1_1.ConnectorFormat>(c.Format),
            PowerType = Enum.Parse<V2_1_1.PowerType>(c.PowerType),
            Voltage = c.MaxVoltage,
            Amperage = c.MaxAmperage,
            TariffId = c.TariffId,
            LastUpdated = now,
        };

    private static V2_0.Connector BuildConnector_V2_0(ConnectorProfile c, DateTimeOffset now) =>
        new()
        {
            Id = c.Id,
            Standard = Enum.Parse<V2_0.ConnectorType>(c.Standard),
            Format = Enum.Parse<V2_0.ConnectorFormat>(c.Format),
            PowerType = Enum.Parse<V2_0.PowerType>(c.PowerType),
            Voltage = c.MaxVoltage,
            Amperage = c.MaxAmperage,
        };

    // ── Session ───────────────────────────────────────────────

    public static object BuildSession(
        OcpiVersion version,
        SessionState session,
        PartyIdentity cpoIdentity,
        PartyIdentity emspIdentity,
        LocationSpec location,
        string currency
    )
    {
        var periods = BuildChargingPeriods(version, session.ChargingPeriods);
        var status = session.Phase switch
        {
            SessionPhase.Pending => "PENDING",
            SessionPhase.Active => "ACTIVE",
            SessionPhase.Completed => "COMPLETED",
            _ => "ACTIVE",
        };

        return version switch
        {
            OcpiVersion.V2_2_1 => new V2_2_1.Session
            {
                CountryCode = cpoIdentity.CountryCode,
                PartyId = cpoIdentity.PartyId,
                Id = session.SessionId,
                StartDateTime = session.StartTime,
                EndDateTime = session.EndTime,
                Kwh = session.KwhDelivered,
                CdrToken = new V2_2_1.CdrToken
                {
                    CountryCode = emspIdentity.CountryCode,
                    PartyId = emspIdentity.PartyId,
                    Uid = session.TokenUid,
                    Type = V2_2_1.TokenType.RFID,
                    ContractId = session.TokenContractId,
                },
                AuthMethod = V2_2_1.AuthMethod.COMMAND,
                LocationId = session.LocationId,
                EvseUid = session.EvseUid,
                ConnectorId = session.ConnectorId,
                Currency = currency,
                ChargingPeriods = periods.Cast<V2_2_1.ChargingPeriod>().ToList(),
                TotalCost = new V2_2_1.Price { ExclVat = session.TotalCostExclVat, InclVat = session.TotalCostInclVat },
                Status = Enum.Parse<V2_2_1.SessionStatus>(status),
                LastUpdated = session.LastUpdated,
            },
            OcpiVersion.V2_2 => new V2_2.Session
            {
                CountryCode = cpoIdentity.CountryCode,
                PartyId = cpoIdentity.PartyId,
                Id = session.SessionId,
                StartDateTime = session.StartTime,
                EndDateTime = session.EndTime,
                Kwh = session.KwhDelivered,
                CdrToken = new V2_2.CdrToken
                {
                    Uid = session.TokenUid,
                    Type = V2_2.TokenType.RFID,
                    ContractId = session.TokenContractId,
                },
                AuthMethod = V2_2.AuthMethod.COMMAND,
                LocationId = session.LocationId,
                EvseUid = session.EvseUid,
                ConnectorId = session.ConnectorId,
                Currency = currency,
                ChargingPeriods = periods.Cast<V2_2.ChargingPeriod>().ToList(),
                TotalCost = new V2_2.Price { ExclVat = session.TotalCostExclVat, InclVat = session.TotalCostInclVat },
                Status = Enum.Parse<V2_2.SessionStatus>(status),
                LastUpdated = session.LastUpdated,
            },
            OcpiVersion.V2_1_1 => new V2_1_1.Session
            {
                Id = session.SessionId,
                StartDateTime = session.StartTime,
                EndDateTime = session.EndTime,
                Kwh = session.KwhDelivered,
                AuthId = session.TokenContractId,
                AuthMethod = V2_1_1.AuthMethod.AUTH_REQUEST,
                Location = (V2_1_1.Location)BuildLocation(OcpiVersion.V2_1_1, location, cpoIdentity),
                Currency = currency,
                ChargingPeriods = periods.Cast<V2_1_1.ChargingPeriod>().ToList(),
                TotalCost = session.TotalCostExclVat,
                Status = Enum.Parse<V2_1_1.SessionStatus>(status),
                LastUpdated = session.LastUpdated,
            },
            OcpiVersion.V2_0 => new V2_0.Session
            {
                Id = session.SessionId,
                StartDateTime = session.StartTime,
                EndDateTime = session.EndTime,
                Kwh = session.KwhDelivered,
                AuthId = session.TokenContractId,
                AuthMethod = V2_0.AuthMethod.AUTH_REQUEST,
                Location = (V2_0.Location)BuildLocation(OcpiVersion.V2_0, location, cpoIdentity),
                Currency = currency,
                ChargingPeriods = periods.Cast<V2_0.ChargingPeriod>().ToList(),
                Status = Enum.Parse<V2_0.SessionStatus>(status),
            },
            _ => throw new ArgumentOutOfRangeException(nameof(version)),
        };
    }

    // ── CDR ───────────────────────────────────────────────────

    public static object BuildCdr(
        OcpiVersion version,
        SessionState session,
        PartyIdentity cpoIdentity,
        PartyIdentity emspIdentity,
        LocationSpec location,
        string currency,
        decimal vatRate
    )
    {
        var totalCostExcl = session.TotalCostExclVat;
        var totalCostIncl = session.TotalCostInclVat;
        var totalTime = session.EndTime.HasValue ? (decimal)(session.EndTime.Value - session.StartTime).TotalHours : 0m;
        var periods = BuildChargingPeriods(version, session.ChargingPeriods)!;
        var cdrId = $"CDR-{session.SessionId}";
        var now = DateTimeOffset.UtcNow;
        var evseSpec = location.Evses.FirstOrDefault(e => e.Uid == session.EvseUid) ?? location.Evses[0];

        return version switch
        {
            OcpiVersion.V2_2_1 => new V2_2_1.Cdr
            {
                CountryCode = cpoIdentity.CountryCode,
                PartyId = cpoIdentity.PartyId,
                Id = cdrId,
                StartDateTime = session.StartTime,
                EndDateTime = session.EndTime ?? now,
                SessionId = session.SessionId,
                CdrToken = new V2_2_1.CdrToken
                {
                    CountryCode = emspIdentity.CountryCode,
                    PartyId = emspIdentity.PartyId,
                    Uid = session.TokenUid,
                    Type = V2_2_1.TokenType.RFID,
                    ContractId = session.TokenContractId,
                },
                AuthMethod = V2_2_1.AuthMethod.COMMAND,
                CdrLocation = new V2_2_1.CdrLocation
                {
                    Id = location.Id,
                    Name = location.Name,
                    Address = location.Address,
                    City = location.City,
                    PostalCode = location.PostalCode,
                    Country = location.Country,
                    Coordinates = new GeoLocation(location.Latitude, location.Longitude),
                    EvseUid = session.EvseUid,
                    EvseId = evseSpec.EvseId ?? session.EvseUid,
                    ConnectorId = session.ConnectorId,
                    ConnectorStandard = Enum.Parse<V2_2_1.ConnectorType>(evseSpec.Connector.Standard),
                    ConnectorFormat = Enum.Parse<V2_2_1.ConnectorFormat>(evseSpec.Connector.Format),
                    ConnectorPowerType = Enum.Parse<V2_2_1.PowerType>(evseSpec.Connector.PowerType),
                },
                Currency = currency,
                ChargingPeriods = periods.Cast<V2_2_1.ChargingPeriod>().ToList(),
                TotalCost = new V2_2_1.Price { ExclVat = totalCostExcl, InclVat = totalCostIncl },
                TotalEnergy = session.KwhDelivered,
                TotalTime = totalTime,
                TotalEnergyCost = new V2_2_1.Price { ExclVat = totalCostExcl, InclVat = totalCostIncl },
                LastUpdated = now,
            },
            OcpiVersion.V2_2 => new V2_2.Cdr
            {
                CountryCode = cpoIdentity.CountryCode,
                PartyId = cpoIdentity.PartyId,
                Id = cdrId,
                StartDateTime = session.StartTime,
                EndDateTime = session.EndTime ?? now,
                SessionId = session.SessionId,
                CdrToken = new V2_2.CdrToken
                {
                    Uid = session.TokenUid,
                    Type = V2_2.TokenType.RFID,
                    ContractId = session.TokenContractId,
                },
                AuthMethod = V2_2.AuthMethod.COMMAND,
                CdrLocation = new V2_2.CdrLocation
                {
                    Id = location.Id,
                    Name = location.Name,
                    Address = location.Address,
                    City = location.City,
                    PostalCode = location.PostalCode,
                    Country = location.Country,
                    Coordinates = new GeoLocation(location.Latitude, location.Longitude),
                    EvseUid = session.EvseUid,
                    EvseId = evseSpec.EvseId ?? session.EvseUid,
                    ConnectorId = session.ConnectorId,
                    ConnectorStandard = Enum.Parse<V2_2.ConnectorType>(evseSpec.Connector.Standard),
                    ConnectorFormat = Enum.Parse<V2_2.ConnectorFormat>(evseSpec.Connector.Format),
                    ConnectorPowerType = Enum.Parse<V2_2.PowerType>(evseSpec.Connector.PowerType),
                },
                Currency = currency,
                ChargingPeriods = periods.Cast<V2_2.ChargingPeriod>().ToList(),
                TotalCost = new V2_2.Price { ExclVat = totalCostExcl, InclVat = totalCostIncl },
                TotalEnergy = session.KwhDelivered,
                TotalTime = totalTime,
                TotalEnergyCost = new V2_2.Price { ExclVat = totalCostExcl, InclVat = totalCostIncl },
                LastUpdated = now,
            },
            OcpiVersion.V2_1_1 => new V2_1_1.Cdr
            {
                Id = cdrId,
                StartDateTime = session.StartTime,
                EndDateTime = session.EndTime ?? now,
                AuthId = session.TokenContractId,
                AuthMethod = V2_1_1.AuthMethod.AUTH_REQUEST,
                Location = (V2_1_1.Location)BuildLocation(OcpiVersion.V2_1_1, location, cpoIdentity),
                Currency = currency,
                ChargingPeriods = periods.Cast<V2_1_1.ChargingPeriod>().ToList(),
                TotalCost = totalCostExcl,
                TotalEnergy = session.KwhDelivered,
                TotalTime = totalTime,
                LastUpdated = now,
            },
            OcpiVersion.V2_0 => new V2_0.Cdr
            {
                Id = cdrId,
                StartDateTime = session.StartTime,
                EndDateTime = session.EndTime ?? now,
                AuthId = session.TokenContractId,
                AuthMethod = V2_0.AuthMethod.AUTH_REQUEST,
                Location = (V2_0.Location)BuildLocation(OcpiVersion.V2_0, location, cpoIdentity),
                Currency = currency,
                ChargingPeriods = periods.Cast<V2_0.ChargingPeriod>().ToList(),
                TotalCost = totalCostExcl,
                TotalEnergy = session.KwhDelivered,
                TotalTime = totalTime,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(version)),
        };
    }

    // ── Charging Periods ──────────────────────────────────────

    internal static List<object> BuildChargingPeriods(OcpiVersion version, List<ChargingPeriodState> periods)
    {
        var source = periods;
        if (source.Count == 0)
        {
            source =
            [
                new ChargingPeriodState
                {
                    StartTime = DateTimeOffset.UtcNow,
                    EnergyKwh = 0,
                    TimeHours = 0,
                },
            ];
        }

        return version switch
        {
            OcpiVersion.V2_2_1 => source
                .Select(p =>
                    (object)
                        new V2_2_1.ChargingPeriod
                        {
                            StartDateTime = p.StartTime,
                            Dimensions =
                            [
                                new V2_2_1.CdrDimension { Type = V2_2_1.CdrDimensionType.ENERGY, Volume = p.EnergyKwh },
                                new V2_2_1.CdrDimension { Type = V2_2_1.CdrDimensionType.TIME, Volume = p.TimeHours },
                            ],
                        }
                )
                .ToList(),
            OcpiVersion.V2_2 => source
                .Select(p =>
                    (object)
                        new V2_2.ChargingPeriod
                        {
                            StartDateTime = p.StartTime,
                            Dimensions =
                            [
                                new V2_2.CdrDimension { Type = V2_2.CdrDimensionType.ENERGY, Volume = p.EnergyKwh },
                                new V2_2.CdrDimension { Type = V2_2.CdrDimensionType.TIME, Volume = p.TimeHours },
                            ],
                        }
                )
                .ToList(),
            OcpiVersion.V2_1_1 => source
                .Select(p =>
                    (object)
                        new V2_1_1.ChargingPeriod
                        {
                            StartDateTime = p.StartTime,
                            Dimensions =
                            [
                                new V2_1_1.CdrDimension { Type = V2_1_1.CdrDimensionType.ENERGY, Volume = p.EnergyKwh },
                                new V2_1_1.CdrDimension { Type = V2_1_1.CdrDimensionType.TIME, Volume = p.TimeHours },
                            ],
                        }
                )
                .ToList(),
            OcpiVersion.V2_0 => source
                .Select(p =>
                    (object)
                        new V2_0.ChargingPeriod
                        {
                            StartDateTime = p.StartTime,
                            Dimensions =
                            [
                                new V2_0.CdrDimension { Type = V2_0.CdrDimensionType.ENERGY, Volume = p.EnergyKwh },
                                new V2_0.CdrDimension { Type = V2_0.CdrDimensionType.TIME, Volume = p.TimeHours },
                            ],
                        }
                )
                .ToList(),
            _ => throw new ArgumentOutOfRangeException(nameof(version)),
        };
    }

    // ── Tariff ─────────────────────────────────────────────────

    public static object BuildTariff(OcpiVersion version, TariffSpec spec, PartyIdentity cpoIdentity)
    {
        var now = DateTimeOffset.UtcNow;
        return version switch
        {
            OcpiVersion.V2_2_1 => new V2_2_1.Tariff
            {
                CountryCode = cpoIdentity.CountryCode,
                PartyId = cpoIdentity.PartyId,
                Id = spec.Id,
                Currency = spec.Currency,
                Elements =
                [
                    new V2_2_1.TariffElement
                    {
                        PriceComponents =
                        [
                            new V2_2_1.PriceComponent
                            {
                                Type = V2_2_1.TariffDimensionType.ENERGY,
                                Price = spec.PricePerKwh,
                                Vat = spec.VatRate * 100m,
                                StepSize = 1,
                            },
                        ],
                    },
                ],
                LastUpdated = now,
            },
            OcpiVersion.V2_2 => new V2_2.Tariff
            {
                CountryCode = cpoIdentity.CountryCode,
                PartyId = cpoIdentity.PartyId,
                Id = spec.Id,
                Currency = spec.Currency,
                Elements =
                [
                    new V2_2.TariffElement
                    {
                        PriceComponents =
                        [
                            new V2_2.PriceComponent
                            {
                                Type = V2_2.TariffDimensionType.ENERGY,
                                Price = spec.PricePerKwh,
                                Vat = spec.VatRate * 100m,
                                StepSize = 1,
                            },
                        ],
                    },
                ],
                LastUpdated = now,
            },
            OcpiVersion.V2_1_1 => new V2_1_1.Tariff
            {
                Id = spec.Id,
                Currency = spec.Currency,
                Elements =
                [
                    new V2_1_1.TariffElement
                    {
                        PriceComponents =
                        [
                            new V2_1_1.PriceComponent
                            {
                                Type = V2_1_1.TariffDimensionType.ENERGY,
                                Price = spec.PricePerKwh,
                                StepSize = 1,
                            },
                        ],
                    },
                ],
                LastUpdated = now,
            },
            OcpiVersion.V2_0 => new V2_0.Tariff
            {
                Id = spec.Id,
                Currency = spec.Currency,
                Elements =
                [
                    new V2_0.TariffElement
                    {
                        PriceComponents =
                        [
                            new V2_0.PriceComponent
                            {
                                Type = V2_0.TariffDimensionType.ENERGY,
                                Price = spec.PricePerKwh,
                                StepSize = 1,
                            },
                        ],
                    },
                ],
            },
            _ => throw new ArgumentOutOfRangeException(nameof(version)),
        };
    }

    // ── Credentials ───────────────────────────────────────────

    public static object BuildCredentialsResponse(
        OcpiVersion version,
        string tokenB,
        string versionsUrl,
        PartyIdentity cpoIdentity
    )
    {
        return version switch
        {
            OcpiVersion.V2_2_1 => new V2_2_1.Credentials
            {
                Token = tokenB,
                Url = versionsUrl,
                Roles =
                [
                    new V2_2_1.CredentialsRole
                    {
                        Role = V2_2_1.Role.CPO,
                        CountryCode = cpoIdentity.CountryCode,
                        PartyId = cpoIdentity.PartyId,
                        BusinessDetails = new V2_2_1.BusinessDetails { Name = "Test CPO" },
                    },
                ],
            },
            OcpiVersion.V2_2 => new V2_2.Credentials
            {
                Token = tokenB,
                Url = versionsUrl,
                Roles =
                [
                    new V2_2.CredentialsRole
                    {
                        Role = V2_2.Role.CPO,
                        CountryCode = cpoIdentity.CountryCode,
                        PartyId = cpoIdentity.PartyId,
                        BusinessDetails = new V2_2.BusinessDetails { Name = "Test CPO" },
                    },
                ],
            },
            OcpiVersion.V2_1_1 => new V2_1_1.Credentials
            {
                Token = tokenB,
                Url = versionsUrl,
                CountryCode = cpoIdentity.CountryCode,
                PartyId = cpoIdentity.PartyId,
                BusinessName = "Test CPO",
            },
            OcpiVersion.V2_0 => new V2_0.Credentials
            {
                Token = tokenB,
                Url = versionsUrl,
                CountryCode = cpoIdentity.CountryCode,
                PartyId = cpoIdentity.PartyId,
                BusinessName = "Test CPO",
            },
            _ => throw new ArgumentOutOfRangeException(nameof(version)),
        };
    }

    // ── Status mapping ────────────────────────────────────────

    private static EvseStatus ResolveEvseStatus(
        IReadOnlyDictionary<string, EvseState>? states,
        string locationId,
        string evseUid
    )
    {
        if (states is null)
            return EvseStatus.Available;

        return states.TryGetValue($"{locationId}:{evseUid}", out var state) ? state.Status : EvseStatus.Available;
    }

    private static V2_2_1.Status MapStatus_V2_2_1(EvseStatus status) =>
        status switch
        {
            EvseStatus.Available => V2_2_1.Status.AVAILABLE,
            EvseStatus.Reserved => V2_2_1.Status.RESERVED,
            EvseStatus.Charging => V2_2_1.Status.CHARGING,
            EvseStatus.Inoperative => V2_2_1.Status.INOPERATIVE,
            EvseStatus.OutOfOrder => V2_2_1.Status.OUTOFORDER,
            EvseStatus.Blocked => V2_2_1.Status.BLOCKED,
            EvseStatus.Planned => V2_2_1.Status.PLANNED,
            EvseStatus.Removed => V2_2_1.Status.REMOVED,
            _ => V2_2_1.Status.UNKNOWN,
        };

    private static V2_2.Status MapStatus_V2_2(EvseStatus status) =>
        status switch
        {
            EvseStatus.Available => V2_2.Status.AVAILABLE,
            EvseStatus.Reserved => V2_2.Status.RESERVED,
            EvseStatus.Charging => V2_2.Status.CHARGING,
            EvseStatus.Inoperative => V2_2.Status.INOPERATIVE,
            EvseStatus.OutOfOrder => V2_2.Status.OUTOFORDER,
            EvseStatus.Blocked => V2_2.Status.BLOCKED,
            EvseStatus.Planned => V2_2.Status.PLANNED,
            EvseStatus.Removed => V2_2.Status.REMOVED,
            _ => V2_2.Status.UNKNOWN,
        };

    private static V2_1_1.Status MapStatus_V2_1_1(EvseStatus status) =>
        status switch
        {
            EvseStatus.Available => V2_1_1.Status.AVAILABLE,
            EvseStatus.Reserved => V2_1_1.Status.RESERVED,
            EvseStatus.Charging => V2_1_1.Status.CHARGING,
            EvseStatus.Inoperative => V2_1_1.Status.INOPERATIVE,
            EvseStatus.OutOfOrder => V2_1_1.Status.OUTOFORDER,
            EvseStatus.Blocked => V2_1_1.Status.BLOCKED,
            EvseStatus.Planned => V2_1_1.Status.PLANNED,
            EvseStatus.Removed => V2_1_1.Status.REMOVED,
            _ => V2_1_1.Status.UNKNOWN,
        };

    private static V2_0.Status MapStatus_V2_0(EvseStatus status) =>
        status switch
        {
            EvseStatus.Available => V2_0.Status.AVAILABLE,
            EvseStatus.Reserved => V2_0.Status.RESERVED,
            EvseStatus.Charging => V2_0.Status.CHARGING,
            EvseStatus.Inoperative => V2_0.Status.INOPERATIVE,
            EvseStatus.OutOfOrder => V2_0.Status.OUTOFORDER,
            EvseStatus.Blocked => V2_0.Status.BLOCKED,
            EvseStatus.Planned => V2_0.Status.PLANNED,
            EvseStatus.Removed => V2_0.Status.REMOVED,
            _ => V2_0.Status.UNKNOWN,
        };
}
