using SqlSugar;
using Entity.Models;

namespace Entity.Database;

public static class DatabaseMigrator
{
    /// <summary>
    /// 启动时完整迁移入口：确保数据库存在 → 初始化表结构。
    /// 失败时抛出异常，阻止服务器启动。
    /// </summary>
    public static void Initialize()
    {
        Console.WriteLine("[Migration] 开始执行数据库迁移...");
        var config = DbConfig.LoadFromEnv();
        EnsureDatabase(config);
        InitTables(DbManager.GetInstance());
        Console.WriteLine("[Migration] 数据库迁移完成");
    }

    /// <summary>
    /// 检查目标数据库是否存在，不存在则创建。
    /// 必须连接 postgres 库来执行此操作。
    /// </summary>
    public static void EnsureDatabase(DbConfig config)
    {
        var bootstrapConn = $"Host={config.Host};Port={config.Port};Database=postgres;Username={config.User};Password={config.Password}";

        using var client = new SqlSugarClient(new ConnectionConfig
        {
            DbType = DbType.PostgreSQL,
            ConnectionString = bootstrapConn,
            IsAutoCloseConnection = true
        });

        if (!client.Ado.IsValidConnection())
        {
            throw new Exception($"无法连接到数据库服务器 {config.Host}:{config.Port}");
        }

        var exists = client.Ado.SqlQuerySingle<int>(
            "SELECT COUNT(1) FROM pg_database WHERE datname = @db",
            new { db = config.Database }) > 0;

        if (!exists)
        {
            Console.WriteLine($"[Migration] 数据库 '{config.Database}' 不存在，正在创建...");
            client.Ado.ExecuteCommand($"CREATE DATABASE \"{config.Database}\"");
            Console.WriteLine($"[Migration] 数据库 '{config.Database}' 创建成功");
        }
        else
        {
            Console.WriteLine($"[Migration] 数据库 '{config.Database}' 已存在，跳过创建");
        }
    }

    /// <summary>
    /// 在已连接的客户端上初始化表结构。
    /// </summary>
    public static void InitTables(ISqlSugarClient client)
    {
        Console.WriteLine("[Migration] 开始初始化表结构...");
        client.CodeFirst.InitTables(typeof(User));
        Console.WriteLine("[Migration] 表结构初始化完成");
    }
}
