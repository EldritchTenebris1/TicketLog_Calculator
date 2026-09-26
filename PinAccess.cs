using System.Security.Cryptography;

namespace TicketLog.App;

internal static class PinAccess
{
    // Para trocar o PIN, execute tools/Set-Pin.ps1 e compile ou publique novamente.
    private static readonly byte[] Salt = Convert.FromHexString("FFB6C6A73422EF51F7E650AC499269C3");
    private static readonly byte[] HashEsperado = Convert.FromHexString("46B70897373F5EC0AD04F9F0F63A0B71E0D7F5FD259755251FF722F56BF98FD6");

    internal static bool Validar(string pin)
    {
        if (pin.Length != 6 || !pin.All(c => c >= '0' && c <= '9'))
            return false;

        var hash = Rfc2898DeriveBytes.Pbkdf2(pin, Salt, 120_000, HashAlgorithmName.SHA256, 32);
        try
        {
            return CryptographicOperations.FixedTimeEquals(hash, HashEsperado);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(hash);
        }
    }
}
