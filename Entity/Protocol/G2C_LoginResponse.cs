using MemoryPack;

#pragma warning disable CS8618

namespace Fantasy.Protocol;

/// <summary>
/// 网关 → 客户端 登录响应
/// </summary>
[MemoryPackable]
public partial class G2C_LoginResponse
{
    [MemoryPackOrder(0)]
    public string Token { get; set; }

    [MemoryPackOrder(1)]
    public uint ErrorCode { get; set; }
}
