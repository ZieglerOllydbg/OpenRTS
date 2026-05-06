
using ZLib;
using System;

namespace ZFrame
{
    /// <summary>
    /// 事件管理器（静态）
    /// 此管理器可实现了实践的派发和接收（框架内部使用）
    /// </summary>
    static internal class ModuleEventManager
    {
        /// <summary>
        /// 消息观察线程
        /// </summary>
        private static ObserverThread ot = new ObserverThread();

        /// <summary>
        /// 注册事件
        /// </summary>
        /// <param name="__key">唯一标识</param>
        /// <param name="__callBack">收数据回调 ,回调类似:void receivedEventHandle( Notification __notification)</param>
        /// <param name="__owner">所有者标识</param>
        internal static void AddEvent(int __key, Action<Notification> __callBack, string __owner)
        {
            //if (!__meClass.IsSubclassOf(typeof(ModuleEvent)))
            //{
            //    throw new Exception("注册的事件模型必须继承自ModuleEvent");
            //}
            Observer observer = new Observer(__callBack, __owner);

            ot.registerObserver(__key, observer);
        }

        ///// <summary>
        ///// 批量注册事件
        ///// </summary>
        ///// <param name="__keyArr">唯一标识集合</param>
        ///// <param name="__callBack">收数据回调 ,回调类似:void receivedEventHandle( Notification __notification)</param>
        ///// <param name="__owner">所有者标识</param>
        //internal static void AddEvents(List<string> __keyArr, Action<Notification> __callBack, string __owner)
        //{
        //    __keyArr.ForEach(p => {
        //        AddEvent(p, __callBack, __owner);
        //    });
        //}

        /// <summary>
        /// 移除事件
        /// </summary>
        /// <param name="__key">唯一标识</param>
        /// <param name="__owner">所有者标识</param>
        internal static void RemoveEvent(int __key, string __owner)
        {
            ot.removeObserver(__key, __owner);
        }

        /// <summary>
        /// 移除事件(通过所有者标识)
        /// </summary>
        /// <param name="__owner">所有者标识</param>
        internal static void RemoveEventByOwner(string __owner)
        {
            ot.removeObserverByNotifyContext(__owner);
        }

        ///// <summary>
        ///// 批量删除事件
        ///// </summary>
        ///// <param name="__meClassArr">唯一标识集合</param>
        ///// <param name="__owner">所有者标识</param>
        //internal static void RemoveEvents(List<string> __keyArr, string __owner)
        //{
        //    __keyArr.ForEach(p =>
        //    {
        //        RemoveEvent(p, __owner);
        //    });
        //}

        /// <summary>
        /// 派发事件
        /// </summary>
        /// <param name="__mEvent">ModuleEvent事件</param>
        internal static void DispatchEvent(ModuleEvent __mEvent)
        {
            DispatchEvent(__mEvent.key, __mEvent);
        }
        /// <summary>
        /// 派发事件
        /// </summary>
        /// <param name="__key">唯一标识</param>
        /// <param name="__data">数据</param>
        internal static void DispatchEvent(int __key, object __data)
        {
            //通知
            Notification notification = new Notification(__key, __data);
            ot.notifyObservers(notification);
        }

        /// <summary>
        /// 销毁静态的状态
        /// </summary>
        internal static void Dispose()
        {
            ot.clear();
        }
    }
}
