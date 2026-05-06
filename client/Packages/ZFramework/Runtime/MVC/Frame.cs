using System;
using System.Collections.Generic;
using ZLib;

namespace ZFrame
{
    /// <summary>
    /// Frame基类(单例)
    /// </summary>
    public class Frame : Singleton<Frame>//, IFrame
    {
        //原始的ModuleList
        private List<Module> _originalModuleList;

        /// <summary>
        /// Frame基类(单例)
        /// </summary>
        public Frame()
        {
            _originalModuleList = ListModules();

            //注册模块
            //============================================
            if (_originalModuleList != null)
            {
                for (int i = 0; i < _originalModuleList.Count; i++)
                {
                    Module module = _originalModuleList[i];

                    //注册module,保证单例
                    //DLog.add("注册模块:"+getQualifiedClassName(module));
                    RegisterModule(module);
                }
            }
            //============================================

            //注册刷新
            var loopManager = PlayerMainLoop.PlayerLoopManager().CreateInternal();
            loopManager.SetUpdate(internal_OnUpdate);
            loopManager.SetLateUpdate(internal_OnLateUpdate);
            //Tick.AddUpdate(internal_OnUpdate);
            //Tick.AddLateUpdate(internal_OnLateUpdate);
        }


        //--------------------------------------------------------
        // public API(注册 、查询、移除 Module)
        //--------------------------------------------------------
        //module字典 
        private Dictionary<Type, Module> moduleMap = new Dictionary<Type, Module>();

        /// <summary>
        /// 注册Module
        /// </summary>
        /// <param name="__module"></param>
        public void RegisterModule(Module __module)//暂时对外隐藏此方法
        {
            //单例
            if (moduleMap.ContainsKey(__module.GetType())) throw new Exception("不能注册两个相同的Module");
            moduleMap.Add(__module.GetType(), __module);
            __module.RegisterProcessors();
            __module.internal_OnRegister();
        }

        /// <summary>
        /// 移除module(返回被移除的Module)
        /// </summary>
        /// <param name="__moduleClass"></param>
        /// <returns></returns>
        public Module RemoveModule(Type __moduleClass)
        {
            Module module;
            if (moduleMap.TryGetValue(__moduleClass, out module))
            {
                moduleMap.Remove(__moduleClass);

                module.RemoveProcessors();
                module.internal_OnRemove();
            }
            return module;
        }
        public T RemoveModule<T>() where T : Module
        {
            Module module;
            if (moduleMap.TryGetValue(typeof(T), out module))
            {
                moduleMap.Remove(typeof(T));

                module.RemoveProcessors();
                module.internal_OnRemove();
            }
            return (T)module;
        }

        /// <summary>
        /// 指定module是否存在
        /// </summary>
        /// <param name="__moduleClass"></param>
        /// <returns></returns>
        public bool HasModule(Type __moduleClass)
        {
            return moduleMap.ContainsKey(__moduleClass);
        }
        public bool HasModule<T>() where T : Module
        {
            return moduleMap.ContainsKey(typeof(T));
        }

        /// <summary>
        /// 查找module
        /// </summary>
        /// <param name="__moduleClass"></param>
        /// <returns></returns>
        public Module GetModule(Type __moduleClass)//暂时对外隐藏此方法
        {
            Module module;
            moduleMap.TryGetValue(__moduleClass, out module);
            return module;
        }
        public T GetModule<T>() where T : Module//暂时对外隐藏此方法
        {
            Module module;
            moduleMap.TryGetValue(typeof(T), out module);
            return (T)module;
        }

        /// <summary>
        /// 当每帧刷新时
        /// </summary>
        private void internal_OnUpdate()
        {
            OnUpdate();

            //foreach (Module module in moduleMap.Values)//这里是否需要一个副本或者使用new List<Type>，以防止在module.OnUpdate修改moduleMap而引起错误呢，后面测试下？11111111
            //{
            //    module.internal_OnUpdate();
            //}

            //foreach (KeyValuePair<Type, Module> item in moduleMap)
            //{
            //    item.Value.internal_OnUpdate();
            //}

            for (var i = moduleMap.GetEnumerator(); i.MoveNext();)
            {
                i.Current.Value.internal_OnUpdate();
            }

            ////可能存在一点性能问题，后面测试下11111111111111111111
            //foreach (KeyValuePair<Type, Module> kvp in moduleMap)
            //{
            //    Module module = kvp.Value;
            //    module.OnUpdate();
            //}
        }

        /// <summary>
        /// 当每帧刷新时
        /// </summary>
        private void internal_OnLateUpdate()
        {
            OnLateUpdate();

            //foreach (var module in moduleMap.Values)//这里是否需要一个副本或者使用new List<Type>，以防止在module.OnUpdate修改moduleMap而引起错误呢，后面测试下？11111111
            //{
            //    module.internal_OnLateUpdate();
            //}

            //foreach (KeyValuePair<Type, Module> item in moduleMap)
            //{
            //    item.Value.internal_OnLateUpdate();
            //}

            for (var i = moduleMap.GetEnumerator(); i.MoveNext();)
            {
                i.Current.Value.internal_OnLateUpdate();
            }
        }


        //--------------------------------------------------------
        // 静态方法
        //--------------------------------------------------------
        /// <summary>
        /// 派发事件
        /// </summary>
        /// <param name="__mEvent">ModuleEvent事件</param>
        public static void DispatchEvent(ModuleEvent __mEvent)
        {
            ModuleEventManager.DispatchEvent(__mEvent);
        }
        /// <summary>
        /// 派发事件
        /// </summary>
        /// <param name="__key">唯一标识</param>
        /// <param name="__data">数据</param>
        public static void DispatchEvent(int __key, object __data)
        {
            ModuleEventManager.DispatchEvent(__key, __data);
        }
        /// <summary>
        /// 注册事件（一般用不到）
        /// </summary>
        /// <param name="__key">唯一标识</param>
        /// <param name="__callBack">收数据回调 ,回调类似:void receivedEventHandle( Notification __notification)</param>
        /// <param name="__owner">所有者标识</param>
        public static void AddEvent(int __key, Action<Notification> __callBack, string __owner)
        {
            ModuleEventManager.AddEvent(__key, __callBack, __owner);
        }
        /// <summary>
        /// 移除事件
        /// </summary>
        /// <param name="__key">唯一标识</param>
        /// <param name="__owner">所有者标识</param>
        public static void RemoveEvent(int __key, string __owner)
        {
            ModuleEventManager.RemoveEvent(__key, __owner);
        }
        /// <summary>
        /// 移除事件（一般用不到）
        /// </summary>
        /// <param name="__owner">所有者标识</param>
        public static void RemoveEventByOwner(string __owner)
        {
            ModuleEventManager.RemoveEventByOwner(__owner);
        }

        //--------------------------------------------------------
        // 以下为需要覆写的函数
        //--------------------------------------------------------
        /// <summary>
        /// 当每帧刷新时
        /// </summary>
        virtual protected void OnUpdate() { }

        /// <summary>
        /// 当每帧刷新时
        /// </summary>
        virtual protected void OnLateUpdate() { }

        /// <summary>
        /// 注册的Module的集合（会在构造中被自动注册）(注意此函数在框架中只能调用一次！！！)
        /// 请注意：返回为Module的实例数组
        /// </summary>
        /// <returns></returns>
        virtual protected List<Module> ListModules()
        {
            return null;
        }

        /// <summary>
        /// Frame销毁
        /// 会同时销毁所有已经注册的事件
        /// </summary>
        public override void Dispose()
        {
            for (var i = moduleMap.GetEnumerator(); i.MoveNext();)
            {
                var module = i.Current.Value;

                module.RemoveProcessors();
                module.internal_OnRemove();
            }

            moduleMap.Clear();

            ModuleEventManager.Dispose();

            base.Dispose();
        }
    }
}
