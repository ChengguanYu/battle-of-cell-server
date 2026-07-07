using MemoryPack;
using Fantasy;
using Fantasy.Network.Interface;

#pragma warning disable CS8618
#pragma warning disable CS8625

namespace Fantasy.Protocol;

// ==================================================================
// 协议实体（MemoryPack 序列化字段）
// ==================================================================

[MemoryPackable]
public partial class G2C_LoginResponse
{
    [MemoryPackOrder(0)]
    public string Token { get; set; }

    [MemoryPackOrder(1)]
    public uint ErrorCode { get; set; }
}

// ==================================================================
// Framework 兼容（Fantasy 接口绑定）
// ==================================================================

public partial class G2C_LoginResponse : AMessage, IResponse
{
    public uint OpCode() => OuterOpcode.G2C_LoginResponse;
    public void Dispose() { Token = default; ErrorCode = 0; }
}
