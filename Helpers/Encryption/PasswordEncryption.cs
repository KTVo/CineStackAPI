using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace CineStackAPI.Helpers.Encryption;

public static class PasswordEncryption
{
    /// <summary>
    /// Hashes a password using PBKDF2.
    /// </summary>
    /// <param name="password"></param>
    /// <returns></returns> <summary>
    /// 
    /// </summary>
    /// <param name="password"></param>
    /// <returns></returns>
    public static string? HashPassword(string? password)
    {
        if (string.IsNullOrEmpty(password) == true) { return null; }

        byte[] salt = RandomNumberGenerator.GetBytes(128 / 8); // 128-bit salt

        string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 10000,
            numBytesRequested: 256 / 8)); // 256-bit hash

        return hashed;
    }
}
