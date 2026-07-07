using MemoryPack;
using Fantasy;
using Fantasy.Network.Interface;

#pragma warning disable CS8618
#pragma warning disable CS8625

namespace Fantasy.Protocol;

// Fantasy 框架兼容 partial：补充 IRequest/IResponse、OpCode、Dispose
// 协议实体类定义在独立的 .cs 文件中，保持纯 MemoryPack 字段

public partial class C2G_LoginRequest : AMessage, IRequest
{
    [MemoryPackIgnore]
    public G2C_LoginResponse ResponseType { get; set; }

    public uint OpCode() => OuterOpcode.C2G_LoginRequest;
    public void Dispose() { Account = default; Password = default; }
}

public partial class G2C_LoginResponse : AMessage, IResponse
{
    public uint OpCode() => OuterOpcode.G2C_LoginResponse;
    public void Dispose() { Token = default; ErrorCode = 0; }
}
