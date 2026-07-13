using LightProto;
using MemoryPack;
using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using Fantasy;
using Fantasy.Pool;
using Fantasy.Network.Interface;
using Fantasy.Serialize;

// ReSharper disable InconsistentNaming
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable RedundantTypeArgumentsOfMethod
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable PreferConcreteValueOverDefault
// ReSharper disable RedundantNameQualifier
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable CheckNamespace
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable RedundantUsingDirective
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618
namespace Fantasy
{
    /// <summary>
    /// 玩家进入请求
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class PlayerEntryReq : AMessage, IRequest
    {
        public static PlayerEntryReq Create(bool autoReturn = true)
        {
            var playerEntryReq = MessageObjectPool<PlayerEntryReq>.Rent();
            playerEntryReq.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                playerEntryReq.SetIsPool(false);
            }
            
            return playerEntryReq;
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
            userId = default;
            MessageObjectPool<PlayerEntryReq>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.PlayerEntryReq; } 
        [ProtoIgnore]
        public PlayerEntryResp ResponseType { get; set; }
        [ProtoMember(1)]
        public long userId { get; set; }
    }
    /// <summary>
    /// 玩家进入响应
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class PlayerEntryResp : AMessage, IResponse
    {
        public static PlayerEntryResp Create(bool autoReturn = true)
        {
            var playerEntryResp = MessageObjectPool<PlayerEntryResp>.Rent();
            playerEntryResp.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                playerEntryResp.SetIsPool(false);
            }
            
            return playerEntryResp;
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
            ok = default;
            MessageObjectPool<PlayerEntryResp>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.PlayerEntryResp; } 
        [ProtoMember(2)]
        public uint ErrorCode { get; set; }
        [ProtoMember(1)]
        public bool ok { get; set; }
    }
}