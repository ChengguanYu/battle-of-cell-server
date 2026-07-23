/***
 * 本文件的作用是让自动生成的协议类手动继承 ICommonResponse
 * 以便添加拓展方法
 */
using Entity.Generate.Helper;

namespace Fantasy
{
    public partial class EntryHomeResp : ICommonResponse
    {
    }

    public partial class PlayerMatchResp : ICommonResponse
    {
    }

    public partial class PlayerLeaveRoomResp : ICommonResponse
    {
    }
}