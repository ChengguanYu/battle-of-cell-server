// ================================================================================
// Fantasy.Net 服务器应用程序入口
// ================================================================================
// 本文件是 Fantasy.Net 分布式游戏服务器的主入口点
//
// 初始化流程：
//   1. 加载 .env 环境变量文件
//   2. 强制加载引用程序集，触发 ModuleInitializer 执行
//   3. 创建 NLog 日志实例
//   4. 执行数据库迁移（创建/检查数据库和表结构）
//   5. 启动 Fantasy.Net 框架（传入 NLog 日志实例）
// ================================================================================

using DotNetEnv;
using Fantasy;
using Hotfix.Database;
using Main.Database;

// 加载 .env 文件（从可执行文件所在目录）
var envPath = Path.Combine(AppContext.BaseDirectory, ".env");
if (File.Exists(envPath))
{
    Env.Load(envPath);
}

// 检查 MACHINE_ID 环境变量，未配置则拒绝启动
var machineId = Environment.GetEnvironmentVariable("MACHINE_ID");
if (string.IsNullOrWhiteSpace(machineId))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine("致命错误：未检测到 MACHINE_ID，请在 .env 中配置 MACHINE_ID=1 (或其他唯一编号)");
    Console.ResetColor();
    Environment.Exit(1);
    return;
}

try
{
    // 初始化引用的程序集，确保 ModuleInitializer 执行
    // .NET 采用延迟加载机制 - 仅当类型被引用时才加载程序集
    // 通过访问 AssemblyMarker 强制加载程序集并调用 ModuleInitializer
    // 注意：Native AOT 不存在延迟加载问题，所有程序集在编译时打包
    AssemblyHelper.Initialize();

    // 创建 NLog 日志实例（迁移前创建，确保错误输出有日志着色）
    var logger = new Fantasy.NLog("Server");

    // 执行数据库迁移（创建/检查数据库和表结构）
    // 在 Fantasy 框架启动前完成，失败则阻止服务器启动
    DatabaseMigrator.Initialize();

    // 启动 Fantasy.Net 框架（使用 NLog）
    await Fantasy.Platform.Net.Entry.Start(logger);
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine($"服务器初始化过程中发生致命错误：{ex}");
    Console.ResetColor();
    Environment.Exit(1);
}
