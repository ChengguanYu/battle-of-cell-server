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
public partial class C2G_LoginRequest
{
    [MemoryPackOrder(0)]
    public string Account { get; set; }

    [MemoryPackOrder(1)]
    public string Password { get; set; }
}

// ==================================================================
// Framework 兼容（Fantasy 接口绑定）
// ==================================================================

public partial class C2G_LoginRequest : AMessage, IRequest
{
    [MemoryPackIgnore]
    public G2C_LoginResponse ResponseType { get; set; }

    public uint OpCode() { return OuterOpcode.C2G_LoginRequest; }
    public void Dispose() { Account = default; Password = default; }
}
