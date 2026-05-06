using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZLib
{
    /// <summary>
    /// 共享池（计数共享，延迟卸载）(静态)
    /// ObjectCache、ObjectPool、ObjectShare三者区别：
    /// ObjectCache:     “缓存池”是保存某一类型的多个实例，各个对象“不同”，供后面重复使用，使用的时候对象从池移除，可设置保存个数 和 延迟卸载时间。不可新建对象，查不到则返回null。
    ///                   一般用于在使用过程中内部“不”发生改变的复杂对象。例如：GameObject对象, 存起来供后面直接获取之后直接使用， 一个实例只能在一个地方使用。
    /// ObjectPool：     “对象池”是保存某一类型的多个实例，各个对象“等价”，供后面重复使用，使用的时候对象从池移除，可设置保存个数。可新建对象，必然有返回值。
    ///                   一般用于在使用过程中内部“会”发生改变的复杂“自定义”对象。例如：自定义的角色对象。
    /// ObjectShare：    “共享池”是保存某一类型的一个实例，各个对象“不同”，供多个地方共用，使用的时候对象“不”从池移除，可设置延迟卸载时间。不可新建对象，查不到则返回null。
    ///                   一般用于全局独一份的共享数据源。例如：Texture对象，类似Flash中的BitmapData对象。

    ////类似这样使用:
    //var a = new Class1();
    //设置延迟卸载时间
    //ObjectShare<Class1>.destroyDelay = 30;
    //检查有无
    //ObjectShare<Class1>.HasShareData("a");
    //首次添加(计数=0)
    //ObjectShare<Class1>.AddShareData("a",a);
    //装载(计数+1)
    //Class1 aa = ObjectShare<Class1>.InstallShareData("a");
    //卸载(计数-1)
    //ObjectShare<Class1>.UnInstallShareData("a");
    /// 
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    static public class ObjectShare<T> where T : IShare
    {

        /// <summary>
        /// 		//存储数据字典
        /// 		key: 唯一的键值
        /// 		value: IShare
        /// </summary>
        static public Dictionary<string, T> shareDataDict = new Dictionary<string, T>();

        /// <summary>
        /// 延时销毁时间(计数为0的对象在此时间后会被销毁)，单位：秒
        /// </summary>
        static public float destroyDelay = 30;


        /// <summary>
        /// 计数共享
        /// </summary>
        static ObjectShare()
        {
            //添加进帧循环
            PlayerMainLoop.PlayerLoopManager().CreateInternal().SetUpdate(Update);
            //Tick.AddUpdate(Update);
        }

        /// <summary>
        /// 检查缓存中有没有指定数据
        /// </summary>
        /// <param name="__key"></param>
        /// <returns></returns>
        static public bool HasShareData(string __key)
        {
            return shareDataDict.ContainsKey(__key);
        }
        /// <summary>
        /// 获取指定数据(注意只起到获取作用，不改变计数)
        /// </summary>
        /// <param name="__key"></param>
        /// <returns></returns>
        static public T GetShareData(string __key)//暂时对外屏蔽此方法
        {
            T sh;
            shareDataDict.TryGetValue(__key, out sh);
            return sh;
        }
        /// <summary>
        /// 添加数据进缓存(注意只起到添加进缓存的作用，不改变计数)
        /// </summary>
        /// <param name="__key"></param>
        /// <param name="__sh"></param>
        static public void AddShareData(string __key, T __sh)
        {
#if DEBUG
            if (typeof(T) != __sh.GetType())
            {
                throw new Exception("协变 T类型错误，请实现泛型约束方法");//11111111111111111111111
            }
#endif

            var csd = GetShareData(__key);
            if (csd == null)
            {
                shareDataDict.Add(__key, __sh);
                __sh.time = Time.time;
            }
            else
            {
                throw new Exception("error:key为" + __key + "的数据已存在");
            }
        }
        /// <summary>
        /// 从缓存中移除数据（强制从缓存和计数管理中移除）
        /// </summary>
        /// <param name="__key"></param>
        static private void RemoveShareData(string __key)//暂时对外屏蔽此方法
        {
            T csd = GetShareData(__key);
            if (csd != null)
            {
                //从缓存中移除
                shareDataDict.Remove(__key);

                //调用释放
                csd.Destroy();
            }
        }
        /// <summary>
        /// 装载对象（该对象计数会被加1）
        /// </summary>
        /// <param name="__key"></param>
        /// <returns></returns>
        static public T InstallShareData(string __key, string refInfo = null)
        {
            T csd = GetShareData(__key);
            if (csd != null)
            {
                csd.count += 1;
                //  if(refInfo != null)
                //      csd.AddRefInfo(refInfo);
            }
            return csd;
        }
        /// <summary>
        /// 卸载对象（该对象计数会被减1）
        /// </summary>
        /// <param name="__key"></param>
        static public void UnInstallShareData(string __key, string refInfo = null)
        {
            T csd = GetShareData(__key);
            if (csd != null)
            {
                csd.count -= 1;
                // if (refInfo != null)
                //     csd.DelRefInfo(refInfo);
                if (csd.count == 0)
                {
                    //记录时间
                    csd.time = Time.time;
                }
                else if (csd.count < 0)
                {
                    Debug.LogError($"UnInstallShareData 卸载出问题....{csd.count}");
                }
            }
        }


        //**********************************************************************************************************
        //辅助参数
        static private int _tempCount = 0;
        //检测
        static private void Update()
        {
            //700帧检测一次
            if (++_tempCount < 20)
            {
                return;
            }
            _tempCount %= 20;

            ForceUpdate();
        }

        static public void ForceUpdate()
        {
            //检查卸载
            List<string> keys = new List<string>(shareDataDict.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                var key = keys[i];

                T csd = shareDataDict[key];
                if (csd.count == 0)//计数已经为0
                {
                    //超过时限了
                    if (Time.time - shareDataDict[key].time >= destroyDelay)
                    {
                        //从缓存中移除
                        shareDataDict.Remove(key);

                        //调用释放
                        csd.Destroy();
                    }
                }
            }
        }
        //**********************************************************************************************************
    }
}
