
using System;
using System.Collections.Generic;

namespace ZFrame
{
    /// <summary>
    /// 约定接口
    /// @author Ollydbg
    /// @date 2019年4月29日
    /// </summary>
    public interface IDispathMessage
    {
        /// <summary>
        ///  派发消息
        /// </summary>
        /// <param name="_event"></param>
        void DispatchEvent(ModuleEvent _event);

        /// <summary>
        /// 派发消息
        /// </summary>
        /// <param name="_key"></param>
        /// <param name="data"></param>
        void DispatchEvent(int _key, object data);
    }
}
