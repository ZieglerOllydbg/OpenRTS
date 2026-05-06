using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System;

namespace ZLib
{

    #region 线程安全无锁队列
    /// <summary>
    /// 队列类（使用循环数组）(加上线程安全)
    /// </summary>
    /// <typeparam name="T">队列中元素的类型</typeparam>
    class ThreadSafeQueue<T>
    {
        /// <summary>
        /// 通知的状态机
        /// </summary>
        AutoResetEvent notice = new AutoResetEvent(true);
        /// <summary>
        /// 循环数组，初始大小为100
        /// </summary>
        T[] ary = new T[100];
        /// <summary>
        /// 队头
        /// </summary>
        int front = 0;
        /// <summary>
        /// 队尾
        /// </summary>
        int rear = 0;
        /// <summary>
        /// 队大小
        /// </summary>
        int size = 0;
        /// <summary>
        /// 入队
        /// </summary>
        /// <param name="t">入队元素</param>
        public void Enqueue(T t)
        {
            Lock();
            //如果队列大小等于数组长度，那么数组大小加倍
            if (size == ary.Length)
            {
                DoubleSize();
            }
            ary[rear] = t;
            //队尾前移
            rear++;
            //这一句是循环数组的关键：
            //如果rear超过数组下标了，
            //那么将从头开始使用数组。
            rear %= ary.Length;
            //大小加一
            size++;
            UnLock();
        }
        /// <summary>
        /// 出队
        /// </summary>
        /// <returns>出队元素</returns>
        public T Dequeue()
        {
            Lock();
            //如果大小为零，那么队列已经空了
            if (size == 0)
            {
                UnLock();
                throw new Exception("队列已经空了");
            }
            T t = ary[front];
            //队头前移
            front++;
            //这一句是循环数组的关键：
            //如果front超过数组下标了，
            //那么将从头开始使用数组。
            front %= ary.Length;
            //大小减一
            size--;
            UnLock();
            return t;
        }
        /// <summary>
        /// 队大小
        /// </summary>
        public int Count
        {
            get
            {
                Lock();
                int result = size;
                UnLock();
                return result;
            }
        }
        /// <summary>
        /// 清除队中的元素
        /// </summary>
        public void Clear()
        {
            Lock();
            size = 0;
            front = 0;
            rear = 0;
            ary = new T[ary.Length];
            UnLock();
        }
        /// <summary>
        /// 数组大小加倍
        /// </summary>
        private void DoubleSize()
        {
            //临时数组
            T[] temp = new T[ary.Length];
            //将原始数组的内容拷贝到临时数组
            Array.Copy(ary, temp, ary.Length);
            //原始数组大小加倍
            ary = new T[ary.Length * 2];
            //将临时数组的内容拷贝到新数组中
            for (int i = 0; i < size; i++)
            {
                ary[i] = temp[front];
                front++;
                front %= temp.Length;
            }
            front = 0;
            rear = size;
        }
        /// <summary>
        /// 锁定
        /// </summary>
        private void Lock()
        {
            notice.WaitOne();
        }
        /// <summary>
        /// 解锁
        /// </summary>
        private void UnLock()
        {
            notice.Set();
        }

    }
    #endregion

    #region 回调数据类
    class CallBackData
    {
        protected static int ID;
        public int id;

        public Action fun;
        public float interval = 0;//延时或间隔多少秒（0，则每帧都刷，但不会立即执行，而是等到下一帧）
        public float timeout = -1;//重复调用间隔时间
        public bool ignoreTimeScale = true; //使用的时间类型 Time.realtimeSinceStartup为true ,Time.time为false
        public float targetTime = 0;//最终执行时间
        public CallBackData()
        {
            id = ID++;
        }

        public static int GetID() 
        {
            return ID;
        }
        virtual internal void Execute()
        {
            fun();
        }
    }
    /// <summary>
    /// 延迟回调带参数版本 避免闭包
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class CallBackData<T> : CallBackData
    {
        public new Action<T> fun;

        public T arg1;

        internal override void Execute()
        {
            if (fun != null)
                fun(arg1);
        }
    }
    /// <summary>
    /// 延迟回调带参数版本 避免闭包
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="U"></typeparam>
    class CallBackData<T, U> : CallBackData
    {
        public new Action<T, U> fun;

        public T arg1;

        public U arg2;

        internal override void Execute()
        {
            if (fun != null)
                fun(arg1, arg2);
        }

    }

    /// <summary>
    /// 延迟回调带参数版本 避免闭包
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="U"></typeparam>
    /// <typeparam name="V"></typeparam>
    class CallBackData<T, U, V> : CallBackData
    {
        public new Action<T, U, V> fun;

        public T arg1;

        public U arg2;

        public V arg3;

        internal override void Execute()
        {
            if (fun != null)
                fun(arg1, arg2, arg3);
        }
    }

    class CallBackData<T, U, V, A> : CallBackData
    {
        public new Action<T, U, V, A> fun;

        public T arg1;

        public U arg2;

        public V arg3;

        public A arg4;

        internal override void Execute()
        {
            if (fun != null)
                fun(arg1, arg2, arg3,arg4);
        }
    }

    #endregion

    # region InternalTick类
    public class InternalTick
    {
        float _realDeltaTime = 0;
        float pauseRealTime = 0;
        float pauseTimetime = 0;
        /// <summary>
        /// 存储internalTick对象
        /// </summary>
        private List<CallBackData> cbdList = null; 

        /// <summary>
        /// 需要移除CallBackData的ID
        /// </summary>
        private List<int> removeList = null;

        /// <summary>
        /// 添加线程安全能力
        /// </summary>
        private ThreadSafeQueue<CallBackData> safeQueue = null;
        internal InternalTick()
        {
            cbdList = new List<CallBackData>();
            removeList = new List<int>();
            safeQueue = new ThreadSafeQueue<CallBackData>();
        }

        #region SetTimeout 方法
        /// <summary>
        /// 延时运行
        /// </summary>
        /// <param name="action"></param>
        /// <param name="__interval"></param>
        /// <param name="ignoreTimeScale"></param>
        /// <returns></returns>
        public int SetTimeout(Action action, float __interval = 0, bool ignoreTimeScale = true)
        {
            var cbd = new CallBackData();
            cbd.fun = action;
            cbd.interval = __interval;
            cbd.ignoreTimeScale = ignoreTimeScale;
            cbd.targetTime = (cbd.ignoreTimeScale ? Time.realtimeSinceStartup : Time.time) + cbd.interval;

            safeQueue.Enqueue(cbd);
            return cbd.id;
        }

        public int SetTimeout<T>(Action<T> action,T arg1, float __interval = 0, bool ignoreTimeScale = true)
        {
            var cbd = new CallBackData<T>();
            cbd.fun = action;
            cbd.interval = __interval;
            cbd.ignoreTimeScale = ignoreTimeScale;
            cbd.targetTime = (cbd.ignoreTimeScale ? Time.realtimeSinceStartup : Time.time) + cbd.interval;

            cbd.arg1 = arg1;

            safeQueue.Enqueue(cbd);
            return cbd.id;
        }

        public int SetTimeout<T,U>(Action<T,U> action, T arg1, U arg2, float __interval = 0, bool ignoreTimeScale = true)
        {
            var cbd = new CallBackData<T,U>();
            cbd.fun = action;
            cbd.interval = __interval;
            cbd.ignoreTimeScale = ignoreTimeScale;
            cbd.targetTime = (cbd.ignoreTimeScale ? Time.realtimeSinceStartup : Time.time) + cbd.interval;

            cbd.arg1 = arg1;
            cbd.arg2 = arg2;

            safeQueue.Enqueue(cbd);
            return cbd.id;
        }

        public int SetTimeout<T,U,V>(Action<T, U, V> action, T arg1, U arg2, V arg3, float __interval = 0, bool ignoreTimeScale = true)
        {
            var cbd = new CallBackData<T, U, V>();
            cbd.fun = action;
            cbd.interval = __interval;
            cbd.ignoreTimeScale = ignoreTimeScale;
            cbd.targetTime = (cbd.ignoreTimeScale ? Time.realtimeSinceStartup : Time.time) + cbd.interval;

            cbd.arg1 = arg1;
            cbd.arg2 = arg2;
            cbd.arg3 = arg3;


            safeQueue.Enqueue(cbd);
            return cbd.id;
        }

        public int SetTimeout<T, U, V, A>(Action<T, U, V,A> action, T arg1, U arg2, V arg3, A arg4, float __interval = 0, bool ignoreTimeScale = true)
        {
            var cbd = new CallBackData<T, U, V, A>();
            cbd.fun = action;
            cbd.interval = __interval;
            cbd.ignoreTimeScale = ignoreTimeScale;
            cbd.targetTime = (cbd.ignoreTimeScale ? Time.realtimeSinceStartup : Time.time) + cbd.interval;

            cbd.arg1 = arg1;
            cbd.arg2 = arg2;
            cbd.arg3 = arg3;
            cbd.arg4 = arg4;

            safeQueue.Enqueue(cbd);
            return cbd.id;
        }
        #endregion

        /// <summary>
        /// 取消延迟运行
        /// </summary>
        /// <param name="tid"></param>
        /// <returns>移除的CallBackData的ID</returns>
        public bool ClearTimeout(int tid) {

            for (int i = cbdList.Count - 1; i >= 0; i--)
            {
                if (cbdList[i].id == tid)
                {
                    cbdList[i].id = -1;
                    return true;
                }
            }

            if (CallBackData.GetID() >= tid) 
            { 
                if (!removeList.Contains(tid))
                {
                    removeList.Add(tid);
                }
            }
            return false;
        }

        #region SetInterval 方法
        /// <summary>
        /// 定时运行
        /// </summary>
        /// <param name="action"></param>
        /// <param name="__interval"></param>
        /// <param name="ignoreTimeScale"></param>
        /// <returns></returns>

        public int SetInterval(Action action, float __interval = 0, bool ignoreTimeScale = true)
        {
            var cbd = new CallBackData();
            cbd.fun = action;
            cbd.interval = __interval;
            cbd.ignoreTimeScale = ignoreTimeScale;
            cbd.targetTime = (cbd.ignoreTimeScale ? Time.realtimeSinceStartup : Time.time) + cbd.interval;


            cbd.timeout = __interval;

            safeQueue.Enqueue(cbd);
            return cbd.id;
        }

        public int SetInterval<T>(Action<T> action, T arg1, float __interval = 0, bool ignoreTimeScale = true)
        {
            var cbd = new CallBackData<T>();
            cbd.fun = action;
            cbd.interval = __interval;
            cbd.ignoreTimeScale = ignoreTimeScale;
            cbd.targetTime = (cbd.ignoreTimeScale ? Time.realtimeSinceStartup : Time.time) + cbd.interval;

            cbd.timeout = __interval;
            cbd.arg1 = arg1;

            safeQueue.Enqueue(cbd);
            return cbd.id;
        }

        public int SetInterval<T, U>(Action<T, U> action, T arg1, U arg2, float __interval = 0, bool ignoreTimeScale = true)
        {
            var cbd = new CallBackData<T, U>();
            cbd.fun = action;
            cbd.interval = __interval;
            cbd.ignoreTimeScale = ignoreTimeScale;
            cbd.targetTime = (cbd.ignoreTimeScale ? Time.realtimeSinceStartup : Time.time) + cbd.interval;

            cbd.timeout = __interval;
            cbd.arg1 = arg1;
            cbd.arg2 = arg2;

            safeQueue.Enqueue(cbd);
            return cbd.id;
        }

        public int SetInterval<T, U, V>(Action<T, U, V> action, T arg1, U arg2, V arg3, float __interval = 0, bool ignoreTimeScale = true)
        {
            var cbd = new CallBackData<T, U, V>();
            cbd.fun = action;
            cbd.interval = __interval;
            cbd.ignoreTimeScale = ignoreTimeScale;
            cbd.targetTime = (cbd.ignoreTimeScale ? Time.realtimeSinceStartup : Time.time) + cbd.interval;

            cbd.timeout = __interval;
            cbd.arg1 = arg1;
            cbd.arg2 = arg2;
            cbd.arg3 = arg3;


            safeQueue.Enqueue(cbd);
            return cbd.id;
        }

        public int SetInterval<T, U, V, A>(Action<T, U, V, A> action, T arg1, U arg2, V arg3, A arg4, float __interval = 0, bool ignoreTimeScale = true)
        {
            var cbd = new CallBackData<T, U, V, A>();
            cbd.fun = action;
            cbd.interval = __interval;
            cbd.ignoreTimeScale = ignoreTimeScale;
            cbd.targetTime = (cbd.ignoreTimeScale ? Time.realtimeSinceStartup : Time.time) + cbd.interval;

            cbd.timeout = __interval;
            cbd.arg1 = arg1;
            cbd.arg2 = arg2;
            cbd.arg3 = arg3;
            cbd.arg4 = arg4;

            safeQueue.Enqueue(cbd);
            return cbd.id;
        }
        #endregion
        /// <summary>
        /// 取消定时运行
        /// </summary>
        /// <param name="tid"></param>
        /// <returns>移除的CallBackData的ID</returns>
        public bool ClearInterval(int tid) {
            return ClearTimeout(tid);
        }

        private bool isRunning = true;
        /// <summary>
        /// 暂停
        /// </summary>
        public void Pause()
        {
            pauseTimetime = Time.time;
            pauseRealTime = Time.realtimeSinceStartup;
            isRunning = false;
        }
        /// <summary>
        /// 继续
        /// </summary>
        public void Resume() 
        {
            _realDeltaTime = Time.realtimeSinceStartup - pauseRealTime;
            RefreshTargetTime();
            isRunning = true;
        }


        internal void Dispose()
        {
            cbdList.Clear();
            cbdList = null;
            removeList.Clear();
            removeList = null;
            safeQueue.Clear();
            safeQueue = null;
        }
        /// <summary>
        /// 刷新cbdList每个元素的最终执行时间（用于恢复暂停后）
        /// </summary>
        private void RefreshTargetTime() 
        {
            for (int i = cbdList.Count - 1; i >= 0; i--)
            {
                cbdList[i].targetTime = cbdList[i].targetTime + _realDeltaTime;
            }
           
            if (safeQueue.Count > 0)
            {
                AddDataFromSafeQueue();  
            }
            else 
            {
                _realDeltaTime = 0;
            }
        }

        /// <summary>
        ///  刷新线程安全队列单个元素的最终执行时间（用于恢复暂停后）
        /// </summary>
        /// <param name="_cbd"></param>
        private void RefreshTargetTimeBySafeQueueELement(CallBackData _cbd) 
        {
            if (!isRunning)
            {
                if (compareTime(_cbd))
                {
                    _cbd.targetTime = _cbd.targetTime + _realDeltaTime;
                }
                else
                {
                    var pauseTime = _cbd.ignoreTimeScale ? pauseRealTime : pauseTimetime;
                    _cbd.targetTime = _cbd.targetTime + _realDeltaTime - (_cbd.targetTime - _cbd.interval - pauseTime);
                }
            }
            else
            {
                _realDeltaTime = 0;
            }
        }
        
        /// <summary>
        /// 比较回调任务注册时间是否在暂停之前
        /// </summary>
        /// <param name="_cbd"></param>
        /// <returns></returns>
        private bool compareTime(CallBackData _cbd)
        {
            float originTime = _cbd.targetTime - _cbd.interval;

            return (_cbd.ignoreTimeScale ? pauseRealTime : pauseTimetime) >= originTime;
        }

        /// <summary>
        /// 插入排序（从大到小）
        /// </summary>
        /// <param name="array"></param>
        private void InsertSort(List<CallBackData> array)
        {
            for (int i = 1; i < array.Count; i++)
            {
                var temp = array[i];
                for (int j = i - 1; j >= 0; j--)
                {
                    if (array[j].targetTime < temp.targetTime)
                    {
                        array[j + 1] = array[j];
                        array[j] = temp;
                        temp = array[j];
                    }
                    else if (array[j].targetTime == temp.targetTime)
                    {
                        if (array[j].id < temp.id)
                        {
                            array[j + 1] = array[j];
                            array[j] = temp;
                            temp = array[j];
                        }
                    }
                    else
                        break;
                }
            }
        }
        #region 线程安全添加时间轴回调函数
        /// <summary>
        /// 处理线程队列里的回调数据，添加到cbdList
        /// </summary>
        private void AddDataFromSafeQueue() 
        {
            try
            {
                if (safeQueue.Count > 0)
                {
                    for (int i = 0; i < safeQueue.Count; i++)
                    {
                        CallBackData _cbd = safeQueue.Dequeue();
                        if (_cbd != null)
                        {
                            if (removeList.Contains(_cbd.id))
                            {
                                for (int j = removeList.Count - 1; j >= 0; j--)
                                {
                                    if (removeList[j] == _cbd.id)
                                    {
                                        removeList.RemoveAt(j);
                                    }
                                }
                            }
                            else
                            {
                                RefreshTargetTimeBySafeQueueELement(_cbd);
                                cbdList.Add(_cbd);
                            }
                        }
                    }
                    InsertSort(cbdList);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.ToString());
            }
        }
        #endregion

        /// <summary>
        /// 回调处理逻辑
        /// </summary>
        internal void UpdataLogic() 
        {
            if (!isRunning) { return; }

            AddDataFromSafeQueue();

            #region 处理事件回调函数
            try
            {
                if (cbdList.Count == 0)
                    return;

                bool isSort = false;
                for (int i = cbdList.Count - 1; i >= 0; i--)
                {
                    CallBackData _cbd = cbdList[i];
                    if (_cbd != null)
                    {
                        if( cbdList[i].id == -1 ) 
                        {
                            cbdList.RemoveAt( i );
                            continue;
                        }


                        var selfTime = _cbd.ignoreTimeScale ? Time.realtimeSinceStartup : Time.time;
                        if (_cbd.interval == 0 || selfTime >= _cbd.targetTime)
                        {
                            
                            if (_cbd.timeout != -1)
                            {
                                _cbd.targetTime = (_cbd.ignoreTimeScale ? Time.realtimeSinceStartup : Time.time) + _cbd.interval;
                                isSort = true;
                            }
                            else
                            {
                                cbdList.RemoveAt(i);
                            }

                            _cbd.Execute();
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                if (isSort) 
                {
                    InsertSort(cbdList);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.ToString());
            }
            #endregion
        }
    }
    #endregion
}

