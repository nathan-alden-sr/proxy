namespace NathanAlden.Proxy.Services.CredentialService
{
    public interface ICredentialService
    {
        (GetCredentialsResult result, string username, string clearTextPassword) GetCredentials(string username = null);
    }
}