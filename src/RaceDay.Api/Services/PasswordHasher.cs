using System;
using System.Security.Cryptography;

namespace RaceDay.Api.Services;

// Handles turning a plain text password into something safe to store, and
// checking a login attempt against that stored value.

// We never store the password itself. If the database ever leaked, the
// attacker would only get hashes, which cannot be reversed back into
// passwords. Each user also gets their own random salt, so two people who
// happen to pick the same password still end up with different hashes.
public static class PasswordHasher
{
    private const int SaltSize = 16;        // 128 bits
    private const int HashSize = 32;        // 256 bits
    private const int Iterations = 100_000; 

    public static string Hash(string password)
    {
        // A cryptographically random salt, different for every single user.
        var salt = RandomNumberGenerator.GetBytes(SaltSize);

        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        // Store salt and hash together as "salt.hash" in Base64, so a single
        // database column holds everything needed to verify later on.
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string storedHash)
    {
        // Older or seeded rows might not follow our format, so fail safely
        // rather than throwing an exception the user cannot do anything about.
        var parts = storedHash.Split('.');
        if (parts.Length != 2)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[0]);
            var expected = Convert.FromBase64String(parts[1]);

            var actual = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize);

            // Fixed time comparison. A normal == would return faster on an
            // early mismatch, which in theory leaks information to an attacker.
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}