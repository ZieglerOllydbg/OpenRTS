using System;

namespace ZLib
{
    /// <summary>
    /// 单例(抽象类)
    /// 需要单例的类只需要继承自此类即可, 不支持子类单例
    ////类似这样使用:
    //Class1继承自Singleton<Class1>
    //Class1.instance
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    abstract public class Singleton<T> where T : new()
    {
        private static T _instance;

        /// <summary>
        /// 单例
        /// </summary>
        public static T instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new T();
                }
                return Singleton<T>._instance;
            }
        }

        public Singleton()
        {
            //防止创建多个实例
            if (Singleton<T>._instance != null)
            {
                throw new Exception(typeof(T).ToString() + "单例！");
            }

            Singleton<T>._instance = (T)(System.Object)this;
        }

        /// <summary>
        /// 对单例的析构
        /// </summary>
        public virtual void Dispose()
        {
            _instance = default(T);
        }
    }
}
