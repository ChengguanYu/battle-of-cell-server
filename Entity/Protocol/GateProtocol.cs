using System;
using MemoryPack;
using Fantasy;
using Fantasy.Pool;
using Fantasy.Network.Interface;

#pragma warning disable CS8625
#pragma warning disable CS8618
// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace

namespace Fantasy
{
    [Serializable]
    [MemoryPackable]
    public partial class C2G_LoginRequest : AMessage, IRequest
    {
        public static C2G_LoginRequest Create(bool autoReturn = true)
        {
            var request = MessageObjectPool<C2G_LoginRequest>.Rent();
            request.AutoReturn = autoReturn;

            if (!autoReturn)
            {
                request.SetIsPool(false);
            }

            return request;
        }

        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return;
            Account = default;
            Password = default;
            MessageObjectPool<C2G_LoginRequest>.Return(this);
        }

        public uint OpCode() => OuterOpcode.C2G_LoginRequest;

        [MemoryPackIgnore]
        public G2C_LoginResponse ResponseType { get; set; }

        [MemoryPackOrder(0)]
        public string Account { get; set; }

        [MemoryPackOrder(1)]
        public string Password { get; set; }
    }

    [Serializable]
    [MemoryPackable]
    public partial class G2C_LoginResponse : AMessage, IResponse
    {
        public static G2C_LoginResponse Create(bool autoReturn = true)
        {
            var response = MessageObjectPool<G2C_LoginResponse>.Rent();
            response.AutoReturn = autoReturn;

            if (!autoReturn)
            {
                response.SetIsPool(false);
            }

            return response;
        }

        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return;
            ErrorCode = 0;
            Token = default;
            MessageObjectPool<G2C_LoginResponse>.Return(this);
        }

        public uint OpCode() => OuterOpcode.G2C_LoginResponse;

        [MemoryPackOrder(0)]
        public string Token { get; set; }

        [MemoryPackOrder(1)]
        public uint ErrorCode { get; set; }
    }
}
