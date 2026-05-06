using System;
namespace ZLib
{
    /// <summary>
    /// 观察者
    /// @author zcp
    /// </summary>
    public class Observer
    {
        /// <summary>
        /// 通知函数
        /// </summary>
        private Action<Notification> _notifyMethod;

        /// <summary>
        /// 通知的所属标识，一般为一个字符串， 即该通知属于哪个所有者
        /// </summary>
        public string notifyContext;

        /// <summary>
        /// 观察者
        /// </summary>
        /// <param name="__notifyMethod">通知函数</param>
        /// <param name="__notifyContext">通知的所属标识，一般为一个字符串， 即该通知属于哪个所有者</param>
        public Observer(Action<Notification> __notifyMethod, string __notifyContext)
        {
            _notifyMethod = __notifyMethod;
            notifyContext = __notifyContext;
        }

        /// <summary>
        /// 通知Observer
        /// </summary>
        /// <param name="__notification"></param>
        public void notifyObserver(Notification __notification)
        {
            //notifyMethod.apply(notifyContext,[$notification]);
            _notifyMethod(__notification);

        }

        /// <summary>
        /// 比较是不是属于同一所有者
        /// </summary>
        /// <param name="__notifyContext"></param>
        /// <returns></returns>
        public bool compareNotifyContext(string __notifyContext)
        {
            return notifyContext == __notifyContext;//notifyContext.Equals(__notifyContext);
        }
    }
}