using Microsoft.AspNetCore.DataProtection;

namespace NexaFlow.Infrastructure.Integrations;

/// <summary>
/// Encrypts/decrypts integration credential JSON before it touches the database, using ASP.NET
/// Data Protection. A fixed purpose string isolates these payloads from any other protected data.
/// </summary>
public class IntegrationCrypto
{
    private readonly IDataProtector _protector;

    public IntegrationCrypto(IDataProtectionProvider provider)
        => _protector = provider.CreateProtector("NexaFlow.TenantIntegration.Config.v1");

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string Unprotect(string ciphertext) => _protector.Unprotect(ciphertext);
}
