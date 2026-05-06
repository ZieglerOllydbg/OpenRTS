using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZLib
{
    /// <summary>
    /// 缓存池(静态) (Key可不唯一，Value理论上唯一)
    /// 注意：可以存取key值相同的对象！！！比如同一个key可以对应两个不同的GameObject(这两个GameObject是从同一个prefab复制来的), 所以key并不唯一。
    /// 
    //类似这样使用:
    //var a = new Class1();
    //回收
    //ObjectCache<Class1>.Recycle(a);
    //重新使用
    //Class1 aa = ObjectCache<Class1>.Get("key1");
    /// </summary>
    /// <typeparam name="T"></typeparam>
    static public class ObjectCache<T> where T : ICache
    {
        /// <summary>
        /// 缓存池数据
        /// </summary>
        private class ObjectData
        {
            //缓存数据
            internal T data;

            //处于free状态的时间(仅供内部使用)
            internal float time;

            //是否经过了Updated（内部使用，针对Unity添加的参数，回收到池子里的东西，要经过一次Update以后才能继续使用，否则新设置的值有可能会被Destory掉,因为Destory总是在Update之后执行！！！）
            internal bool IsUpdated;
            internal ObjectData(T __t)
            {
                data = __t;
            }
        }

        //双向链表
        //从前往后：time < time
        static private LinkedList<ObjectData> llist = new LinkedList<ObjectData>();

        /// <summary>
        /// 池子最大容量（不会立即更新）
        /// </summary>
        static public uint maxSize = 200;
        /// <summary>
        /// 延时销毁时间，单位：秒
        /// </summary>
        static public float destroyDelay = 30;
        static ObjectCache()
        {
            //添加进帧循环
            PlayerMainLoop.PlayerLoopManager().CreateInternal().SetUpdate(Update);
            //Tick.AddUpdate(Update);
        }

        /// <summary>
        /// 池中存储对象数量
        /// </summary>
        /// <returns></returns>
        static public int GetSize()
        {
            return llist.Count;
        }

        /// <summary>
        /// 是否存在某对象
        /// </summary>
        /// <param name="__key"></param>
        /// <returns></returns>
        static public bool Has(string __key)
        {
            if (GetSize() > 0)
            {
                LinkedListNode<ObjectData> lln = llist.First;//顺序
                while (true)
                {
                    if (lln == null) break;
                    if (lln.Value.data.key == __key
                        /*&& lln.Value.IsUpdated*/)
                    {
                        //返回
                        return true;
                    }
                    lln = lln.Next;
                }
            }
            return false;
        }

        /// <summary>
        /// 刷新倒计时时间
        /// </summary>
        /// <param name="time"></param>
        static public void Refresh(float time)
        {
            if (GetSize() > 0)
            {
                LinkedListNode<ObjectData> lln = llist.First;//顺序
                while (true)
                {
                    if (lln == null) break;
                    lln.Value.time = (Time.time - destroyDelay) + time;
                    lln = lln.Next;
                }
            }
        }

        /// <summary>
        /// 获取对象(可能存在多个， 仅返回第一个)
        /// </summary>
        /// <param name="__key"></param>
        /// <returns></returns>
        static public T Get(string __key)
        {
            if (GetSize() > 0)
            {
                LinkedListNode<ObjectData> lln = llist.First;//顺序
                while (true)
                {
                    if (lln == null) break;
                    if (lln.Value.data.key == __key)
                    {
                        //移除此项
                        llist.Remove(lln);
                        //返回
                        return lln.Value.data;
                    }
                    lln = lln.Next;
                }
            }
            return default(T);
        }
        /// <summary>
        /// 回收对象
        /// </summary>
        /// <param name="__t"></param>
        static public void Recycle(T __t)
        {
#if DEBUG
            if (typeof(T) != __t.GetType())
            {
                throw new Exception("请保证传入参数真实类型与泛型约束真实类型一致！");//11111111111111111111111
            }
#endif

            //新建一个
            var od = new ObjectData(__t);
            //添加到到末尾!!!
            llist.AddLast(od);

            //记录时间
            od.time = Time.time;

            //检查容量
            while (GetSize() > maxSize)
            {
                Remove(llist.First);
            }
        }
        //移除一项
        static private void Remove(LinkedListNode<ObjectData> __lt)//暂时对外隐藏此方法
        {
            llist.Remove(__lt);
            __lt.Value.data.Destroy();
        }


        /// <summary>
        /// 清空缓冲池 立即释放资源
        /// </summary>
        static public void Clear(bool isClearStrong = false)
        {
            LinkedListNode<ObjectData> lln = llist.First;//顺序

            while (lln != null)
            {
                LinkedListNode<ObjectData> tempLln = lln;

                lln = lln.Next;

                if (!isClearStrong)
                {
                    if (!tempLln.Value.data.isStrong)

                        Remove(tempLln);
                }
                else
                {
                    Remove(tempLln);
                }
            }
        }

        //**********************************************************************************************************
        //辅助参数
        static private int _tempCount = 0;
        //检测
        static private void Update()
        {
            //检查 Update
            if (llist.Count > 0)
            {
                var od = llist.First;

                while (od != null)
                {
                    od.Value.IsUpdated = true;
                    od = od.Next;
                }
            }

            //30帧检测一次
            if (++_tempCount < 30)
            {
                return;
            }
            _tempCount %= 30;

            //检查 延时卸载
            if (llist.Count > 0)
            {
                LinkedListNode<ObjectData> lln = llist.First;//顺序
                while (lln != null)
                {
                    LinkedListNode<ObjectData> tempLln = lln;
                    lln = lln.Next;

                    if (Time.time - tempLln.Value.time > destroyDelay)
                    {
                        if (!tempLln.Value.data.isStrong)       //只有非strong的才允许移除
                        {
                            Remove(tempLln);
                        }
                    }
                    else
                    {
                        break;//因为链表中是按最后调用时间排序的， 所以此处可以做跳出处理
                    }
                }
            }
        }
        //**********************************************************************************************************
    }
}
