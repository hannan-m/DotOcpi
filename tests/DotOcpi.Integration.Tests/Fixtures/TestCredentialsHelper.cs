using DotOcpi;

namespace DotOcpi.Integration.Tests.Fixtures;

internal static class TestCredentialsHelper
{
    internal static Models.V2_2_1.Credentials V2_2_1(string token = "test-token") =>
        new()
        {
            Token = token,
            Url = "https://emsp.example.com/ocpi/versions",
            Roles =
            [
                new Models.V2_2_1.CredentialsRole
                {
                    Role = Models.V2_2_1.Role.EMSP,
                    BusinessDetails = new Models.V2_2_1.BusinessDetails { Name = "Test eMSP" },
                    PartyId = new CiString("MSP"),
                    CountryCode = new CiString("NL"),
                },
            ],
        };
}
