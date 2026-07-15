using System.Security.Cryptography;

namespace Hotfix.Utils;

public static class IdGenerator
{
    public static Guid CreateUuid()
    {
        var machineId = Environment.GetEnvironmentVariable("MACHINE_ID") ?? "0";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var random = RandomNumberGenerator.GetInt32(int.MaxValue);

        var bytes = new byte[16];
        BitConverter.TryWriteBytes(bytes.AsSpan(0, 8), timestamp);
        BitConverter.TryWriteBytes(bytes.AsSpan(8, 4), random);
        System.Text.Encoding.UTF8.GetBytes(machineId).CopyTo(bytes, 12);

        return new Guid(bytes);
    }
}

public static class PasswordHelper
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    public static string GenerateSalt()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(SaltSize));
    }

    public static string Hash(string password, string salt)
    {
        var saltBytes = Convert.FromBase64String(salt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, Iterations, HashAlgorithmName.SHA256, HashSize);
        return Convert.ToBase64String(hash);
    }
}
