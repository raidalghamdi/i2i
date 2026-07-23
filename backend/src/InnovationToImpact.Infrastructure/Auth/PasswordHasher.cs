namespace InnovationToImpact.Infrastructure.Auth;

public static class PasswordHasher
{
    public static string Hash(string plaintext) => BCrypt.Net.BCrypt.HashPassword(plaintext, workFactor: 12);

    public static bool Verify(string plaintext, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(plaintext, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
