using System.ComponentModel;

namespace SimpleRedis.Enum
{
    /// <summary>
    /// TcpClient连接状态枚举
    /// </summary>
    public enum TcpConnectStatusEnum
    {
        /// <summary>
        /// 未连接
        /// </summary>
        [Description("未连接")]
        Ununited = 0,

        /// <summary>
        /// 连接中
        /// </summary>
        [Description("连接中")]
        Connecting = 1,

        /// <summary>
        /// 已连接
        /// </summary>
        [Description("已连接")]
        Connected = 2
    }
}
