namespace CortexiaAuth.Api.Services;

public interface ICortexiaCredentialProtector
{
    string Protect(string cortexiaPassword);
    string Unprotect(string protectedCortexiaPassword);
}
