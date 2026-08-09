using System.Security.Cryptography;
using System.Text;

namespace WarehouseApp.Data.Data;

/// <summary>
/// Demo-only hashing (no salt/iterations) — good enough to avoid storing plaintext
/// in this showcase app, not a production-grade credential store.
/// </summary>
public static class PasswordHasher
{
    public static string Hash(string password)
    {
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    public static bool Verify(string password, string hash) => Hash(password) == hash;
}
