using DotOcpi.Models.V2_2_1;

namespace DotOcpi.Sample.Services;

/// <summary>
/// Builds version-appropriate OCPI credentials objects.
/// </summary>
public static class CredentialsHelper
{
    public static object Build(string tokenValue, OcpiVersion version) =>
        version switch
        {
            OcpiVersion.V2_2_1 or OcpiVersion.V2_2 => new Credentials
            {
                Token = tokenValue,
                Url = "https://emsp.example.com/ocpi/versions",
                Roles =
                [
                    new CredentialsRole
                    {
                        Role = Role.EMSP,
                        BusinessDetails = new BusinessDetails { Name = "DotOcpi Sample eMSP" },
                        PartyId = new CiString("MSP"),
                        CountryCode = new CiString("NL"),
                    },
                ],
            },
            _ => new Models.V2_1_1.Credentials
            {
                Token = tokenValue,
                Url = "https://emsp.example.com/ocpi/versions",
                BusinessName = "DotOcpi Sample eMSP",
                PartyId = "MSP",
                CountryCode = "NL",
            },
        };
}
