using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
namespace ZLib
{
    /// <summary>
    /// .net 网络加载器
    /// </summary>
    public class DotNetHttpClient : IWebClient
    {

        static HttpClient Client = new HttpClient();
        public int Timeout { get; set; }
        public string FilePath { get; set; }
        public string Error { get; private set; }
        public Dictionary<string, string> responseHeader { get; set; }

        private WebRequestType curRequestType;

        private long totalLength;
        private long loadBytesSize;
        private string Url;
        private WebRequestExParam Param;
        private readonly string Method = "GET";
        private byte[] uploadBytes;
        private float progress;
        private object resultData;
        private Uri curUri;
        private HttpResponseMessage httpResponse;
        private bool isDone;
        private int statusCode;
        private CancellationTokenSource cancelSource;
        Stream fileStream;
        public DotNetHttpClient(string url, WebRequestExParam param) : base()
        {
            isDone = false;
            totalLength = 0;
            loadBytesSize = 0;
            progress = 0.0f;
            Url = url;
            curUri = new Uri(Url);
            Param = param;
            statusCode = 0;
            cancelSource = new CancellationTokenSource();
        }



        /// <summary>
        /// 启动请求
        /// </summary>
        public async void StartRequest(WebRequestType type)
        {
            curRequestType = type;
            statusCode = 0;

            switch (type)
            {

                case WebRequestType.file:
                case WebRequestType.backGroundFile:
                case WebRequestType.audio:
                case WebRequestType.bundle:
                    {
                        try
                        {
                            var dir = Path.GetDirectoryName(FilePath);
                            if (!Directory.Exists(dir))
                            {
                                Directory.CreateDirectory(dir);
                            }

                            httpResponse = await Client.GetAsync(curUri, HttpCompletionOption.ResponseHeadersRead, cancelSource.Token);
                            if (cancelSource.IsCancellationRequested)
                            {
                                if (httpResponse != null)
                                {
                                    httpResponse.Dispose();
                                    httpResponse = null;
                                }
                                return;
                            }
                            Download();
                        }
                        catch (Exception e)
                        {
                            Error = e.ToString();
                            //if (e.InnerException != null)
                            //     Error += " @ " + e.InnerException.Message;
                        }

                    }
                    break;
                default:
                    Error = "httpClient cant resolve " + type;
                    this.LogError(Error);
                    break;

            }
            //opt.priority = curPriority; //诡异的变量，测试后无效。
            //ResolveResult();
        }


        async void Download()
        {
            statusCode = (int)httpResponse.StatusCode;

            if (!httpResponse.IsSuccessStatusCode)
            {
                Error = httpResponse.ReasonPhrase;
                return;
            }

            var headers = httpResponse.Content.Headers;
            if (headers.ContentLength != null)
                totalLength = headers.ContentLength.Value;

            if (Param != null && Param.ResponseHeader != null)
            {
                responseHeader = new Dictionary<string, string>();
                foreach (var head in Param.ResponseHeader)
                {
                    if (headers.TryGetValues(head, out var values) && values != null)
                        foreach (var hValue in values)
                        {
                            if (!string.IsNullOrEmpty(hValue))
                            {
                                responseHeader[head] = hValue;
                                break;
                            }
                        }
                }
            }

            /* using (var sourceStream = await httpResponse.Content.ReadAsStreamAsync())//.ConfigureAwait(false);
             {
                 using (var reader = new StreamReader(sourceStream))
                 {
                     return reader.ReadToEnd();
                 }
             }*/

            try
            {
                fileStream = new FileStream(FilePath, FileMode.OpenOrCreate, FileAccess.Write);
                await httpResponse.Content.CopyToAsync(fileStream);
            }
            finally
            {
                Dispose();
                isDone = true;
            }

            // fileStream = new FileStream(FilePath, FileMode.Create, FileAccess.Write);
            // await this.SaveAsAsync(fileStream, cancelSource.Token).ConfigureAwait(false);

            /* using (var fileStream = new FileStream(FilePath, FileMode.OpenOrCreate, FileAccess.Write))
             {
                 await this.SaveAsAsync(fileStream, cancelSource.Token).ConfigureAwait(false);
             }*/

        }



        public async Task SaveAsAsync(Stream stream, CancellationToken cancellationToken)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }
            if (stream.CanWrite == false)
            {
                throw new ArgumentException(nameof(stream) + " cannot be write", nameof(stream));
            }
            var buffer = new byte[8 * 1024];
            var sourceStream = await httpResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);

            while (true)
            {
                var length = await sourceStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var isCompleted = (length == 0 && sourceStream.Length == totalLength);

                loadBytesSize = loadBytesSize + length;
                var total = totalLength;
                if (isCompleted == true && total > 0)
                {
                    total = loadBytesSize;
                }

                progress = (float)loadBytesSize / total;

                if (isCompleted == true)
                {
                    break;
                }
                await stream.WriteAsync(buffer, 0, length, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 处理加载结果
        /// </summary>
        public object GetData()
        {
            return FilePath;
        }

        public long ContentLength()
        {
            return totalLength;
        }
        /// <summary>
        /// 获取当前下载文件大小
        /// </summary>
        /// <returns></returns>
        public long LoadBytesSize()
        {
            return loadBytesSize;
        }

        public int ResponseCode()
        {
            return statusCode;
        }

        public bool IsDone()
        {
            return Error != null || isDone;
        }
        public float Progress()
        {
            if (fileStream != null && !isDone)
            {
                loadBytesSize = fileStream.Length;
                progress = (float)loadBytesSize / totalLength;
            }

            return progress;
        }

        /// <summary>
        /// 停止加载器
        /// </summary>
        public void Stop()
        {
            Dispose();

            if (!IsDone())
            {
                Error = "cancelled";
            }
        }


        void CheckError(AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
                Error = e.Error.Message;
            if (e.Cancelled)
                Error = "cancelled";
        }

        public void Dispose()
        {
            cancelSource.Cancel();
            if (fileStream != null)
            {
                fileStream.Flush();
                fileStream.Dispose();
                fileStream.Close();
                fileStream = null;
            }

            if (httpResponse != null)
            {
                httpResponse.Dispose();
                httpResponse = null;
            }

        }
    }
}