using System;
namespace ZLib
{
    /// <summary>
    /// 外部实现的网络请求线程，如：下载插件。
    /// </summary>
    public abstract class ExternalWebloadThread
    {
        public enum Status
        {
            None,
            Pading,
            Done,
            Error
        }

        /// <summary>
        /// 完成通知
        /// </summary>
        public Action<WebAsyncRequest> OnResultCallback;

        /// <summary>
        /// 检查请求状态
        /// </summary>
        public abstract Status CheckStatus(WebAsyncRequest _request);

        /// <summary>
        /// 启动请求
        /// </summary>
        public abstract void StartRequest(WebAsyncRequest _request);

        /// <summary>
        /// 立即丢弃请求，由管理器交接给其他处理线程。
        /// </summary>
        public abstract void DropRequest(WebAsyncRequest _request);
    }
}