using MemoryPack;
using Fantasy;
using Fantasy.Network.Interface;

#pragma warning disable CS8618
#pragma warning disable CS8625
// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace

namespace Fantasy.Protocol
{
    [MemoryPackable]
    public partial class C2G_LoginRequest : AMessage, IRequest
    {
        public uint OpCode() => OuterOpcode.C2G_LoginRequest;

        [MemoryPackIgnore]
        public G2C_LoginResponse ResponseType { get; set; }

        [MemoryPackOrder(0)]
        public string Account { get; set; }

        [MemoryPackOrder(1)]
        public string Password { get; set; }

        public void Dispose()
        {
            Account = default;
            Password = default;
        }
    }

    [MemoryPackable]
    public partial class G2C_LoginResponse : AMessage, IResponse
    {
        public uint OpCode() => OuterOpcode.G2C_LoginResponse;

        [MemoryPackOrder(0)]
        public string Token { get; set; }

        [MemoryPackOrder(1)]
        public uint ErrorCode { get; set; }

        public void Dispose()
        {
            Token = default;
            ErrorCode = 0;
        }
    }
}
