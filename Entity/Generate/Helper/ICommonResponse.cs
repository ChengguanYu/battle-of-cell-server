using System.Collections.Generic;
using Fantasy;

namespace Entity.Generate.Helper;

/// <summary>
/// 该接口实现了基本响应的字段，供拓展类添加逻辑
/// 如果需要让协议支持拓展类:<br/>
/// 1. 应该满足存在 Meta\RespError<br/>
/// 2. 在useCommonResp.cs 中 手动的把生成的类继承本接口
/// </summary>
public interface ICommonResponse
{
    MetaData meta { get; set; }
    List<RespError> error { get; set; }
}


