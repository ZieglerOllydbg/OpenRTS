using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
namespace ZLib
{
    /// <summary>
    /// Unity 网络加载器
    /// </summary>
    public class UnityWebClient : IWebClient
    {

        public int Timeout { get; set; }
        public string FilePath { get; set; }
        public string Error { get; private set; }
        public Dictionary<string, string> responseHeader { get; set; }

        private WebRequestType curRequestType;
        private UnityWebRequest curHandle;
        private WebRequestExParam Param;

        private long totalLength;


        public UnityWebClient(string url, WebRequestExParam param)
        {
            totalLength = 0;
            curHandle = new UnityWebRequest(url);
            Param = param;
            ReadyParam(param);
        }


        /// <summary>
        /// 启动请求
        /// </summary>
        public void StartRequest(WebRequestType type)
        {
            curHandle.timeout = Timeout;
            curRequestType = type;

            switch (type)
            {
                case WebRequestType.text:
                case WebRequestType.bytes:
                    curHandle.downloadHandler = new DownloadHandlerBuffer();
                    break;
                case WebRequestType.file:
                case WebRequestType.audio:
                case WebRequestType.bundle:
                case WebRequestType.backGroundFile:
                    var fileHandler = new DownloadHandlerFile(FilePath);
                    fileHandler.removeFileOnAbort = true; //TODO: 断点续传时要处理这里
                    curHandle.downloadHandler = fileHandler;
                    break;
                case WebRequestType.texture:

                    curHandle.downloadHandler = new DownloadHandlerTexture(true);
                    break;

                    //TODO WebRequestType.bundle
                    // case WebRequestType.bundle:
                    //     curHandle.downloadHandler = new DownloadHandlerAssetBundle(curHandle.url, 0);
                    //     break;
            }

            var opt = curHandle.SendWebRequest();
            //opt.priority = curPriority; //诡异的变量，测试后无效。
            //ResolveResult();
        }

        /// <summary>
        /// 准备请求参数
        /// </summary>
        void ReadyParam(WebRequestExParam exParam)
        {
            curHandle.method = UnityWebRequest.kHttpVerbGET;
            if (exParam != null)
            {
                if (exParam.Head != null)
                {
                    foreach (var p in exParam.Head)
                    {
                        curHandle.SetRequestHeader(p.Key, p.Value);
                    }
                }

                if (!string.IsNullOrEmpty(exParam.Method))
                {
                    curHandle.method = exParam.Method;
                }
                if (!string.IsNullOrEmpty(exParam.UploadData))
                {
                    //  Debug.LogWarning($"send web data: {exParam.UploadData} to:{curRequest.Url}");
                    byte[] postBytes = System.Text.Encoding.UTF8.GetBytes(exParam.UploadData);
                    curHandle.uploadHandler = new UploadHandlerRaw(postBytes);
                }
                //Add by LLL,2018.11.16
                if (exParam.upLoadBytes != null)
                {
                    byte[] upLoadBytes = exParam.upLoadBytes;
                    curHandle.uploadHandler = new UploadHandlerRaw(upLoadBytes);
                }
                if (exParam.formDatas != null)
                {
                    byte[] payload = null;
                    if (exParam.formDatas != null && exParam.formDatas.Count != 0)
                        payload = UnityWebRequest.SerializeSimpleForm(exParam.formDatas);

                    UploadHandler formUploadHandler = new UploadHandlerRaw(payload);
                    formUploadHandler.contentType = "application/x-www-form-urlencoded";
                    curHandle.uploadHandler = formUploadHandler;
                }
            }

            if (curHandle.url.StartsWith("https"))
            {
                curHandle.certificateHandler = new WebRequestCert();
            }
        }



        /// <summary>
        /// 处理加载结果
        /// </summary>
        public object GetData()
        {
            object data = null;
            switch (curRequestType)
            {
                case WebRequestType.text:
                    data = curHandle.downloadHandler.text;
                    break;
                case WebRequestType.bytes:
                    data = curHandle.downloadHandler.data;
                    break;
                case WebRequestType.file:
                case WebRequestType.audio:
                case WebRequestType.bundle:
                case WebRequestType.backGroundFile:
                    data = FilePath;
                    break;
                case WebRequestType.texture:
                    if (ResponseCode() == 200)
                    {
                        data = DownloadHandlerTexture.GetContent(curHandle);
                    }
                    break;
            }

            if (Param != null && Param.ResponseHeader != null)
            {
                responseHeader = new Dictionary<string, string>();
                foreach (var header in Param.ResponseHeader)
                    responseHeader[header] = curHandle.GetResponseHeader(header);
            }

            return data;
        }

        public long ContentLength()
        {
            if (totalLength != 0)
                return totalLength;

            var lengthStr = curHandle.GetResponseHeader("Content-Length");
            if (long.TryParse(lengthStr, out totalLength))
                return totalLength;
            else
                return 0;
        }
        /// <summary>
        /// 获取当前下载文件大小
        /// </summary>
        /// <returns></returns>
        public long LoadBytesSize()
        {
            if (curHandle == null)
                return 0;

            return (long)curHandle.downloadedBytes;

            /* if (string.IsNullOrEmpty(fileSavePathTemp))
                 return 0;


             var fi = new FileInfo(fileSavePathTemp);
             if(fi.Exists)
                 return fi.Length;

             return 0;*/
        }

        public int ResponseCode()
        {
            return (int)curHandle.responseCode;
        }

        public bool IsDone()
        {
            bool isDone = curHandle.isDone;
            if (isDone)
            {
                if (curHandle.result == UnityWebRequest.Result.ConnectionError || curHandle.error != null)
                {
                    Error = "unity web error: " + curHandle.error;
                    string debugStr = "";
                    debugStr += $"isNetworkError : {curHandle.result}";
                    debugStr += $"  Error : {curHandle.error}";
                    debugStr += $"  Content-Length : {ContentLength()}";
                    debugStr += $"  downloadedBytes : {LoadBytesSize()}";
                    debugStr += $"  progress : {Progress()}";
                    debugStr += $"  respondCode : {ResponseCode()}";
                    //  var data = curHandle.downloadHandler.data == null ? "null" : "" + curHandle.downloadHandler.data.Length;
                    //  debugStr += $"  downloadHandler.data.length : {data}";
                    Debug.LogWarning(debugStr);
                }
            }
            return isDone;
        }
        public float Progress()
        {
            return curHandle.downloadProgress;
        }

        /// <summary>
        /// 停止加载器
        /// </summary>
        public void Stop()
        {
            Dispose();
            if (curHandle != null)
            {
                curHandle.Abort();
                curHandle.Dispose();
                curHandle = null;
            }
        }

        public void Dispose()
        {
            if (curHandle != null)
            {
                if (curHandle.downloadHandler != null)
                    curHandle.downloadHandler.Dispose();
            }

        }


        public class WebRequestCert : UnityEngine.Networking.CertificateHandler
        {
            protected override bool ValidateCertificate(byte[] certificateData)
            {
                //return base.ValidateCertificate(certificateData);
                return true;
            }
        }
    }
}