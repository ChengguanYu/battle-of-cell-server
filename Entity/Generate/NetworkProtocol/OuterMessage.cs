using LightProto;
using System;
using MemoryPack;
using System.Collections.Generic;
using Fantasy;
using Fantasy.Pool;
using Fantasy.Network.Interface;
using Fantasy.Serialize;

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618
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
namespace Fantasy
{
    /// <summary>
    /// 客户端进家园请求
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class EntryHomeReq : AMessage, IRequest
    {
        public static EntryHomeReq Create(bool autoReturn = true)
        {
            var entryHomeReq = MessageObjectPool<EntryHomeReq>.Rent();
            entryHomeReq.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                entryHomeReq.SetIsPool(false);
            }
            
            return entryHomeReq;
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
            token = default;
            MessageObjectPool<EntryHomeReq>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.EntryHomeReq; } 
        [ProtoIgnore]
        public EntryHomeRes ResponseType { get; set; }
        [ProtoMember(1)]
        public string token { get; set; }
    }
    /// <summary>
    /// 客户端进家园响应
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class EntryHomeRes : AMessage, IResponse
    {
        public static EntryHomeRes Create(bool autoReturn = true)
        {
            var entryHomeRes = MessageObjectPool<EntryHomeRes>.Rent();
            entryHomeRes.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                entryHomeRes.SetIsPool(false);
            }
            
            return entryHomeRes;
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
            MessageObjectPool<EntryHomeRes>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.EntryHomeRes; } 
        [ProtoMember(2)]
        public uint ErrorCode { get; set; }
        [ProtoMember(1)]
        public bool ok { get; set; }
    }
}