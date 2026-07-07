using MemoryPack;

#pragma warning disable CS8618

namespace Fantasy.Protocol;

/// <summary>
/// 客户端 → 网关 登录请求
/// </summary>
[MemoryPackable]
public partial class C2G_LoginRequest
{
    [MemoryPackOrder(0)]
    public string Account { get; set; }

    [MemoryPackOrder(1)]
    public string Password { get; set; }
}
