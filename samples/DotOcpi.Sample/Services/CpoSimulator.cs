using DotOcpi.Simulator;
using DotOcpi.Simulator.Charging;
using DotOcpi.Simulator.Models;

namespace DotOcpi.Sample.Services;

/// <summary>
/// Manages simulated CPO server lifecycle. Creates two test CPOs on startup:
/// DE:CPO (OCPI 2.2.1) and FR:ION (OCPI 2.1.1) to demonstrate multi-version support.
/// Uses the typed simulator API with EVSE state machine and charging simulation.
/// </summary>
public sealed partial class CpoSimulator(ILogger<CpoSimulator> logger) : IHostedService, IAsyncDisposable
{
    private readonly ILogger _logger = logger;
    private OcpiCpoSimulator? _germanCpo;
    private OcpiCpoSimulator? _frenchCpo;

    public OcpiCpoSimulator GermanCpo =>
        _germanCpo ?? throw new InvalidOperationException("CPO simulator not started.");

    public OcpiCpoSimulator FrenchCpo =>
        _frenchCpo ?? throw new InvalidOperationException("CPO simulator not started.");

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _germanCpo = await CreateGermanCpoAsync().ConfigureAwait(false);
        LogCpoStarted("DE:CPO (v2.2.1)", _germanCpo.BaseUrl);

        _frenchCpo = await CreateFrenchCpoAsync().ConfigureAwait(false);
        LogCpoStarted("FR:ION (v2.1.1)", _frenchCpo.BaseUrl);
    }

    public async Task StopAsync(CancellationToken cancellationToken) =>
        await DisposeServersAsync().ConfigureAwait(false);

    public ValueTask DisposeAsync() => DisposeServersAsync();

    private async ValueTask DisposeServersAsync()
    {
        var german = Interlocked.Exchange(ref _germanCpo, null);
        var french = Interlocked.Exchange(ref _frenchCpo, null);
        if (german is not null)
            await german.DisposeAsync().ConfigureAwait(false);
        if (french is not null)
            await french.DisposeAsync().ConfigureAwait(false);
    }

    // ── German CPO: OCPI 2.2.1 with full typed data ──────────────

    private static Task<OcpiCpoSimulator> CreateGermanCpoAsync() =>
        OcpiCpoSimulator.CreateAsync(config =>
        {
            config.SupportedVersions = [OcpiVersion.V2_2_1];
            config.CpoIdentity = new PartyIdentity("DE", "CPO");
            config.EmspIdentity = new PartyIdentity("NL", "MSP");
            config.CommandCallbackEnabled = true;
            config.CommandCallbackDelay = TimeSpan.FromMilliseconds(200);
            config.CpoCurrency = "EUR";
            config.DefaultVatRate = 0.19m;

            // Charging simulation: DC fast with realistic CC/CV curve
            config.DefaultChargingProfile = new ChargingProfileSpec
            {
                MaxPowerKw = 350m,
                TaperStartFraction = 0.80m,
                TaperFinalPowerKw = 25m,
                TargetKwh = 75m,
                PricePerKwh = 0.39m,
                VatRate = 0.19m,
            };
            config.ChargingTickInterval = TimeSpan.FromSeconds(5);

            config.LocationSpecs =
            [
                new LocationSpec
                {
                    Id = "LOC1",
                    Name = "Berlin Hauptbahnhof Charging Hub",
                    Address = "Europaplatz 1",
                    City = "Berlin",
                    PostalCode = "10557",
                    Country = "DEU",
                    Latitude = "52.5251",
                    Longitude = "13.3694",
                    TimeZone = "Europe/Berlin",
                    Evses =
                    [
                        new EvseSpec
                        {
                            Uid = "DE*CPO*E001",
                            EvseId = "DE*CPO*E001",
                            Connector = new ConnectorProfile
                            {
                                Id = "C1",
                                Standard = "IEC_62196_T2_COMBO",
                                Format = "CABLE",
                                PowerType = "DC",
                                MaxVoltage = 920,
                                MaxAmperage = 500,
                                MaxPowerKw = 350m,
                                TariffId = "TAR1",
                            },
                            Capabilities = ["REMOTE_START_STOP_CAPABLE", "RFID_READER", "CONTACTLESS_CARD_SUPPORT"],
                        },
                        new EvseSpec
                        {
                            Uid = "DE*CPO*E002",
                            EvseId = "DE*CPO*E002",
                            Connector = new ConnectorProfile
                            {
                                Id = "C1",
                                Standard = "CHADEMO",
                                Format = "CABLE",
                                PowerType = "DC",
                                MaxVoltage = 500,
                                MaxAmperage = 120,
                                MaxPowerKw = 60m,
                                TariffId = "TAR1",
                            },
                            Capabilities = ["REMOTE_START_STOP_CAPABLE", "RFID_READER"],
                        },
                    ],
                },
                new LocationSpec
                {
                    Id = "LOC2",
                    Name = "Munich Airport Fast Charger",
                    Address = "Nordallee 25",
                    City = "Munich",
                    PostalCode = "85356",
                    Country = "DEU",
                    Latitude = "48.3537",
                    Longitude = "11.7750",
                    TimeZone = "Europe/Berlin",
                    Evses =
                    [
                        new EvseSpec
                        {
                            Uid = "DE*CPO*E003",
                            EvseId = "DE*CPO*E003",
                            Connector = new ConnectorProfile
                            {
                                Id = "C1",
                                Standard = "IEC_62196_T2",
                                Format = "SOCKET",
                                PowerType = "AC_3_PHASE",
                                MaxVoltage = 400,
                                MaxAmperage = 32,
                                MaxPowerKw = 22m,
                                TariffId = "TAR2",
                            },
                            Capabilities = ["REMOTE_START_STOP_CAPABLE", "RFID_READER", "RESERVABLE"],
                        },
                    ],
                },
            ];

            config.TariffSpecs =
            [
                new TariffSpec
                {
                    Id = "TAR1",
                    Currency = "EUR",
                    PricePerKwh = 0.39m,
                    VatRate = 0.19m,
                },
                new TariffSpec
                {
                    Id = "TAR2",
                    Currency = "EUR",
                    PricePerKwh = 0.29m,
                    VatRate = 0.19m,
                },
            ];

            // Tariffs still use legacy format for rich OCPI structure
            // (TariffSpec only captures pricing; full tariff elements need legacy objects)
            config.Tariffs =
            [
                new
                {
                    country_code = "DE",
                    party_id = "CPO",
                    id = "TAR1",
                    currency = "EUR",
                    type = "REGULAR",
                    tariff_alt_text = new[]
                    {
                        new { language = "en", text = "DC fast charging: 0.39 EUR/kWh + 0.05 EUR/min after 45 min" },
                    },
                    elements = new object[]
                    {
                        new
                        {
                            price_components = new[]
                            {
                                new
                                {
                                    type = "ENERGY",
                                    price = 0.39m,
                                    vat = 19.0m,
                                    step_size = 1,
                                },
                            },
                        },
                        new
                        {
                            price_components = new[]
                            {
                                new
                                {
                                    type = "PARKING_TIME",
                                    price = 0.05m,
                                    vat = 19.0m,
                                    step_size = 60,
                                },
                            },
                            restrictions = new { min_duration = 2700 },
                        },
                    },
                    last_updated = "2026-01-01T00:00:00Z",
                },
                new
                {
                    country_code = "DE",
                    party_id = "CPO",
                    id = "TAR2",
                    currency = "EUR",
                    type = "REGULAR",
                    tariff_alt_text = new[] { new { language = "en", text = "AC charging: 0.29 EUR/kWh" } },
                    elements = new[]
                    {
                        new
                        {
                            price_components = new[]
                            {
                                new
                                {
                                    type = "ENERGY",
                                    price = 0.29m,
                                    vat = 19.0m,
                                    step_size = 1,
                                },
                                new
                                {
                                    type = "TIME",
                                    price = 0.02m,
                                    vat = 19.0m,
                                    step_size = 60,
                                },
                            },
                        },
                    },
                    last_updated = "2026-01-01T00:00:00Z",
                },
            ];
        });

    // ── French CPO: OCPI 2.1.1 (legacy mode for older version) ───

    private static Task<OcpiCpoSimulator> CreateFrenchCpoAsync() =>
        OcpiCpoSimulator.CreateAsync(config =>
        {
            config.SupportedVersions = [OcpiVersion.V2_1_1];
            config.CpoIdentity = new PartyIdentity("FR", "ION");

            config.Locations =
            [
                new
                {
                    id = "LOC-PAR1",
                    name = "Paris La Defense Charging Station",
                    address = "1 Parvis de La Defense",
                    city = "Paris",
                    postal_code = "92800",
                    country = "FRA",
                    coordinates = new { latitude = "48.8920", longitude = "2.2360" },
                    time_zone = "Europe/Paris",
                    evses = new[]
                    {
                        new
                        {
                            uid = "FR*ION*E001",
                            evse_id = "FR*ION*E001",
                            status = "AVAILABLE",
                            connectors = new[]
                            {
                                new
                                {
                                    id = "C1",
                                    standard = "IEC_62196_T2_COMBO",
                                    format = "CABLE",
                                    power_type = "DC",
                                    voltage = 920,
                                    amperage = 500,
                                    tariff_id = "TAR-FR1",
                                    last_updated = "2026-03-01T09:00:00Z",
                                },
                                new
                                {
                                    id = "C2",
                                    standard = "CHADEMO",
                                    format = "CABLE",
                                    power_type = "DC",
                                    voltage = 500,
                                    amperage = 120,
                                    tariff_id = "TAR-FR1",
                                    last_updated = "2026-03-01T09:00:00Z",
                                },
                            },
                            last_updated = "2026-03-01T09:00:00Z",
                        },
                    },
                    last_updated = "2026-03-01T09:00:00Z",
                },
            ];

            config.Tariffs =
            [
                new
                {
                    id = "TAR-FR1",
                    currency = "EUR",
                    elements = new[]
                    {
                        new
                        {
                            price_components = new[]
                            {
                                new
                                {
                                    type = "ENERGY",
                                    price = 0.35m,
                                    step_size = 1,
                                },
                                new
                                {
                                    type = "TIME",
                                    price = 0.03m,
                                    step_size = 60,
                                },
                            },
                        },
                    },
                    last_updated = "2026-01-01T00:00:00Z",
                },
            ];
        });

    [LoggerMessage(Level = LogLevel.Information, Message = "Started {CpoId} at {BaseUrl}")]
    private partial void LogCpoStarted(string cpoId, Uri baseUrl);
}
