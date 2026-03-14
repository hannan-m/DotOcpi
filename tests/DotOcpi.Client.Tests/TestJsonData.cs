namespace DotOcpi.Client.Tests;

/// <summary>
/// Valid OCPI JSON test data per module. Uses the superset of required fields
/// across all versions — extra fields are ignored by earlier versions.
/// </summary>
internal static class TestJsonData
{
    internal const string Location = """
        {
            "country_code": "DE",
            "party_id": "ALL",
            "id": "LOC1",
            "publish": true,
            "address": "Beispielstraße 1",
            "city": "Berlin",
            "postal_code": "10115",
            "country": "DEU",
            "coordinates": {"latitude": "52.520008", "longitude": "13.404954"},
            "time_zone": "Europe/Berlin",
            "last_updated": "2024-01-01T00:00:00Z"
        }
        """;

    internal const string Location2 = """
        {
            "country_code": "DE",
            "party_id": "ALL",
            "id": "LOC2",
            "publish": true,
            "address": "Musterweg 2",
            "city": "Munich",
            "postal_code": "80331",
            "country": "DEU",
            "coordinates": {"latitude": "48.137154", "longitude": "11.576124"},
            "time_zone": "Europe/Berlin",
            "last_updated": "2024-01-01T00:00:00Z"
        }
        """;

    internal const string Session = """
        {
            "country_code": "DE",
            "party_id": "ALL",
            "id": "S1",
            "start_date_time": "2024-01-01T10:00:00Z",
            "kwh": 0.0,
            "cdr_token": {"country_code": "NL", "party_id": "TNM", "uid": "012345678", "type": "RFID", "contract_id": "NL-TNM-000001"},
            "auth_method": "AUTH_REQUEST",
            "location_id": "LOC1",
            "evse_uid": "3256",
            "connector_id": "1",
            "currency": "EUR",
            "total_cost": {"excl_vat": 0.0},
            "status": "ACTIVE",
            "last_updated": "2024-01-01T10:00:00Z"
        }
        """;

    internal const string Session2 = """
        {
            "country_code": "DE",
            "party_id": "ALL",
            "id": "S2",
            "start_date_time": "2024-01-01T11:00:00Z",
            "kwh": 10.5,
            "cdr_token": {"country_code": "NL", "party_id": "TNM", "uid": "012345679", "type": "RFID", "contract_id": "NL-TNM-000002"},
            "auth_method": "AUTH_REQUEST",
            "location_id": "LOC1",
            "evse_uid": "3256",
            "connector_id": "1",
            "currency": "EUR",
            "total_cost": {"excl_vat": 5.25},
            "status": "COMPLETED",
            "last_updated": "2024-01-01T12:00:00Z"
        }
        """;

    internal const string Cdr = """
        {
            "country_code": "DE",
            "party_id": "ALL",
            "id": "CDR1",
            "start_date_time": "2024-01-01T10:00:00Z",
            "end_date_time": "2024-01-01T12:00:00Z",
            "cdr_token": {"country_code": "NL", "party_id": "TNM", "uid": "012345678", "type": "RFID", "contract_id": "NL-TNM-000001"},
            "auth_method": "AUTH_REQUEST",
            "cdr_location": {"id": "LOC1", "address": "Beispielstraße 1", "city": "Berlin", "postal_code": "10115", "country": "DEU", "coordinates": {"latitude": "52.520008", "longitude": "13.404954"}, "evse_uid": "3256", "evse_id": "DE*ALL*E3256", "connector_id": "1", "connector_standard": "IEC_62196_T2", "connector_format": "SOCKET", "connector_power_type": "AC_3_PHASE"},
            "currency": "EUR",
            "total_cost": {"excl_vat": 15.50},
            "total_energy": 30.0,
            "total_time": 2.0,
            "charging_periods": [],
            "auth_id": "NL-TNM-000001",
            "location_id": "LOC1",
            "evse_uid": "3256",
            "connector_id": "1",
            "last_updated": "2024-01-01T12:00:00Z"
        }
        """;

    internal const string Cdr2 = """
        {
            "country_code": "DE",
            "party_id": "ALL",
            "id": "CDR2",
            "start_date_time": "2024-01-02T08:00:00Z",
            "end_date_time": "2024-01-02T09:30:00Z",
            "cdr_token": {"country_code": "NL", "party_id": "TNM", "uid": "012345679", "type": "RFID", "contract_id": "NL-TNM-000002"},
            "auth_method": "AUTH_REQUEST",
            "cdr_location": {"id": "LOC2", "address": "Musterweg 2", "city": "Munich", "postal_code": "80331", "country": "DEU", "coordinates": {"latitude": "48.137154", "longitude": "11.576124"}, "evse_uid": "3257", "evse_id": "DE*ALL*E3257", "connector_id": "1", "connector_standard": "IEC_62196_T2", "connector_format": "SOCKET", "connector_power_type": "AC_3_PHASE"},
            "currency": "EUR",
            "total_cost": {"excl_vat": 8.75},
            "total_energy": 15.0,
            "total_time": 1.5,
            "charging_periods": [],
            "auth_id": "NL-TNM-000002",
            "location_id": "LOC2",
            "evse_uid": "3257",
            "connector_id": "1",
            "last_updated": "2024-01-02T09:30:00Z"
        }
        """;

    internal const string Tariff = """
        {
            "country_code": "DE",
            "party_id": "ALL",
            "id": "T1",
            "currency": "EUR",
            "elements": [{"price_components": [{"type": "ENERGY", "price": 0.25, "step_size": 1}]}],
            "last_updated": "2024-01-01T00:00:00Z"
        }
        """;

    internal const string Tariff2 = """
        {
            "country_code": "DE",
            "party_id": "ALL",
            "id": "T2",
            "currency": "EUR",
            "elements": [{"price_components": [{"type": "TIME", "price": 2.00, "step_size": 300}]}],
            "last_updated": "2024-01-01T00:00:00Z"
        }
        """;

    internal static string WrapList(params string[] items) =>
        $$"""{"status_code": 1000, "data": [{{string.Join(",", items)}}], "timestamp": "2024-01-01T00:00:00Z"}""";

    internal static string WrapObject(string data) =>
        $$"""{"status_code": 1000, "data": {{data}}, "timestamp": "2024-01-01T00:00:00Z"}""";

    internal static string WrapEmpty() => """{"status_code": 1000, "data": [], "timestamp": "2024-01-01T00:00:00Z"}""";

    internal static string WrapError(int statusCode = 3000, string message = "Server error") =>
        $$"""{"status_code": {{statusCode}}, "status_message": "{{message}}"}""";
}
