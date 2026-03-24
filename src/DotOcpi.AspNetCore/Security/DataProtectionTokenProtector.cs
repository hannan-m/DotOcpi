using DotOcpi.Security;
using Microsoft.AspNetCore.DataProtection;

namespace DotOcpi.AspNetCore.Security;

/// <summary>
/// Protects outbound CPO tokens using the ASP.NET Core Data Protection API.
/// Registered automatically by <see cref="DotOcpiAspNetCoreExtensions.AddAspNetCoreServer"/>,
/// replacing the default <see cref="PlaintextTokenProtector"/>.
/// </summary>
/// <remarks>
/// Uses purpose string "DotOcpi.CpoTokens" to isolate these keys from other
/// Data Protection consumers in the application.
/// </remarks>
internal sealed class DataProtectionTokenProtector : ITokenProtector
{
    private readonly IDataProtector _protector;

    public DataProtectionTokenProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("DotOcpi.CpoTokens");
    }

    /// <inheritdoc />
    public string Protect(string plaintext) => _protector.Protect(plaintext);

    /// <inheritdoc />
    public string Unprotect(string protectedData) => _protector.Unprotect(protectedData);
}
