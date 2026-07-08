using Fantasy.Network.Interface;
using MemoryPack;
using Fantasy.Protocol;

namespace Fantasy.Protocol;

[MemoryPackable]
[GenerateTypeScript]
public partial class EntryHomeReq : AMessage, IRequest
{
    [MemoryPackOrder(0)]
    public string token { get; set; }

    [MemoryPackIgnore]
    public EntryHomeRes ResponseType { get; set; }
    public uint OpCode() { return OuterOpcode.EntryHomeReq; }
    public void Dispose() { token = default; }
}

[MemoryPackable]
[GenerateTypeScript]
public partial class EntryHomeRes : AMessage, IResponse
{
    [MemoryPackOrder(0)]
    public bool ok { get; set; }

    [MemoryPackOrder(1)]
    public uint ErrorCode { get; set; }
    public uint OpCode() { return OuterOpcode.EntryHomeRes; }
    public void Dispose() { ok = default; ErrorCode = 0; }
}
