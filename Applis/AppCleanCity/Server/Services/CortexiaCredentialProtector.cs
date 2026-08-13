using Microsoft.AspNetCore.DataProtection;

namespace CortexiaAuth.Api.Services;

public class CortexiaCredentialProtector : ICortexiaCredentialProtector
{
    private readonly IDataProtector _protector;

    public CortexiaCredentialProtector(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("CortexiaAuth.CortexiaCredentials.v1");
    }

    public string Protect(string cortexiaPassword) => _protector.Protect(cortexiaPassword);

    public string Unprotect(string protectedCortexiaPassword) => _protector.Unprotect(protectedCortexiaPassword);
}
