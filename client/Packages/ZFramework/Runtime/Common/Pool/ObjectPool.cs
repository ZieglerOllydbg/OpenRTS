using System;
using System.Collections.Generic;

namespace ZLib
{
    /// <summary>
    /// 对象池(静态)
    /// 
    ////类似这样使用:
    //var a = new Class1();
    //进池
    //ObjectPool<Class1>.Recycle(a);
    //出池
    //Class1 aa = ObjectPool<Class1>.Get();
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    static public class ObjectPool<T> where T : IPool//, new()
    {
        /// <summary>
        /// 对象池数据
        /// </summary>
        private class ObjectData
        {
            //缓存数据
            internal T data;

            //是否经过了Updated（内部使用，针对Unity添加的参数，回收到池子里的东西，要经过一次Update以后才能继续使用，否则新设置的值有可能会被Destory掉,因为Destory总是在Update之后执行！！！）
            internal bool IsUpdated;
            internal ObjectData(T __t)
            {
                data = __t;
            }
        }

        //池
        static private List<ObjectData> pool = new List<ObjectData>();

        //该实例创建的总数
        static private int totalCreated = 0;


        /// <summary>
        /// 池子最大容量（不会立即更新）
        /// </summary>
        static public uint maxSize = 200;

        static ObjectPool()
        {
            //添加进帧循环
            PlayerMainLoop.PlayerLoopManager().CreateInternal().SetUpdate(Update);
            //Tick.AddUpdate(Update);
        }

        /// <summary>
        /// 该类创建的实例的总数
        /// </summary>
        /// <returns></returns>
        static public int GetTotalCreated()
        {
            return totalCreated;
        }

        /// <summary>
        /// 池中存储对象数量
        /// </summary>
        /// <returns></returns>
        static public int GetSize()
        {
            return pool.Count;
        }

        /// <summary>
        /// 回收（并释放）一个对象
        /// </summary>
        /// <param name="__obj"></param>
        static public void Recycle(T __obj)
        {
#if DEBUG
            if (typeof(T) != __obj.GetType())
            {
                throw new Exception("请保证传入参数真实类型与泛型约束真实类型一致！");//11111111111111111111111
            }
#endif
            //释放
            __obj.Dispose();

            if (pool.Count < maxSize)//长度判断
            {
                ObjectData od = new ObjectData(__obj);
                //改变标识
                od.IsUpdated = false;

                pool.Add(od);//添加到结尾
            }
            //else
            //{
            //    throw new Exception("对象池超界");//11111111111111111111111111111
            //}
        }

        /// <summary>
        /// 获取（或创建）一个对象
        /// </summary>
        /// <param name="__objs"></param>
        /// <returns></returns>
        static public T Get(params object[] __objs)
        {
            T obj = default(T);

            //查找空闲的
            if (pool.Count > 0)
            {
                for (int i = 0; i < pool.Count; i++)
                {
                    var od = pool[i];

                    if (od.IsUpdated)
                    {
                        //从池中移除
                        pool.Remove(od);

                        obj = od.data;
                        obj.ReSet(__objs);
                        break;
                    }
                }
            }

            //新建
            if (obj == null)
            {
                obj = (T)Activator.CreateInstance(typeof(T), __objs);//111111111111111111 直接new 性能更高???
                                                                     //obj = Activator.CreateInstance<T>();
                                                                     //obj.ReSet(objs);
                ++totalCreated;
            }
            return obj;
        }

        //**********************************************************************************************************
        //辅助参数
        //static private int _tempCount = 0;
        //检测
        static private void Update()
        {
            ////30帧检测一次
            //if (++_tempCount < 30)
            //{
            //    return;
            //}
            //_tempCount %= 30;

            //检查 Update过之后才认为是Free的
            if (pool.Count > 0)
            {
                for (int i = 0; i < pool.Count; i++)
                {
                    var od = pool[i];
                    od.IsUpdated = true;
                }
            }
        }
        //**********************************************************************************************************
    }
}