using System.ComponentModel;

namespace SimpleRedis.Enum
{
    /// <summary>
    /// TCP连接状态枚举值
    /// </summary>
    public enum TcpConnectStatusEnum
    {
        /// <summary>
        /// 未连接
        /// </summary>
        [Description("未连接")]
        Disconnected = 0,

        /// <summary>
        /// 连接中
        /// </summary>
        [Description("连接中")]
        Connecting = 1,

        /// <summary>
        /// 连接成功
        /// </summary>
        [Description("连接成功")]
        Connected = 2,

        /// <summary>
        /// 连接失败
        /// </summary>
        [Description("连接失败")]
        ConnectionFailed = 3
    }
}