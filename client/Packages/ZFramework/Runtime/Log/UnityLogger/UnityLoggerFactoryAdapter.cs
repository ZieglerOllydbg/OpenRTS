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
using Common.Logging;
using Common.Logging.Factory;

namespace ZLog
{
    /// <summary>
    /// UnityLoggerFactoryAdapter
    /// 基于CommonLogging的实现
    /// </summary>
    internal class UnityLoggerFactoryAdapter : AbstractCachingLoggerFactoryAdapter
    {
        private LogLevel level;

        /// <summary>
        /// The default <see cref="LogLevel"/> to use when creating new <see cref="ILog"/> instances.
        /// </summary>
        [CoverageExclude]
        public LogLevel Level
        {
            get { return level; }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public UnityLoggerFactoryAdapter(LogLevel _level) : base()
        {
            this.level = _level;
        }

        /// <summary>
        /// Create the specified named logger instance
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected override ILog CreateLogger(string name)
        {
            ILog log = new UnityLogger(name, level);

            return log;
        }

        /// <summary>
        /// 清理
        /// </summary>
        public void Clear()
        {
            ClearLoggerCache();
        }
    }
}