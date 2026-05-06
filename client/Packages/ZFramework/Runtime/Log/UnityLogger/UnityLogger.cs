// Copyright (C) 2014 Extesla, LLC.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
using System;
using Common.Logging;
using Common.Logging.Factory;
using UnityDebug = UnityEngine.Debug;

namespace ZLog
{
    /// <summary>
    /// UnityLogger 类
    /// 实现了CommonLogger
    /// 真正用户写日志的地方，也许是调用UnityAPI，也许是直接写入文件，不通过Unity日志系统，这主要取决于日志等级的设计。
    /// </summary>
    internal class UnityLogger : AbstractLogger
    {
        private string name;
        private LogLevel currentLogLevel;

        /// <summary>
        /// The key of the logger.
        /// </summary>
        [CoverageExclude]
        public string Name
        {
            get { return name; }
        }

        /// <summary>
        /// The current logging threshold. Messages recieved that are beneath this threshold will not be logged.
        /// </summary>
        [CoverageExclude]
        public LogLevel CurrentLogLevel
        {
            get { return currentLogLevel; }
        }

        /// <summary>
        ///  Initializes a new instance of the <see cref="UnityLogger"/> class.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="logLevel"></param>
        public UnityLogger(string name, LogLevel logLevel)
            : base()
        {
            this.name = name;
            this.currentLogLevel = logLevel;
        }

        /// <summary>
        /// Actually sends the message to the underlying log system.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Writes the message (and exception if not <c>null</c>) to the
        /// Unity engine log.
        /// </para>
        /// </remarks>
        /// <param key="level">the level of this log event.</param>
        /// <param key="message">the message to log</param>
        /// <param key="exception">the exception to log (may be null)</param>
        protected override void WriteInternal(LogLevel level, object message, Exception exception, bool writeOnly = false)
        {
            if (LogSystem.EnableType == LogEnableType.UseUnityEngineAPI)
            {
                var logMsg = $"[{Name}] {message}";

                if (LogLevel.Warn == level)
                {
                    UnityDebug.LogWarning(logMsg);
                }
                else if (LogLevel.Error == level)
                {
                    UnityDebug.LogError(logMsg);
                }
                else
                {
                    UnityDebug.Log(logMsg);
                }

                // **
                // If the exception is not null, we should log the actual exception
                // using the UnityEngine's logging capability. [SWQ]
                if (exception != null)
                {
                    UnityDebug.LogException(exception);
                }
            }
            else if(LogSystem.EnableType == LogEnableType.OnlyWrite)
            {
                //仅写入文件，不使用 Unity Log API
                CustomLoggerWriter.OnWrite(level, Name, message == null ? "" : message.ToString(), exception);
            }
        }

        #region Properties
        /// <summary>
        /// Gets a value indicating whether this instance has debug logging
        /// enabled.
        /// </summary>
        /// <value><c>true</c> if this instance has debug level logging enabled;
        /// otherwise, <c>false</c>.</value>
        public override bool IsDebugEnabled
        {
            get { return LogLevel.Debug >= CurrentLogLevel; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance has error logging
        /// enabled.
        /// </summary>
        /// <value><c>true</c> if this instance has error level logging enabled;
        /// otherwise, <c>false</c>.</value>
        public override bool IsErrorEnabled
        {
            get { return LogLevel.Error >= CurrentLogLevel; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance has fatal logging
        /// enabled.
        /// </summary>
        /// <value><c>true</c> if this instance has fatal level logging enabled;
        /// otherwise, <c>false</c>.</value>
        public override bool IsFatalEnabled
        {
            get { return LogLevel.Fatal >= CurrentLogLevel; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance has info logging
        /// enabled.
        /// </summary>
        /// <value><c>true</c> if this instance has info level logging enabled;
        /// otherwise, <c>false</c>.</value>
        public override bool IsInfoEnabled
        {
            get { return LogLevel.Info >= CurrentLogLevel; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance has trace logging
        /// enabled.
        /// </summary>
        /// <value><c>true</c> if this instance has trace level logging enabled;
        /// otherwise, <c>false</c>.</value>
        public override bool IsTraceEnabled
        {
            get { return LogLevel.Trace >= CurrentLogLevel; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance has warn logging
        /// enabled.
        /// </summary>
        /// <value><c>true</c> if this instance has warn level logging enabled;
        /// otherwise, <c>false</c>.</value>
        public override bool IsWarnEnabled
        {
            get { return LogLevel.Warn >= CurrentLogLevel; }
        }

        #endregion
    }
}
