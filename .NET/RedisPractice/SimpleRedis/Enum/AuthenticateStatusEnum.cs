using System.ComponentModel;

namespace SimpleRedis.Enum
{
    /// <summary>
    /// Redis服务器认证状态枚举
    /// </summary>
    public enum AuthenticateStatusEnum
    {
        /// <summary>
        /// 未认证
        /// </summary>
        [Description("未认证")]
        Unauthenticated = 0,

        /// <summary>
        /// 认证中
        /// </summary>
        [Description("认证中")]
        Authenticating = 1,

        /// <summary>
        /// 已认证
        /// </summary>
        [Description("已认证")]
        Authenticated = 2,

        /// <summary>
        /// 认证失败
        /// </summary>
        [Description("认证失败")]
        AuthenticationFailed = 3
    }
}