using System.Security.Cryptography;
using System.Text;

namespace TeaShopManager.DataAccess;

public static class PasswordHasher
{
    public static string HashPasswordSha256(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}
