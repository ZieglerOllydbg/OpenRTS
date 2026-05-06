using System;
using System.IO;
using UnityEngine;
namespace ZLib
{
    /// <summary>
    /// 网络加载器，负责执行一个Request，管理Request生命周期,并将执行结果存入Request。
    /// 
    /// 在处理文件的时候，总是先临时保存到temp里面去，真正下载完毕了才会走到File.Move
    /// 避免了外面逻辑下次根据文件是否存在来做判断的问题
    /// </summary>
    public class WebLoadThread
    {
        public static string DOWNLOAD_PATH = "";
        internal const int RETRY_TIME = 2;
        internal const int FREEZ_CHECK_FRAME = 10;
        internal const int FREEZ_CHECK_SPEED = 10240;
        internal static long TotalDownloadSize = 0;
        internal static long DownloadSpeed;

        /// <summary>
        /// 固定的资源服务器，此服务资源使用固定的HttpClient下载
        /// </summary>
        public static string ASSIGN_FILE_SERVER = null;

        /// <summary>
        /// cdn尾巴，用于刷新cdn
        /// </summary>
        private static string CDNPostfix;

        /// <summary>
        /// 注册处理结果回调
        /// </summary>
        internal Action<WebAsyncRequest> OnResultCallback;

        /// <summary>
        /// 是否正在运行
        /// </summary>
        internal bool Runing { get; private set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        internal float StartTime { get; private set; }

        /// <summary>
        /// 当前状态
        /// </summary>
        internal WebRequestState CurState { get; private set; }

        private WebAsyncRequest curRequest;
        private IWebClient curHandle;
        private float timeout;
        private int retryTime;
        private string fileSavePath;
        private float lastProgress;
        private int freezcheckTime;
        private bool loadLocal;

        /// <summary>
        /// 下载文件的大小
        /// </summary>
        private long curFileSize;
        /// <summary>
        /// 上次检查时计算文件的大小
        /// </summary>
        private long lastCountSize;
        private long lastFreezCheckSize;

        private int tickCount = 0;

        /// <summary>
        /// 临时文件后缀名
        /// </summary>
        const string tempFileExtension = ".tmp";

        /// <summary>
        /// 临时存储的文件目录
        /// </summary>
        private string fileSavePathTemp;

        private bool unityWebApi;
        /// <summary>
        /// 启动加载器
        /// </summary>
        public void Start(WebAsyncRequest _request, float _timeout)
        {
            curRequest = _request;
            timeout = _timeout;
            StartTime = Time.time;
            retryTime = RETRY_TIME;
            fileSavePath = null;
            fileSavePathTemp = null;
            Runing = true;
            unityWebApi = true;

            lastProgress = 0.0f;
            freezcheckTime = FREEZ_CHECK_FRAME;
            loadLocal = !curRequest.Url.StartsWith("http");

            var isDownloadFile = IsDownloadFile(curRequest.Type);
            if (isDownloadFile)
                retryTime = RETRY_TIME << 1;

            //用户设置了重试次数，这里只处理非无限重试的情况，无限重试交给Manager处理。
            if (curRequest.ExParam != null && curRequest.ExParam.retryTime > -1)
                retryTime = curRequest.ExParam.retryTime;


            StartRequest();
        }

        /// <summary>
        /// 停止加载器
        /// </summary>
        public void Stop(string errorMsg = null)
        {
            if (curRequest == null)
                return;
            if (curRequest.State == WebRequestState.cancel)
            {
                curRequest.Error = "canceled..";
            }
            else if (errorMsg != null)
            {
                curRequest.Error = errorMsg;
                curRequest.ChangeState(WebRequestState.error);
            }
            else if (curRequest.Error != null)
            {
                curRequest.ChangeState(WebRequestState.error);
            }

            if (!loadLocal)
                TotalDownloadSize += curFileSize - lastCountSize;

            retryTime = 0;
            curFileSize = 0;
            lastCountSize = 0;

            if (curHandle != null)
            {
                curRequest.ResponseCode = (int)curHandle.ResponseCode();
                curRequest.ResponseHeader = curHandle.responseHeader;
                // if (curRequest.State != WebRequestState.done)
                //    this.LogError($"web request error:{curRequest.Error} code:{ curRequest.ResponseCode}");

                curHandle.Stop();
                curHandle = null;
            }
            WebAsyncRequest request = curRequest;
            curRequest = null;
            Runing = false;

            var tempResultCallBack = OnResultCallback;

            OnResultCallback = null;

            if (tempResultCallBack != null)
                tempResultCallBack(request);
        }

        private void Restart(string errorMsg)
        {
            if (curHandle != null)
            {
                this.LogError($"web request Restart:{curRequest.Url} error:{errorMsg}  webapi:{unityWebApi}");

                curHandle.Stop();
                curHandle = null;
            }
            //StartRequest();
            var tick = PlayerMainLoop.TickManager().CreateInternal();
            tick.SetTimeout(StartRequest, 1.0f);
            //Tick.AddDelayCallback(StartRequest, 1.0f);
        }


        /// <summary>
        /// 检查状态，是否cancel 、更新进度 、检查下载进度超时
        /// </summary>
        public void CheckState()
        {
            if (curRequest != null && curHandle != null)
            {
                bool isDone = curHandle.IsDone();

                if (curRequest.State == WebRequestState.cancel)
                {
                    Stop();
                }
                else if (isDone)
                {
                    ResolveResult();
                }
                else
                {
                    /*if(curHandle.timeout != 0)
                    {
                        freezcheckTime--;
                        if (freezcheckTime < 0)
                        {
                            freezcheckTime = FREEZ_CHECK_FRAME;
                            if (Mathf.Abs(curHandle.downloadProgress - lastProgress) < 0.0001f)
                            {
                                this.LogError("Webload Freez: " + curRequest.Url);
                                curRequest.Error = "web request error: " + curHandle.error;
                                curRequest.ChangeState(WebRequestState.error);
                                Stop();
                                return;
                            }
                            lastProgress = curHandle.downloadProgress;
                        }
                    }*/

                    if (curRequest.Type == WebRequestType.file)
                    {
                        curRequest.ContentLength = (int)curHandle.ContentLength();

                        curRequest.LoadBytesSize = (int)curHandle.LoadBytesSize();
                    }

                    curRequest.SetProgress(curHandle.Progress());
                }

            }
        }

        /// <summary>
        /// 启动请求
        /// </summary>
        void StartRequest()
        {
            curFileSize = 0;
            lastCountSize = 0;
            lastFreezCheckSize = 0;
            curRequest.ResponseCode = 0;

            var url = curRequest.Url;
            curHandle = null;
            var isDownloadFile = IsDownloadFile(curRequest.Type);

            if (isDownloadFile && CDNPostfix != null && (curRequest.ExParam == null || curRequest.ExParam.Method == null))
                url += CDNPostfix;

            try
            {
                if ((isDownloadFile && retryTime == 1 && !string.IsNullOrEmpty(ASSIGN_FILE_SERVER) && url.StartsWith(ASSIGN_FILE_SERVER)) || (curRequest.ExParam != null && curRequest.ExParam.clientType == HttpClientType.CSharp))
                //if(true)
                {
                    this.LogWarning("start DotNetHttpClient: " + url);
                    unityWebApi = false;
                    curHandle = new DotNetHttpClient(url, curRequest.ExParam);
                }
                else
                {
                    unityWebApi = true;
                    curHandle = new UnityWebClient(url, curRequest.ExParam);
                }
            }
            catch (Exception ex)
            {
                this.LogError(ex);
                curRequest.Error = ex.ToString();
                curRequest.Data = null;
                curRequest.ChangeState(WebRequestState.error);
                curRequest.GetPromise();
                curRequest.Finished = true;
                return;
            }


            if (isDownloadFile)
            {
                fileSavePath = ReadyDownloadPath();
                fileSavePathTemp = fileSavePath + tempFileExtension;
                ResolvePath(fileSavePathTemp);
                curHandle.FilePath = fileSavePathTemp;
            }

            curHandle.Timeout = ReadyTimeout();
            curHandle.StartRequest(curRequest.Type);

            curRequest.ChangeState(WebRequestState.processing);
        }


        /// <summary>
        /// 准备自动超时
        /// </summary>
        /// <param name="handle"></param>
        int ReadyTimeout()
        {
            int setTimeout = Mathf.CeilToInt(timeout);
            bool paramSet = false;
            if (curRequest.ExParam != null && curRequest.ExParam.timeOut > 0)
            {
                paramSet = true;
                setTimeout = curRequest.ExParam.timeOut;
            }

            if (paramSet || curRequest.Type == WebRequestType.text || curRequest.Type == WebRequestType.bytes || curRequest.Type == WebRequestType.texture)
                return setTimeout;


            return 0;
        }

        /// <summary>
        /// 准备下载路径
        /// </summary>
        private string ReadyDownloadPath()
        {
            if (curRequest == null)
                return "emptyName";

            string dir = DOWNLOAD_PATH;

            if (curRequest.ExParam != null && !string.IsNullOrEmpty(curRequest.ExParam.DownloadDir))
                dir = curRequest.ExParam.DownloadDir;

            if (!Directory.Exists(dir))
            {
                throw new Exception($"不存在保存路径 路径：{dir}");
            }

            string fileName = Path.GetFileName(curRequest.PrivateKey);
            if (curRequest.ExParam != null && !string.IsNullOrEmpty(curRequest.ExParam.FileName))
                fileName = curRequest.ExParam.FileName;

            string path = dir + fileName;
            if (curRequest.ExParam == null || curRequest.ExParam.startPos == 0)
            {
                ResolvePath(path);
            }

            return path;
        }

        /// <summary>
        /// 是否成功的把路径问题解决了
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private bool ResolvePath(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception e)
            {
                this.LogError("Exception: WebLoadThread Delete file error: " + path + "\r\n" + e.Message);
                Debug.LogException(e);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 处理加载结果
        /// </summary>
        private void ResolveResult()
        {
            var error = curHandle.Error;//check network error
            var code = curHandle.ResponseCode();
            if (code == 304)// HTTP/1.1 304 Not Modified
            {
                curRequest.Data = curHandle.GetData();
                curRequest.ChangeState(WebRequestState.done);
                Stop();
                return;
            }

            if (error == null)
                error = CheckResult(); //check logic error
            if (error != null)
            {
                if (retryTime > 0 && code < 300)
                {
                    retryTime--;
                    Restart(error);
                    // StartRequest();
                }
                else
                {

                    curRequest.Data = curHandle.GetData();
                    this.LogError("Exception: web request error: " + error + " @" + curRequest.Url + " webapi:" + unityWebApi + "code:" + curHandle.ResponseCode());
                    Stop("web request error: " + error);
                }
                return;
            }
            else if (curRequest.State != WebRequestState.cancel)
            {
                if (IsDownloadFile(curRequest.Type))
                {
                    ResolvePath(fileSavePath);
                    if (curHandle != null)
                    {
                        fileSavePathTemp = curHandle.GetData() as string;
                        curHandle.Dispose();
                    }
                    try
                    {
                        File.Move(fileSavePathTemp, fileSavePath);
                    }
                    catch (Exception e)
                    {
                        if (retryTime > 0)
                        {
                            retryTime--;
                        }
                        else
                        {
                            Debug.LogException(e);
                            this.LogError("Exception: file move error" + fileSavePathTemp + " to " + fileSavePath + e.Message);
                            Stop("file move error " + e.Message);
                        }
                        return;
                    }
                    if (!File.Exists(fileSavePath))
                    {
                        if (retryTime > 0)
                        {
                            retryTime--;
                        }
                        else
                        {
                            Stop("Exception: download file not exist, make error...");
                        }
                        return;
                    }

                    curRequest.Data = fileSavePath;
                }
                else
                {
                    curRequest.Data = curHandle.GetData();
                }


                curRequest.ChangeState(WebRequestState.done);
                Stop();
            }
        }

        /// <summary>
        /// 下载文件的类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsDownloadFile(WebRequestType type)
        {
            switch (type)
            {
                case WebRequestType.file:
                case WebRequestType.backGroundFile:
                case WebRequestType.audio:
                case WebRequestType.bundle:
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 检查结果
        /// </summary>
        /// <returns></returns>
        private string CheckResult()
        {
            if (IsDownloadFile(curRequest.Type))
            {
                if (!File.Exists(fileSavePathTemp))
                {
                    return "web request Exists error: " + fileSavePathTemp;
                }

                var fi = new FileInfo(fileSavePathTemp);
                if (!fi.Exists)
                {
                    return "web request fileInfo Exists error: " + fileSavePathTemp;
                }
                curFileSize = fi.Length;

                long totalLength = curHandle.ContentLength();

                if (totalLength <= 0)
                {
                    if (loadLocal)
                        return null;

                    return "web request ContentLength error: " + totalLength;
                }



                bool sizeMatch = (curFileSize == totalLength);
                if (!sizeMatch)
                    return $"web request sizeMatch error: {curFileSize} / {totalLength} @{fileSavePathTemp}";

                return null;
            }

            return null;
        }

        /// <summary>
        /// 获取当前下载文件大小
        /// </summary>
        /// <returns></returns>
        private long DownloadFileSize()
        {
            if (curHandle == null)
                return 0;

            return (long)curHandle.LoadBytesSize();

            /* if (string.IsNullOrEmpty(fileSavePathTemp))
                 return 0;


             var fi = new FileInfo(fileSavePathTemp);
             if(fi.Exists)
                 return fi.Length;

             return 0;*/
        }

        public void OnSecondTick()
        {
            if (curHandle == null)
                return;


            long fileSize = DownloadFileSize();
            // this.LogWarning($"size {fileSize} / {lastCountSize}");
            if (curHandle.Timeout == 0) //&& unityApi
            {
                //没有超时的任务要检查卡死
                freezcheckTime--;
                if (freezcheckTime < 0)
                {
                    freezcheckTime = FREEZ_CHECK_FRAME;

                    if (fileSize == lastFreezCheckSize && DownloadSpeed < FREEZ_CHECK_SPEED)
                    {

                        if (retryTime > 0)
                        {
                            retryTime--;
                            Restart("freez load: " + fileSize);
                            // StartRequest();
                        }
                        else
                        {
                            this.LogError("Exception: web request freez:" + curRequest.Url + " webapi:" + unityWebApi);
                            Stop("request in freez." + fileSize);
                        }

                        return;
                    }
                    lastFreezCheckSize = fileSize;
                }
            }

            if (fileSize != 0)
            {
                if (!loadLocal)
                    TotalDownloadSize += (fileSize - curFileSize);
                lastCountSize = fileSize;
                curFileSize = fileSize;
            }
        }

        /// <summary>
        /// cdn后缀，通常用于刷新cdn
        /// </summary>
        /// <param name="postfix"></param>
        public static void SetCDNPostfix(string postfix)
        {
            CDNPostfix = postfix;
        }

    }
}