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
[GenerateTypeScript]
public partial class LoginResponse
{
    [MemoryPackOrder(0)]
    public string Token { get; set; }

    [MemoryPackOrder(1)]
    public uint ErrorCode { get; set; }
}

// ==================================================================
// Framework 兼容（Fantasy 接口绑定）
// ==================================================================

public partial class LoginResponse : AMessage, IResponse
{
    public uint OpCode() { return OuterOpcode.LoginResponse; }
    public void Dispose() { Token = default; ErrorCode = 0; }
}
