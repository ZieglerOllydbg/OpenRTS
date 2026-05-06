using System;
using UnityEngine;
namespace ZLib
{
    /// <summary>
    /// 异步操作，类似 AsyncOperation 可以用来 await/yield return
    /// </summary>
    public class WebAsyncOption : CustomYieldInstruction, IAwaitable<WebAsyncOption, object>, IAwaiter<object>
    {
        /// <summary>
        /// 当前操作产生的请求
        /// </summary>
        public WebAsyncRequest Request { get; private set; }

        /// <summary>
        /// 完成回调和错误回调
        /// </summary>
        private Action<WebAsyncOption> onComplete;
        private Action<WebAsyncOption> onError;

        /// <summary>
        /// 附加数据
        /// </summary>
        private object additionData;

        /// override CustomYieldInstruction
        public override bool keepWaiting
        {
            get
            {
                return Request != null && !Request.Finished;
            }
        }

        public bool IsCompleted => Request != null && Request.Finished;

        public WebAsyncOption(WebAsyncRequest _request)
        {
            Request = _request;
            Request.Grab(OnRequestDone);
        }

        /// <summary>
        /// 取消本次操作
        /// </summary>
        public void Cancel()
        {
            Request.Drop(OnRequestDone);
            Request = null;
        }

        private void OnRequestDone()
        {
            DoCallback();
        }

        /// <summary>
        /// 附加数据
        /// </summary>
        public void SetAddition(object data)
        {
            additionData = data;
        }

        /// <summary>
        /// 附加数据
        /// </summary>
        public object GetAddition()
        {
            return additionData;
        }

        /// <summary>
        /// 获取Promise
        /// </summary>
        public ZLib.Promises.IPromise<object> GetPromise()
        {
            if (Request != null)
                return Request.GetPromise();
            return null;
        }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string GetError()
        {
            if (Request == null)
                return "Request Cenceled.";
            else if (Request.State != WebRequestState.done || !string.IsNullOrEmpty(Request.Error))
                return Request.Url + " error: " + Request.Error + " state:" + Request.State;

            return null;
        }

        /// <summary>
        /// 获取结果数据
        /// </summary>
        public T GetData<T>()
        {
            if (Request == null || Request.State == WebRequestState.error)
                return default(T);

            return (T)Request.Data;
        }

        /// <summary>
        /// 获取ResponseCode
        /// </summary>
        /// <returns></returns>
        public int GetCode()
        {
            if (Request != null)
                return Request.ResponseCode;

            return 0;
        }

        /// <summary>
        /// 获取进度
        /// </summary>
        /// <returns></returns>
        public float GetProgress()
        {

            if (Request != null)
                return Request.Progress;
            return 0;
        }

        /// <summary>
        /// 添加回调
        /// </summary>
        public WebAsyncOption AddCallback(Action<WebAsyncOption> _onComplete, Action<WebAsyncOption> _onError)
        {
            onComplete += _onComplete;
            onError += _onError;

            if (Request != null && Request.Finished)
            {
                DoCallback();
            }
            return this;
        }

        private void DoCallback()
        {
            try
            {
                if (Request.State == WebRequestState.done)
                {
                    var tempOnComplete = onComplete;
                    onComplete = null;

                    if (tempOnComplete != null)
                        tempOnComplete(this);
                }
                else if (onError != null)
                {
                    var tempOnError = onError;
                    onError = null;

                    tempOnError(this);
                }

                callBackAction?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public object GetResult()
        {
            return Request.Data;
        }

        private Action callBackAction;

        public void OnCompleted(Action continuation)
        {
            if (IsCompleted)
            {
                continuation();
            }
            else
            {
                callBackAction += continuation;
            }
        }

        public WebAsyncOption GetAwaiter()
        {
            return this;
        }
    }
}