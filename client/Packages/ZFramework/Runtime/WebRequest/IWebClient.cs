using System;
using System.Collections.Generic;
namespace ZLib
{
    /// <summary>
    /// 网络加载器
    /// </summary>
    public interface IWebClient : IDisposable
    {
        Dictionary<string, string> responseHeader { get; set; }
        int Timeout { get; set; }
        string FilePath { get; set; }
        string Error { get; }
        /// <summary>
        /// 停止加载器
        /// </summary>
        void Stop();

        /// <summary>
        /// 启动请求
        /// </summary>
        void StartRequest(WebRequestType type);

        /// <summary>
        /// 请求到的数据
        /// </summary>
        /// <returns></returns>
        object GetData();

        long ContentLength();
        long LoadBytesSize();
        int ResponseCode();
        bool IsDone();
        float Progress();
    }
}