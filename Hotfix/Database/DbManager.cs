using SqlSugar;

namespace Hotfix.Database;

public static class DbManager
{
    private static SqlSugarScope? _db;
    private static readonly object _lock = new();

    public static SqlSugarScope GetInstance()
    {
        if (_db != null) return _db;

        lock (_lock)
        {
            if (_db != null) return _db;

            var config = DbConfig.LoadFromEnv();
            _db = CreateConnection(config);
        }

        return _db;
    }

    private static SqlSugarScope CreateConnection(DbConfig config)
    {
        var connectionString = $"Host={config.Host};Port={config.Port};Database={config.Database};Username={config.User};Password={config.Password}";

        return new SqlSugarScope(new ConnectionConfig
        {
            DbType = DbType.PostgreSQL,
            ConnectionString = connectionString,
            IsAutoCloseConnection = true
        });
    }
}
