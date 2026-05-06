using System;
using System.Collections.Generic;
using UnityEngine;
using ZLib.Promises;

namespace ZLib
{
    /// <summary>
    /// 一个web请求，包含请求信息/请求状态/返回数据
    /// 可以有多个WebAsyncOption异步操作引用一个请求
    /// 请求完成后，所有引用这个请求的WebAsyncOption均标记为完成，并可以从这里获取返回数据
    /// 当所有引用这个请求的WebAsyncOption操作取消后，请求也自动取消
    /// </summary>
    public class WebAsyncRequest : IDisposable
    {

        /// <summary>
        /// 唯一标识
        /// </summary>
        public string PrivateKey { get; private set; }
        /// <summary>
        /// 请求地址
        /// </summary>
        public string Url { get; private set; }

        /// <summary>
        /// 请求到的数据，下载文件时为文件路径
        /// </summary>
        public object Data { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// 返回码
        /// </summary>
        public int ResponseCode { get; set; }

        /// <summary>
        /// 加载进度
        /// </summary>
        public float Progress { get; private set; }

        public Dictionary<string, string> ResponseHeader { get; set; }

        public bool IsNegative { get; private set; }

        /// <summary>
        /// 总的可下载文件尺寸.
        /// 只是在下载模式才可以. 非文件下载模式返回0.
        /// </summary>
        public int ContentLength { get; internal set; }

        /// <summary>
        /// 当前已经下载的文件尺寸
        /// 只是在下载模式才可以. 非文件下载模式返回0.
        /// </summary>
        public int LoadBytesSize { get; internal set; }

        private bool _finished;
        /// <summary>
        /// 请求是否结束
        /// </summary>
        public bool Finished
        {
            get { return _finished; }
            set
            {
                _finished = value;
                if (_finished)
                    NotifyResult();
            }
        }

        /// <summary>
        /// 请求创建时间
        /// </summary>
        public float CreateTime { get; private set; }

        /// <summary>
        /// 请求引用计数
        /// </summary>
        public int Reference { get; private set; }

        /// <summary>
        /// 请求的数据类型
        /// </summary>
        public WebRequestType Type { get; private set; }

        /// <summary>
        /// 请求的状态
        /// </summary>
        public WebRequestState State { get; private set; }

        /// <summary>
        /// 请求额外参数
        /// </summary>
        public WebRequestExParam ExParam { get; private set; }

        /// <summary>
        /// promise 
        /// </summary>
        private Promise<object> promise;

        /// <summary>
        /// 完成请求后通知对象
        /// </summary>
        private event Action Observers;

        public WebAsyncRequest(string key, string _url, WebRequestType _type, WebRequestExParam _exParam)
        {
            PrivateKey = key;
            Url = _url;
            Type = _type;
            Data = null;
            Error = null;
            CreateTime = Time.time;
            Reference = 0;
            _finished = false;
            State = WebRequestState.waiting;
            ExParam = _exParam;
            this.CheckPriority(_exParam);
        }
        public void CheckPriority(WebRequestExParam _exParam)
        {
            if (_exParam == null)
                return;
            this.IsNegative = _exParam.IsNegative;
        }

        /// <summary>
        /// 请求状态更改
        /// </summary>
        public void ChangeState(WebRequestState _state)
        {
            State = _state;
        }

        /// <summary>
        /// 切换请求类型，往往用于后台下载切换到前台下载
        /// </summary>
        /// <param name="type"></param>
        public void ResetType(WebRequestType type)
        {
            Error = null;
            ChangeState(WebRequestState.waiting);
            Type = type;
        }

        public void SetProgress(float progress)
        {
            Progress = progress;
            if (promise != null && promise.CurState == PromiseState.Pending)
                promise.ReportProgress(Progress);
        }

        /// <summary>
        /// 获取引用
        /// </summary>
        public void Grab(Action callback)
        {
            Reference++;
            Observers += callback;
            if (State == WebRequestState.cancel)
            {
                ChangeState(WebRequestState.waiting);
                Finished = false;
            }
        }


        /// <summary>
        /// 取消请求
        /// </summary>
        public void Cancel()
        {
            ChangeState(WebRequestState.cancel);
            Finished = true;
        }

        /// <summary>
        /// 释放引用
        /// </summary>
        public void Drop(Action callback)
        {
            Reference--;
            Observers -= callback;
            if (Reference == 0)
            {
                Cancel();
            }
        }

        public void Dispose()
        {
            PrivateKey = null;
            Url = null;
            Data = null;
        }

        /// <summary>
        /// 通知结果
        /// </summary>
        void NotifyResult()
        {
            try
            {
                var tempObservers = Observers;

                Observers = null;

                if (tempObservers != null)
                {
                    tempObservers();
                }

                if (promise != null && promise.CurState == PromiseState.Pending)
                {
                    if (State == WebRequestState.done)
                        promise.Resolve(Data);
                    else
                        promise.Reject(new PromiseException("state: " + State + "  error: " + Error, (int)State));
                    //promise = null;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                this.LogError("web request NotifyResult error: " + e.Message);
            }

        }

        /// <summary>
        /// 包装为Promise
        /// </summary>
        public IPromise<object> GetPromise()
        {
            if (promise == null)
                promise = new Promise<object>();

            return promise;
        }
    }

    /// <summary>
    /// 数据请求额外参数，可以选填协议头，下载路径等
    /// </summary>
    public class WebRequestExParam
    {
        /// <summary>
        /// 自定义请求http头
        /// </summary>
        public Dictionary<string, string> Head;

        /// <summary>
        /// 需要返回的http头
        /// </summary>
        public List<string> ResponseHeader;

        /// <summary>
        /// 请求方法，默认GET 选项 POST PUT..
        /// </summary>
        public string Method;

        /// <summary>
        /// 上传数据
        /// </summary>
        public string UploadData;

        /// <summary>
        /// 自定义下载路径
        /// </summary>
        public string DownloadDir;

        /// <summary>
        /// 自定义文件名称
        /// </summary>
        public string FileName;

        /// <summary>
        /// 切片/断点开始位置
        /// </summary>
        public int startPos;

        /// <summary>
        /// 切片结束位置
        /// </summary>
        public int endPos;

        /// <summary>
        /// 上传数据
        /// </summary>
        public byte[] upLoadBytes;

        /// <summary>
        /// 表单数据
        /// </summary>
        public Dictionary<string, string> formDatas;

        /// <summary>
        /// 超时时间 秒
        /// </summary>
        public int timeOut;

        /// <summary>
        /// 设定重试次数，默认-1
        /// </summary>
        public int retryTime = -1;

        /// <summary>
        /// 关闭本地dns解析
        /// </summary>
        public bool disableCustomDns;

        public bool IsNegative;

        public int VerifySize;

        public HttpClientType clientType;
    }

    /// <summary>
    /// Http客户端类型
    /// </summary>
    public enum HttpClientType
    {
        Auto,
        Unity,
        CSharp,
    }


    /// <summary>
    /// 请求状态枚举
    /// </summary>
    public enum WebRequestState
    {
        waiting,
        processing,
        cancel,
        error,
        done
    }

    /// <summary>
    /// 请求类型枚举
    /// </summary>
    public enum WebRequestType
    {
        text,
        bytes,
        texture,
        bundle,
        audio,
        file,
        backGroundFile,
    }
}