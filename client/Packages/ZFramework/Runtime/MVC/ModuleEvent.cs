using ZLib;

namespace ZFrame
{
    /// <summary>
    /// 框架使用的事件基类(继承自Object, 区别于EventArgs)
    /// </summary>
    public class ModuleEvent : IPropagation
    {
        /// <summary>
        /// 唯一标识
        /// </summary>
        public int key
        {
            get
            {
                if (_key == -1)
                {
                    _key = GetType().GetHashCode();
                }
                return _key;
            }
            set { _key = value; }
        }
        protected int _key = -1;

        /// <summary>
        /// 事件派发者(可为null)
        /// </summary>
        public object sender
        {
            get;
            protected set;
        }

        /// <summary>
        /// 是否已经停止冒泡
        /// </summary>
        public bool stopPropagation { private set; get; } = false;

        /// <summary>
        /// 框架使用的事件基类(继承自Object, 区别于EventArgs)
        /// </summary>
        public ModuleEvent() { sender = null; }

        /// <summary>
        /// 框架使用的事件基类(继承自Object, 区别于EventArgs)
        /// </summary>
        /// <param name="__sender">事件派发者</param>
        public ModuleEvent(object __sender) { sender = __sender; }


        /// <summary>
        /// 停止冒泡
        /// </summary>
        public void StopPropagation()
        {
            stopPropagation = true;
        }

        /// <summary>
        /// 恢复冒泡
        /// </summary>
        public void ResumePropagation()
        {
            stopPropagation = false;
        }
        ///// <summary>
        ///// 是否是某个类的实例
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <returns></returns>
        //public bool IsInstance<T>() where T : ModuleEvent
        //{
        //    return this.GetType() == typeof(T);
        //}
    }
}
