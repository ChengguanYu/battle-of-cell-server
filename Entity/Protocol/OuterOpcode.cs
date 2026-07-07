// ReSharper disable InconsistentNaming
namespace Fantasy
{
    /// <summary>
    /// 外部消息 OpCode，手动维护
    /// Index 从 10001 开始分配
    /// </summary>
    public static partial class OuterOpcode
    {
        public const uint C2G_LoginRequest = 285222673;
        public const uint G2C_LoginResponse = 419440401;
    }
}