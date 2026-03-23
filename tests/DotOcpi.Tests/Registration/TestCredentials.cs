using DotOcpi;
using Ocpi_V2_0 = DotOcpi.Models.V2_0;
using Ocpi_V2_1_1 = DotOcpi.Models.V2_1_1;
using Ocpi_V2_2_1 = DotOcpi.Models.V2_2_1;

namespace DotOcpi.Tests.Registration;

internal static class TestCredentials
{
    internal static Ocpi_V2_2_1.Credentials V2_2_1(string token = "test-token") =>
        new()
        {
            Token = token,
            Url = "https://emsp.example.com/ocpi/versions",
            Roles =
            [
                new Ocpi_V2_2_1.CredentialsRole
                {
                    Role = Ocpi_V2_2_1.Role.EMSP,
                    BusinessDetails = new Ocpi_V2_2_1.BusinessDetails { Name = "Test eMSP" },
                    PartyId = new CiString("MSP"),
                    CountryCode = new CiString("NL"),
                },
            ],
        };

    internal static Ocpi_V2_1_1.Credentials V2_1_1(string token = "test-token") =>
        new()
        {
            Token = token,
            Url = "https://emsp.example.com/ocpi/versions",
            BusinessName = "Test eMSP",
            PartyId = "MSP",
            CountryCode = "NL",
        };

    internal static Ocpi_V2_0.Credentials V2_0(string token = "test-token") =>
        new()
        {
            Token = token,
            Url = "https://emsp.example.com/ocpi/versions",
            BusinessName = "Test eMSP",
            PartyId = "MSP",
            CountryCode = "NL",
        };
}
