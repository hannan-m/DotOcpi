using DotOcpi.Models.V2_2_1;

namespace DotOcpi.Tests.Fixtures;

/// <summary>
/// Shared test data factories for creating valid OCPI model instances.
/// All methods return minimal valid objects — tests should override specific fields as needed.
/// </summary>
public static class TestData
{
    private static readonly DateTimeOffset DefaultTimestamp = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    public static Connector CreateConnector(
        string id = "1",
        ConnectorType standard = ConnectorType.IEC_62196_T2,
        ConnectorFormat format = ConnectorFormat.SOCKET,
        PowerType powerType = PowerType.AC_3_PHASE,
        int maxVoltage = 230,
        int maxAmperage = 32) =>
        new()
        {
            Id = id,
            Standard = standard,
            Format = format,
            PowerType = powerType,
            MaxVoltage = maxVoltage,
            MaxAmperage = maxAmperage,
            LastUpdated = DefaultTimestamp,
        };

    public static Evse CreateEvse(
        string uid = "3256",
        Status status = Status.AVAILABLE,
        Connector? connector = null) =>
        new()
        {
            Uid = uid,
            Status = status,
            Connectors = [connector ?? CreateConnector()],
            LastUpdated = DefaultTimestamp,
        };

    public static Location CreateLocation(
        string countryCode = "NL",
        string partyId = "TNM",
        string id = "LOC1",
        bool publish = true,
        Evse? evse = null) =>
        new()
        {
            CountryCode = countryCode,
            PartyId = partyId,
            Id = id,
            Publish = publish,
            Address = "Keizersgracht 585",
            City = "Amsterdam",
            Country = "NLD",
            Coordinates = new GeoLocation("52.364115", "4.891860"),
            TimeZone = "Europe/Amsterdam",
            Evses = evse is not null ? [evse] : null,
            LastUpdated = DefaultTimestamp,
        };

    public static CdrToken CreateCdrToken(
        string uid = "TOKEN-001",
        TokenType type = TokenType.RFID) =>
        new()
        {
            CountryCode = "NL",
            PartyId = "TNM",
            Uid = uid,
            Type = type,
            ContractId = "NL-TNM-C00001",
        };

    public static Session CreateSession(
        string id = "SES-001",
        SessionStatus status = SessionStatus.ACTIVE) =>
        new()
        {
            CountryCode = "NL",
            PartyId = "TNM",
            Id = id,
            StartDateTime = DefaultTimestamp,
            Kwh = 15.5m,
            CdrToken = CreateCdrToken(),
            AuthMethod = AuthMethod.WHITELIST,
            LocationId = "LOC1",
            EvseUid = "3256",
            ConnectorId = "1",
            Currency = "EUR",
            Status = status,
            LastUpdated = DefaultTimestamp,
        };

    public static CdrLocation CreateCdrLocation() =>
        new()
        {
            Id = "LOC1",
            Address = "Keizersgracht 585",
            City = "Amsterdam",
            Country = "NLD",
            Coordinates = new GeoLocation("52.364115", "4.891860"),
            EvseUid = "3256",
            EvseId = "NL*TNM*E01",
            ConnectorId = "1",
            ConnectorStandard = ConnectorType.IEC_62196_T2,
            ConnectorFormat = ConnectorFormat.SOCKET,
            ConnectorPowerType = PowerType.AC_3_PHASE,
        };

    public static Cdr CreateCdr(string id = "CDR-001") =>
        new()
        {
            CountryCode = "NL",
            PartyId = "TNM",
            Id = id,
            StartDateTime = DefaultTimestamp,
            EndDateTime = DefaultTimestamp.AddHours(2),
            CdrToken = CreateCdrToken(),
            AuthMethod = AuthMethod.WHITELIST,
            CdrLocation = CreateCdrLocation(),
            Currency = "EUR",
            ChargingPeriods =
            [
                new ChargingPeriod
                {
                    StartDateTime = DefaultTimestamp,
                    Dimensions = [new CdrDimension { Type = CdrDimensionType.ENERGY, Volume = 30.0m }],
                },
            ],
            TotalCost = new Price { ExclVat = 12.50m, InclVat = 15.13m },
            TotalEnergy = 30.0m,
            TotalTime = 2.0m,
            LastUpdated = DefaultTimestamp,
        };

    public static PriceComponent CreatePriceComponent(
        TariffDimensionType type = TariffDimensionType.ENERGY,
        decimal price = 0.25m,
        int stepSize = 1) =>
        new()
        {
            Type = type,
            Price = price,
            StepSize = stepSize,
        };

    public static Tariff CreateTariff(string id = "TARIFF-001") =>
        new()
        {
            CountryCode = "NL",
            PartyId = "TNM",
            Id = id,
            Currency = "EUR",
            Elements =
            [
                new TariffElement
                {
                    PriceComponents = [CreatePriceComponent()],
                },
            ],
            LastUpdated = DefaultTimestamp,
        };

    public static Token CreateToken(
        string uid = "TOKEN-001",
        TokenType type = TokenType.RFID,
        bool valid = true) =>
        new()
        {
            CountryCode = "NL",
            PartyId = "TNM",
            Uid = uid,
            Type = type,
            ContractId = "NL-TNM-C00001",
            Issuer = "TheNewMotion",
            Valid = valid,
            Whitelist = WhitelistType.ALLOWED,
            LastUpdated = DefaultTimestamp,
        };

    public static Credentials CreateCredentials() =>
        new()
        {
            Token = "dGVzdC10b2tlbi1iYXNlNjQ=",
            Url = "https://example.com/ocpi/versions",
            Roles =
            [
                new CredentialsRole
                {
                    Role = Role.EMSP,
                    BusinessDetails = new BusinessDetails { Name = "Test eMSP" },
                    PartyId = "TNM",
                    CountryCode = "NL",
                },
            ],
        };
}
