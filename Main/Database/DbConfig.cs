namespace Main.Database;

public readonly record struct DbConfig(
    string Host,
    string Port,
    string User,
    string Password,
    string Database
)
{
    public static DbConfig LoadFromEnv()
    {
        return new DbConfig(
            Env("DB_HOST", "localhost"),
            Env("DB_PORT", "5432"),
            Env("DB_USER", "postgres"),
            Env("DB_PASSWORD", "password"),
            Env("DB_NAME", "boc_main")
        );
    }

    private static string Env(string key, string defaultValue)
    {
        return Environment.GetEnvironmentVariable(key) ?? defaultValue;
    }
}
