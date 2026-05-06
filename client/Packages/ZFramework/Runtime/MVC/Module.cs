using System;
using System.Collections.Generic;

namespace ZFrame
{
    /// <summary>
    /// Module基类
    /// </summary>
    public class Module// :IModule
    {
        //是否处于可显示，可刷新状态
        protected bool is_visiable;

        //原始的ProcessorList
        private List<Processor> _originalProcessorList;

        /// <summary>
        /// Module基类
        /// </summary>
        public Module()
        {
            _originalProcessorList = ListProcessors();

            //模块初始化
            OnInit();

            //默认显示
            is_visiable = true;
        }

        //--------------------------------------------------------
        // public API(注册 、查询、移除 Processor（实例管理，每一个Module互补干涉）)
        //--------------------------------------------------------
        //processor字典 
        private Dictionary<Type, Processor> processorMap = new Dictionary<Type, Processor>();

        /// <summary>
        /// 注册Processor
        /// </summary>
        /// <param name="__processor"></param>
        public void RegisterProcessor(Processor __processor)//暂时对外隐藏此方法
        {
            //单例
            if (processorMap.ContainsKey(__processor.GetType())) throw new Exception("同一Module不能注册两个相同的Processor");
            processorMap.Add(__processor.GetType(), __processor);
            __processor.RegisterEvents();
            __processor.internal_OnRegister();
        }
        /// <summary>
        /// 移除Processor(返回被移除的Processor)
        /// </summary>
        /// <param name="__processorClass"></param>
        /// <returns></returns>
        public Processor RemoveProcessor(Type __processorClass)//暂时对外隐藏此方法
        {
            Processor processor;
            if (processorMap.TryGetValue(__processorClass, out processor))
            {
                processorMap.Remove(__processorClass);

                processor.RemoveEvents();
                processor.internal_OnRemove();
            }
            return processor;
        }
        public T RemoveProcessor<T>() where T : Processor//暂时对外隐藏此方法
        {
            Processor processor;
            if (processorMap.TryGetValue(typeof(T), out processor))
            {
                processorMap.Remove(typeof(T));

                processor.RemoveEvents();
                processor.internal_OnRemove();
            }
            return (T)processor;
        }

        /// <summary>
        /// 检查是否存在
        /// </summary>
        /// <param name="__processorClass"></param>
        /// <returns></returns>
        public bool HasProcessor(Type __processorClass)
        {
            return processorMap.ContainsKey(__processorClass);
        }
        public bool HasProcessor<T>() where T : Processor
        {
            return processorMap.ContainsKey(typeof(T));
        }

        /// <summary>
        /// 查找Processor
        /// </summary>
        /// <param name="__processorClass"></param>
        /// <returns></returns>
        public Processor GetProcessor(Type __processorClass)
        {
            Processor processor;
            processorMap.TryGetValue(__processorClass, out processor);
            return processor;
        }

        /// <summary>
        /// 获取Processor
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetProcessor<T>() where T : Processor
        {
            Processor processor;
            processorMap.TryGetValue(typeof(T), out processor);
            return (T)processor;
        }

        /// <summary>
        /// 获取当前已经注册的所有 Processor
        /// </summary>
        /// <returns></returns>
        public List<Processor> GetAllRegisterProcessor()
        {
            if (processorMap != null && processorMap.Count > 0)
                return new List<Processor>(processorMap.Values);
            return null;
        }

        //--------------------------------------------------------
        // internal API
        //--------------------------------------------------------
        //注册所有的Processor(只含原始的)
        internal void RegisterProcessors()
        {
            //注册Processor
            if (_originalProcessorList != null && _originalProcessorList.Count > 0)
            {
                _originalProcessorList.ForEach(p => RegisterProcessor(p));
                //for (int i = 0; i < _originalProcessorList.Count; i++)
                //{
                //    RegisterProcessor(_originalProcessorList[i]);
                //}
            }
        }

        // 移除所有的Processor(含所有的)
        internal void RemoveProcessors()
        {
            List<Type> keys = new List<Type>(processorMap.Keys);//这里是否需要一个副本或者使用new List<Type>，以防止在module.OnUpdate修改moduleMap而引起错误呢，后面测试下？11111111
            for (int i = 0; i < keys.Count; i++)
            {
                Processor processor = processorMap[keys[i]];
                //下面语句会操作processorMap
                RemoveProcessor(processor.GetType());
            }

            //移除Processor
            //List<Processor> processorArr = ListProcessors();
            //if(processorArr!=null && processorArr.Count>0)
            //{
            //     foreach(Processor processor in processorArr)
            //    {
            //        RemoveProcessor(processor.GetType());
            //    }
            //}
        }
        internal void internal_OnRegister()
        {
            OnRegister();
        }
        internal void internal_OnRemove()
        {
            OnRemove();
        }
        /// <summary>
        /// 当每帧刷新时
        /// </summary>
        internal void internal_OnUpdate()
        {
            //如果是不显示，不更新，那么不处理数据
            if (!IsCanUpdate())
                return;

            OnUpdate();

            //foreach (Processor processor in processorMap.Values)//这里是否需要一个副本或者使用new List<Type>，以防止在module.OnUpdate修改moduleMap而引起错误呢，后面测试下？11111111
            //{
            //    processor.internal_OnUpdate();
            //}

            //foreach (KeyValuePair<Type, Processor> item in processorMap)
            //{
            //    item.Value.internal_OnUpdate();
            //}

            for (var i = processorMap.GetEnumerator(); i.MoveNext();)
            {
                i.Current.Value.internal_OnUpdate();
            }

            //List<Processor> processorArr = ListProcessors();
            //if (processorArr != null && processorArr.Count > 0)
            //{
            //    foreach (Processor processor in processorArr)
            //    {
            //        processor.OnUpdate();
            //    }
            //}
        }

        /// <summary>
        /// 当每帧刷新时
        /// </summary>
        internal void internal_OnLateUpdate()
        {
            //如果是不显示，不更新，那么不处理数据
            if (!IsCanUpdate())
                return;

            OnLateUpdate();

            //foreach (Processor processor in processorMap.Values)//这里是否需要一个副本或者使用new List<Type>，以防止在module.OnUpdate修改moduleMap而引起错误呢，后面测试下？11111111
            //{
            //    processor.internal_OnLateUpdate();
            //}

            //foreach (KeyValuePair<Type, Processor> item in processorMap)
            //{
            //    item.Value.internal_OnLateUpdate();
            //}

            for (var i = processorMap.GetEnumerator(); i.MoveNext();)
            {
                i.Current.Value.internal_OnLateUpdate();
            }
        }

        //--------------------------------------------------------
        // 以下为需要覆写的函数
        //--------------------------------------------------------
        /// <summary>
        /// 当初始化时
        /// </summary>
        virtual protected void OnInit() { }

        /// <summary>
        /// 当被注册时
        /// </summary>
        virtual protected void OnRegister() { }

        /// <summary>
        ///  当被移除时
        /// </summary>
        virtual protected void OnRemove() { }
        /// <summary>
        /// 当每帧刷新时
        /// </summary>
        virtual protected bool OnUpdate() { return true; }

        /// <summary>
        /// 当每帧刷新时
        /// </summary>
        virtual protected bool OnLateUpdate() { return true; }

        /// <summary>
        /// 是否可以更新数据信息
        /// </summary>
        /// <returns></returns>
        virtual public bool IsCanUpdate() { return is_visiable; }

        /// <summary>
        /// 注册的Processor的集合(注意此函数在框架中只能调用一次！！！)
        /// 请注意：返回为Processor的实例数组
        /// </summary>
        /// <returns></returns>
        virtual protected List<Processor> ListProcessors()
        {
            return null;
        }
    }
}
