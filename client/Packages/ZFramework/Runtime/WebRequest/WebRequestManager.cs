using System;
using System.Collections.Generic;
using UnityEngine;
namespace ZLib
{
    /// <summary>
    /// 支持Web请求相关的API
    /// 上传、下载
    /// 同时 本网络请求管理类，支持数据、文件两个请求队列，支持重复请求合并、取消。
    /// 通常用法：
    /// var optA = WebRequestManager.instance.Load("https://www.baidu.com", WebRequestType.text);
    /// yield return optA;
    /// string text = optA.Request.Data as string;
    /// 
    /// OR 一行:
    /// WebRequestManager.instance.Load("https://www.baidu.com", WebRequestType.text).GetPromise().Then((text) => { }).Catch((e) => { });
    /// 
    /// optA：每次调用Load都会创建一个异步‘操作’，可以被Cancel及查询状态、进度，重复的‘操作’会被指向同一个Request。
    /// optA.Request：一个实际的‘请求’，如操作被Cancel，则此操作的请求指向空，当一个‘请求’没有被任何‘操作’引用，则取消该请求。
    /// optA.Request.Data：这里存储请求回来的数据，会根据WebRequestType不同，填入不同的数据，如file类型的请求会填入文件路径。
    /// 
    /// 
    /// //TODO WebRequestManager : 按运行时间插队 / 请求完成缓存一段时间 / 断点续传 / 切片下载
    /// </summary>
    public class WebRequestManager
    {
        private static WebRequestManager _instance;

        public static WebRequestManager instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new WebRequestManager();
                }
                return _instance;
            }
        }

        public delegate string CustomDnsResolver(string url, out string host);
        const int MAX_DATA_THREAD = 10;
        const int MAX_FILE_THREAD = 5;
        const int BACKGROUND_FILE_THREAD = 1;
        const int RETRY_INTERVAL = 5;
        static int GID = 0;

        private List<WebLoadThread> dataThreadList;
        private List<WebLoadThread> fileThreadList;
        private List<WebLoadThread> backgroundThreadList;

        private Dictionary<string, WebAsyncRequest> dataRequestList;
        private Dictionary<string, WebAsyncRequest> fileRequestList;
        private Dictionary<string, WebAsyncRequest> backgroundList;
        private List<WebAsyncRequest> reTryList;

        private Action<int> webErrorCallback;

        private bool inClearing = false;
        private float timeout = 10.0f;
        private string cdnPostfix;

        private int retryTime;

        public int TickCount { get; private set; }
        private int tickId;

        private CustomDnsResolver dnsResolver;

        private ExternalWebloadThread backgroundDownloader;

        private InternalPlayerLoop playerLoop;

        private InternalTick tick;

        private int loopID;

        public WebRequestManager()
        {
            dataThreadList = new List<WebLoadThread>(MAX_DATA_THREAD);
            fileThreadList = new List<WebLoadThread>(MAX_FILE_THREAD);
            backgroundThreadList = new List<WebLoadThread>(BACKGROUND_FILE_THREAD);

            dataRequestList = new Dictionary<string, WebAsyncRequest>();
            fileRequestList = new Dictionary<string, WebAsyncRequest>();
            backgroundList = new Dictionary<string, WebAsyncRequest>();
            reTryList = new List<WebAsyncRequest>();

            inClearing = false;
            retryTime = RETRY_INTERVAL;
            playerLoop = PlayerMainLoop.PlayerLoopManager().CreateInternal();
            loopID = playerLoop.SetUpdate(Update);
            //Tick.AddUpdate(Update);
            tick = PlayerMainLoop.TickManager().CreateInternal();
            ResetTickCallback();

            // tickId = Tick.AddCallback(OnSecondTick, 1.0f, 0);
        }

        /// <summary>
        /// 设置超时时间
        /// </summary>
        /// <param name="_time"></param>
        public void SetTimeout(float _time)
        {
            timeout = _time;
        }

        /// <summary>
        /// 注册 custom DNS
        /// </summary>
        /// <param name="_dnsResolver"></param>
        public void SetDnsResolver(CustomDnsResolver _dnsResolver)
        {
            dnsResolver = _dnsResolver;
        }

        private string ResolveDns(string _url, ref WebRequestExParam _exParam)
        {
            if (dnsResolver != null && _url.StartsWith("http") && (_exParam == null || !_exParam.disableCustomDns))
            {
                string host = null;
                _url = dnsResolver(_url, out host);
                if (!string.IsNullOrEmpty(host))
                {
                    if (_exParam == null)
                        _exParam = new WebRequestExParam();
                    if (_exParam.Head == null)
                        _exParam.Head = new Dictionary<string, string>();
                    _exParam.Head["host"] = host;
                }
            }
            return _url;
        }

        /// <summary>
        /// 注册后台下载插件，分配 WebRequestType.backGroundFile 任务
        /// </summary>
        public void RegistBackgroundDownloader(ExternalWebloadThread downloader)
        {
            backgroundDownloader = downloader;
            if (backgroundDownloader != null)
                backgroundDownloader.OnResultCallback = OnRequestResult;
        }

        /// <summary>
        /// 创建一个网络加载请求
        /// </summary>
        /// <param name="_url">加载url</param>
        /// <param name="_type">加载类型</param>
        /// <param name="_exParam">选填额外参数，包括协议头，下载路径等</param>
        /// <returns></returns>
        public WebAsyncOption Load(string _url, WebRequestType _type, WebRequestExParam _exParam = null)
        {
            inClearing = false;
            string privateKey = _url;
            if (_exParam != null && _exParam.Method != null && !_exParam.Method.ToLower().Equals("get"))
                privateKey = privateKey + (GID++);
            else if (_exParam != null && !string.IsNullOrEmpty(_exParam.FileName) && !_url.EndsWith(_exParam.FileName))
                privateKey = privateKey + _exParam.FileName; //url相同但文件不同的情况需要区分（如以md5为下载路径时复制了文件）

            WebAsyncRequest request = FindRequest(privateKey); //找到空余的可以加载的队列
            if (request == null)
            {
                _url = ResolveDns(_url, ref _exParam);

                request = new WebAsyncRequest(privateKey, _url, _type, _exParam);
                SelectRequestList(request.Type)[request.PrivateKey] = request;
            }

            if (WebLoadThread.IsDownloadFile(_type) && IsBGResolved(request, _type))
            {
                SwitchRequestType(request, WebRequestType.backGroundFile);
            }
            else if (NeedSwitchType(request, _type) && request.State != WebRequestState.processing)
            {
                //请求类型不一致，需切换并重新放入请求队列
                SwitchRequestType(request, _type);
            }

            NotifyRequest(request, true);

            WebAsyncOption option = new WebAsyncOption(request);

            return option;
        }

        /// <summary>
        /// 切换请求类型，通常由后台请求切换到前台请求
        /// </summary>
        private void SwitchRequestType(WebAsyncRequest request, WebRequestType _type)
        {
            if (request.Type == _type)
                return;

            SelectRequestList(request.Type).Remove(request.PrivateKey);
            request.ResetType(_type);
            SelectRequestList(request.Type)[request.PrivateKey] = request;
        }

        /// <summary>
        /// 是否需要切换为新的请求类型，通常是后台请求切换为前台请求
        /// </summary>
        private bool NeedSwitchType(WebAsyncRequest request, WebRequestType next)
        {
            if (request.Type == WebRequestType.backGroundFile && next == WebRequestType.file)
                return true;

            return false;
        }

        /// <summary>
        /// 检查请求是否可以在后台下载器处理
        /// </summary>
        /// <returns>true: 请求已经被处理，false:需要继续处理</returns>
        private bool IsBGResolved(WebAsyncRequest request, WebRequestType type)
        {
            if (backgroundDownloader == null)
                return false;

            var status = backgroundDownloader.CheckStatus(request);

            if (status == ExternalWebloadThread.Status.Done)
            {
                return true;
            }

            if (status == ExternalWebloadThread.Status.Error || (status == ExternalWebloadThread.Status.Pading && type != WebRequestType.backGroundFile))
            {
                //如果下载错误，或者正在后台下载，应该转交到前台下载器
                backgroundDownloader.DropRequest(request);
                request.ChangeState(WebRequestState.waiting);
            }

            return false;
        }

        /* public void Abort(WebAsyncOption option)
         {
             WebAsyncRequest request = option.Request;
             if (request == null)
                 return;
             option.Cancel();
             if(request.State == )
             var list = SelectThreadList(request.Type);
             foreach (var thread in list)
             {
                 if (thread.WorkingWith(request))
                 {
                     thread.Stop();
                     break;
                 }
             }

             request.ChangeState(WebRequestState.cancel);
             OnRequestResult(request);
         }*/

        /// <summary>
        /// 查找请求
        /// </summary>
        private WebAsyncRequest FindRequest(string _key)
        {
            WebAsyncRequest request = null;
            if (backgroundList.TryGetValue(_key, out request))
                return request;

            if (fileRequestList.TryGetValue(_key, out request))
                return request;

            if (dataRequestList.TryGetValue(_key, out request))
                return request;

            for (int i = reTryList.Count - 1; i >= 0; i--)
            {
                var errorRequest = reTryList[i];
                if (errorRequest.PrivateKey.Equals(_key))
                {
                    this.LogWarning("reset error: " + errorRequest.PrivateKey);
                    errorRequest.ResetType(errorRequest.Type);
                    reTryList.Remove(errorRequest);
                    return errorRequest;
                }
            }

            return request;
        }

        /// <summary>
        /// 根据类型选择一个请求队列
        /// </summary>
        private Dictionary<string, WebAsyncRequest> SelectRequestList(WebRequestType _type)
        {
            if (_type == WebRequestType.backGroundFile)
                return backgroundList;
            else if (_type == WebRequestType.text || _type == WebRequestType.bytes)
                return dataRequestList;
            else
                return fileRequestList;
        }

        /// <summary>
        /// 根据类型选择对应的加载器队列
        /// </summary>
        private List<WebLoadThread> SelectThreadList(WebRequestType _type)
        {
            if (_type == WebRequestType.backGroundFile)
                return backgroundThreadList;
            else if (_type == WebRequestType.text || _type == WebRequestType.bytes)
                return dataThreadList;
            else
                return fileThreadList;
        }

        /// <summary>
        /// 发起一个执行提醒，如果队列中有空闲执行器，则立即开始请求。
        /// </summary>
        private void NotifyRequest(WebAsyncRequest request, bool forceCut)
        {
            if (request.State == WebRequestState.done || request.State == WebRequestState.cancel)
            {
                OnRequestResult(request);
            }
            else if (request.State != WebRequestState.processing)
            {
                request.Finished = false;
                if (request.Type == WebRequestType.backGroundFile && backgroundDownloader != null)
                {
                    backgroundDownloader.StartRequest(request);
                }
                else
                {
                    WebLoadThread thread = GetFreeThread(request, forceCut);
                    if (thread != null)
                        thread.Start(request, timeout);
                }

            }
        }

        /// <summary>
        /// 发起一个请求提醒，试图从请求队列选取一个请求，然后提醒执行器是否可以开始请求。
        /// </summary>
        private void NotifyRequest(WebRequestType _type)
        {
            if (_type == WebRequestType.backGroundFile && fileRequestList.Count > 0)
                return;//当有前台下载任务时，后台任务暂停执行。

            if (_type == WebRequestType.file && fileRequestList.Count == 0)
                _type = WebRequestType.backGroundFile; // 当前台任务下载完毕，通知后台下载任务。

            var request = GetFreeRequest(_type);
            if (request != null)
            {
                NotifyRequest(request, false);
            }
        }

        /// <summary>
        /// 获取一个未被执行的请求
        /// </summary>
        private WebAsyncRequest GetFreeRequest(WebRequestType _type)
        {
            var dictionary = this.SelectRequestList(_type);
            WebAsyncRequest webAsyncRequest1 = null;
            foreach (WebAsyncRequest webAsyncRequest2 in dictionary.Values)
            {
                if (webAsyncRequest2.State != WebRequestState.processing)
                {
                    if (webAsyncRequest1 == null || !webAsyncRequest2.IsNegative)
                        webAsyncRequest1 = webAsyncRequest2;
                    if (!webAsyncRequest1.IsNegative)
                        break;
                }
            }
            return webAsyncRequest1;
        }

        /// <summary>
        /// 试图获取一个执行器
        /// </summary>
        private WebLoadThread GetFreeThread(WebAsyncRequest request, bool forceCut)
        {
            List<WebLoadThread> list = SelectThreadList(request.Type);
            if (list.Count < list.Capacity)
            {
                WebLoadThread thread = new WebLoadThread();
                thread.OnResultCallback = OnRequestResult;
                list.Add(thread);
                return thread;
            }
            else
            {
                //TODO WebLoadThread GetThread forceCut
                foreach (var thread in list)
                {
                    if (!thread.Runing)
                    {
                        thread.OnResultCallback = OnRequestResult;
                        return thread;
                    }
                }
            }
            return null;
        }



        /// <summary>
        /// 执行器执行结果处理
        /// </summary>
        private void OnRequestResult(WebAsyncRequest request)
        {
            if (!inClearing)
            {
                SelectRequestList(request.Type).Remove(request.PrivateKey);
                NotifyRequest(request.Type);
            }

            bool isError = (request.State == WebRequestState.error);
            //bool isFile = WebLoadThread.IsDownloadFile(request.Type);
            int retryTime = -1; //默认无限重试
            if (request.ExParam != null) //用户有自定义重试次数
                retryTime = request.ExParam.retryTime;

            if (isError && request.ResponseCode < 300 && retryTime == -1) //&& isFile
            {
                this.Log("Request need retry :" + request.PrivateKey);
                reTryList.Add(request);
            }
            else
            {
                //这里有回调，要保证在最后一行。。
                request.Finished = true;
            }
        }

        /// <summary>
        /// 这里检查各个请求的状态是否被cancel，主要为了方便使用者调用opt.Cancel
        /// //TODO 或许不用每帧检查
        /// </summary>
        public void Update()
        {
            for (int i = dataThreadList.Count - 1; i >= 0; i--)
            {
                WebLoadThread thread = dataThreadList[i];
                if (thread != null)
                    thread.CheckState();
            }

            for (int i = fileThreadList.Count - 1; i >= 0; i--)
            {
                WebLoadThread thread = fileThreadList[i];
                if (thread != null)
                    thread.CheckState();
            }

            for (int i = backgroundThreadList.Count - 1; i >= 0; i--)
            {
                WebLoadThread thread = backgroundThreadList[i];
                if (thread != null)
                    thread.CheckState();
            }
        }

        /// <summary>
        /// 停止所有请求
        /// </summary>
        public void StopAllRequest()
        {
            inClearing = true;

            //取消所有请求
            this.Log("stop dataRequestList " + dataRequestList.Count);
            var tList = new List<WebAsyncRequest>(dataRequestList.Values);
            for (int i = tList.Count - 1; i >= 0; i--)
            {
                WebAsyncRequest request = tList[i];
                request.Cancel();
            }

            this.Log("stop fileRequestList " + fileRequestList.Count);
            tList = new List<WebAsyncRequest>(fileRequestList.Values);
            for (int i = tList.Count - 1; i >= 0; i--)
            {
                WebAsyncRequest request = tList[i];
                request.Cancel();
            }

            this.Log("stop background RequestList " + backgroundList.Count);
            tList = new List<WebAsyncRequest>(backgroundList.Values);
            for (int i = tList.Count - 1; i >= 0; i--)
            {
                WebAsyncRequest request = tList[i];
                request.Cancel();
            }
            for (int i = reTryList.Count - 1; i >= 0; i--)
            {
                WebAsyncRequest request = reTryList[i];
                request.Cancel();
            }
            reTryList.Clear();

            //立即停掉所有加载器
            for (int i = dataThreadList.Count - 1; i >= 0; i--)
            {
                WebLoadThread thread = dataThreadList[i];
                if (thread != null)
                    thread.Stop();
            }

            for (int i = fileThreadList.Count - 1; i >= 0; i--)
            {
                WebLoadThread thread = fileThreadList[i];
                if (thread != null)
                    thread.Stop();
            }

            for (int i = backgroundThreadList.Count - 1; i >= 0; i--)
            {
                WebLoadThread thread = backgroundThreadList[i];
                if (thread != null)
                    thread.Stop();
            }

            inClearing = false;
        }

        public List<string> DebugRequestList(bool onlyData)
        {
            List<string> requestList = new List<string>();
            this.Log("dataRequestList " + dataRequestList.Count);

            foreach (var request in dataRequestList.Values)
            {
                requestList.Add(request.Url);
            }

            if (onlyData)
                return requestList;

            this.Log(" fileRequestList " + fileRequestList.Count);
            foreach (var request in fileRequestList.Values)
            {
                requestList.Add(request.Url);
            }

            this.Log(" background RequestList " + backgroundList.Count);
            foreach (var request in backgroundList.Values)
            {
                requestList.Add(request.Url);
            }

            return requestList;
        }


        public float GetDownloadTaskCount(bool withBackground)
        {
            if (withBackground)
                return GetTaskCount(backgroundList) + GetTaskCount(fileRequestList);
            else
                return GetTaskCount(fileRequestList);
        }

        private float GetTaskCount(Dictionary<string, WebAsyncRequest> list)
        {
            float count = 0;
            foreach (var request in list.Values)
            {
                if (request.State == WebRequestState.waiting)
                    count += 1.0f;
                else
                    count += 1.0f - request.Progress;
            }
            return count;
        }

        /// <summary>
        /// 加载速度 byte/s
        /// </summary>
        /// <returns></returns>
        public int LoadSpeed()
        {
            return downloadSpeed;
        }

        /// <summary>
        /// 析构
        /// </summary>
        public void Release()
        {
            StopAllRequest();
            playerLoop.ClearUpdate(loopID);
            //Tick.RemoveUpdate(Update);
        }

        public int GetCurRequestCount()
        {
            return fileRequestList.Count + dataRequestList.Count + backgroundList.Count;
        }

        public void RegistWebErrorCallback(Action<int> errorCallback)
        {
            webErrorCallback = errorCallback;
        }

        long lastDownloadSize = 0;
        int downloadSpeed = 0;

        public void ResetTickCallback()
        {
            Debug.Log($" ## ResetTickCallback id:{tickId} count:{TickCount}");

            if (tickId != 0)
                tick.ClearInterval(tickId);

            tickId = tick.SetInterval(OnSecondTick, 1.0f);


            //if (tickId != 0)
            //    Tick.RemoveCallback(tickId);

            //tickId = Tick.AddCallback(OnSecondTick, 1.0f, 0);
            //if (tickId == 0)
            //{
            //    Tick.RemoveCallback(tickId);
            //    tickId = Tick.AddCallback(OnSecondTick, 1.0f, 0);
            //}
        }
        private void OnSecondTick()
        {
            TickCount++;
            for (int i = dataThreadList.Count - 1; i >= 0; i--)
            {
                WebLoadThread thread = dataThreadList[i];
                if (thread != null)
                    thread.OnSecondTick();
            }

            for (int i = fileThreadList.Count - 1; i >= 0; i--)
            {
                WebLoadThread thread = fileThreadList[i];
                if (thread != null)
                    thread.OnSecondTick();
            }

            for (int i = backgroundThreadList.Count - 1; i >= 0; i--)
            {
                WebLoadThread thread = backgroundThreadList[i];
                if (thread != null)
                    thread.OnSecondTick();
            }

            downloadSpeed = (int)(WebLoadThread.TotalDownloadSize - lastDownloadSize);
            WebLoadThread.DownloadSpeed = downloadSpeed;

            lastDownloadSize = WebLoadThread.TotalDownloadSize;

            retryTime--;
            if (retryTime < 0)
            {
                if (reTryList.Count > 0 && webErrorCallback != null)
                {
                    try
                    {
                        webErrorCallback(reTryList.Count);
                    }
                    catch (Exception e)
                    {
                        this.LogError("webErrorCallback error");
                        Debug.LogException(e);
                    }
                }

                retryTime = RETRY_INTERVAL;
                RetryErrorRequest();

            }
        }

        void RetryErrorRequest()
        {
            if (reTryList.Count > 0)
            {
                foreach (var request in reTryList)
                {
                    this.LogWarning("webRetry: " + request.PrivateKey);
                    request.ResetType(request.Type);
                    SelectRequestList(request.Type)[request.PrivateKey] = request;
                    NotifyRequest(request, false);
                }
                reTryList.Clear();
            }
        }
    }
}
