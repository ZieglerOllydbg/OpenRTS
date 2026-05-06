using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZLib
{

    #region 静态Tick类
    /// <summary>
    /// 静态Tick类
    /// </summary>
    public static class Tick
    {
        private static InternalTick instance = null;
        public static InternalTick Instance() { if (instance == null) { instance = PlayerMainLoop.TickManager().CreateInternal(); } return instance; }

        #region 延迟运行方法
        /// <summary>
        /// 延迟运行
        /// </summary>
        /// <param name="action"></param>
        /// <param name="__interval"></param>
        /// <param name="ignoreTimeScale"></param>
        /// <returns>tid</returns>
        public static int SetTimeout(Action action, float __interval = 0, bool ignoreTimeScale = true)
        {
            return Instance().SetTimeout(action, __interval, ignoreTimeScale);
        }
        public static int SetTimeout<T>(Action<T> action, T arg1, float __interval = 0, bool ignoreTimeScale = true)
        {
            return Instance().SetTimeout<T>(action, arg1, __interval, ignoreTimeScale);
        }
        public static int SetTimeout<T, U>(Action<T, U> action, T arg1, U arg2, float __interval = 0, bool ignoreTimeScale = true)
        {
            return Instance().SetTimeout<T, U>(action, arg1, arg2, __interval, ignoreTimeScale);
        }
        public static int SetTimeout<T, U, V>(Action<T, U, V> action, T arg1, U arg2, V arg3, float __interval = 0, bool ignoreTimeScale = true)
        {
            return Instance().SetTimeout<T, U, V>(action, arg1, arg2, arg3, __interval, ignoreTimeScale);
        }
        public static int SetTimeout<T, U, V, A>(Action<T, U, V, A> action, T arg1, U arg2, V arg3, A arg4, float __interval = 0, bool ignoreTimeScale = true)
        {
            return Instance().SetTimeout<T, U, V, A>(action, arg1, arg2, arg3, arg4, __interval, ignoreTimeScale);
        }
        #endregion
        /// <summary>
        /// 取消延迟运行
        /// </summary>
        /// <param name="tid"></param>
        /// <returns></returns>
        public static bool ClearTimeout(int tid)
        {
            return Instance().ClearTimeout(tid);
        }

        #region 定时运行方法
        /// <summary>
        /// 定时运行
        /// </summary>
        /// <param name="action"></param>
        /// <param name="__interval"></param>
        /// <param name="ignoreTimeScale"></param>
        /// <returns>tid</returns>
        public static int SetInterval(Action action, float __interval = 0, bool ignoreTimeScale = true)
        {
            return Instance().SetInterval(action, __interval, ignoreTimeScale);
        }
        public static int SetInterval<T>(Action<T> action, T arg1, float __interval = 0, bool ignoreTimeScale = true)
        {
            return Instance().SetInterval<T>(action, arg1, __interval, ignoreTimeScale);
        }
        public static int SetInterval<T, U>(Action<T, U> action, T arg1, U arg2, float __interval = 0, bool ignoreTimeScale = true)
        {
            return Instance().SetInterval<T, U>(action, arg1, arg2, __interval, ignoreTimeScale);
        }
        public static int SetInterval<T, U, V>(Action<T, U, V> action, T arg1, U arg2, V arg3, float __interval = 0, bool ignoreTimeScale = true)
        {
            return Instance().SetInterval<T, U, V>(action, arg1, arg2, arg3, __interval, ignoreTimeScale);
        }
        public static int SetInterval<T, U, V, A>(Action<T, U, V, A> action, T arg1, U arg2, V arg3, A arg4, float __interval = 0, bool ignoreTimeScale = true)
        {
            return Instance().SetInterval<T, U, V, A>(action, arg1, arg2, arg3, arg4, __interval, ignoreTimeScale);
        }
        #endregion
        /// <summary>
        /// 取消定时运行
        /// </summary>
        /// <param name="tid"></param>
        /// <returns></returns>
        public static bool ClearInterval(int tid)
        {
            return Instance().ClearInterval(tid);
        }
        /// <summary>
        /// 暂停
        /// </summary>
        public static void Pause()
        {
            Instance().Pause();
        }
        /// <summary>
        /// 继续
        /// </summary>
        public static void Resume()
        {
            Instance().Resume();
        }
    }
    #endregion

    #region 静态PlayerLoop类
    public static class PlayerLoop
    {
        private static InternalPlayerLoop instance = null;
        public static InternalPlayerLoop Instance() { if (instance == null) { instance = PlayerMainLoop.PlayerLoopManager().CreateInternal(); } return instance; }

        public static int SetUpdate(Action action) { return Instance().SetUpdate(action); }

        public static bool ClearUpdate(int id) { return Instance().ClearUpdate(id); }

        public static int SetLateUpdate(Action action) { return Instance().SetLateUpdate(action); }

        public static bool ClearLateUpdate(int id) { return Instance().ClearLateUpdate(id); }

        public static int SetFixedUpdate(Action action) { return Instance().SetFixedUpdate(action); }

        public static bool ClearFixedUpdate(int id) { return Instance().ClearFixedUpdate(id); }

        public static void Pause(UpdateType updateType = UpdateType.All) { Instance().Pause(updateType); }

        public static void Resume(UpdateType updateType = UpdateType.All) { Instance().Resume(updateType); }

    }
    #endregion

    #region PlayerMainLoop类
    public class PlayerMainLoop
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            var playerloop = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();

            { //添加Update
                var loop = new UnityEngine.LowLevel.PlayerLoopSystem
                {
                    type = typeof(PlayerMainLoop),
                    updateDelegate = Update
                };

                //找到Update这个subsystem
                int updateIndex = Array.FindIndex(playerloop.subSystemList, v => v.type == typeof(UnityEngine.PlayerLoop.Update));

                var updateloop = playerloop.subSystemList[updateIndex];

                var list = new List<UnityEngine.LowLevel.PlayerLoopSystem>();

                list.AddRange(updateloop.subSystemList);

                list.Add(loop);

                updateloop.subSystemList = list.ToArray();

                playerloop.subSystemList[updateIndex] = updateloop;
            }

            { //添加LateUpdate
                var lateUpdateLoop = new UnityEngine.LowLevel.PlayerLoopSystem
                {
                    type = typeof(PlayerMainLoop),
                    updateDelegate = LateUpdate
                };

                int lateUpdateIndex = Array.FindIndex(playerloop.subSystemList, v => v.type == typeof(UnityEngine.PlayerLoop.PostLateUpdate));

                var lateUpdateloop = playerloop.subSystemList[lateUpdateIndex];

                var lateUpdatelist = new List<UnityEngine.LowLevel.PlayerLoopSystem>();

                lateUpdatelist.AddRange(lateUpdateloop.subSystemList);

                lateUpdatelist.Add(lateUpdateLoop);

                lateUpdateloop.subSystemList = lateUpdatelist.ToArray();

                playerloop.subSystemList[lateUpdateIndex] = lateUpdateloop;
            }
            { //添加FixedUpdate
                var fixUpdateLoop = new UnityEngine.LowLevel.PlayerLoopSystem
                {
                    type = typeof(PlayerMainLoop),
                    updateDelegate = FixedUpdate
                };

                int fixedUpdateIndex = Array.FindIndex(playerloop.subSystemList, v => v.type == typeof(UnityEngine.PlayerLoop.FixedUpdate));

                var fixedUpdateSubsystem = playerloop.subSystemList[fixedUpdateIndex];

                var fixedUpdatelist = new List<UnityEngine.LowLevel.PlayerLoopSystem>();

                fixedUpdatelist.AddRange(fixedUpdateSubsystem.subSystemList);

                fixedUpdatelist.Add(fixUpdateLoop);

                fixedUpdateSubsystem.subSystemList = fixedUpdatelist.ToArray();

                playerloop.subSystemList[fixedUpdateIndex] = fixedUpdateSubsystem;
            }
            //3. 设置自定义的 Loop 到 Unity 引擎
            UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(playerloop);


#if UNITY_EDITOR
            //4. 已知：编辑器停止 Play 我们自己插入的 loop 依旧会触发，进入或退出Play 模式先清空 tasks
            UnityEditor.EditorApplication.playModeStateChanged -= EditorApplication_playModeStateChanged;
            UnityEditor.EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;
            static void EditorApplication_playModeStateChanged(UnityEditor.PlayModeStateChange obj)
            {
                if (obj == UnityEditor.PlayModeStateChange.ExitingEditMode || obj == UnityEditor.PlayModeStateChange.ExitingPlayMode)
                {
                    UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(UnityEngine.LowLevel.PlayerLoop.GetDefaultPlayerLoop());
                }
            }
#endif
        }

        private static InternalTickManager tickManager = null;
        public static InternalTickManager TickManager() { if (tickManager == null) { tickManager = InternalTickManager.Instance(); } return tickManager; }

        private static InternalPlayerLoopManager playerLoopManager = null;
        public static InternalPlayerLoopManager PlayerLoopManager() { if (playerLoopManager == null) { playerLoopManager = InternalPlayerLoopManager.Instance(); } return playerLoopManager; }

        /// <summary>
        /// tick管理类
        /// </summary>
        public class InternalTickManager
        {
            internal InternalTickManager() { }
            private static InternalTickManager instance = null;
            public static InternalTickManager Instance()
            {
                if (instance == null)
                {
                    instance = new InternalTickManager();
                    Tick.Instance();//确保Tick的InternalTick对象在队列第一位
                }
                return instance;
            }

            private List<InternalTick> ticks = new List<InternalTick>();

            /// <summary>
            /// 创建InternalTick实例
            /// </summary>
            /// <returns></returns>
            public InternalTick CreateInternal()
            {
                InternalTick tick = new InternalTick();
                Instance().ticks.Add(tick);
                return tick;
            }

            /// <summary>
            /// 移除InternalTick实例
            /// </summary>
            /// <param name="_tick"></param>
            public bool Dispose(InternalTick _tick)
            {
                var tmpTick = Instance().ticks;
                for (int i = tmpTick.Count - 1; i > 0; i--)//遍历剔除了第一个元素，保证静态类的InternalTick对象不会被移除
                {
                    if (tmpTick[i] == _tick)
                    {
                        var temp = tmpTick[i];
                        tmpTick.RemoveAt(i);
                        temp.Dispose();
                        return true;
                    }
                }
                return false;
            }

            internal void Update()
            {
                var tmpTick = Instance().ticks;
                for (int i = 0; i < tmpTick.Count; i++)
                {
                    if (tmpTick[i] != null)
                    {
                        tmpTick[i].UpdataLogic();
                    }
                }
            }
        }
        /// <summary>
        /// playerLoop管理类
        /// </summary>
        public class InternalPlayerLoopManager
        {
            internal InternalPlayerLoopManager() { }

            private static InternalPlayerLoopManager instance = null;
            public static InternalPlayerLoopManager Instance()
            {
                if (instance == null)
                {
                    instance = new InternalPlayerLoopManager();
                    PlayerLoop.Instance();//确保PlayerLoop的InternalPlayerLoop对象在队列第一位
                }
                return instance;
            }

            private List<InternalPlayerLoop> playerLoops = new List<InternalPlayerLoop>();
            /// <summary>
            /// 创建InternalPlayerLoop实例
            /// </summary>
            /// <returns></returns>
            public InternalPlayerLoop CreateInternal()
            {
                InternalPlayerLoop tick = new InternalPlayerLoop();
                Instance().playerLoops.Add(tick);
                return tick;
            }
            /// <summary>
            /// 移除InternalPlayerLoop对象
            /// </summary>
            /// <param name="_playerLoops"></param>
            /// <returns></returns>
            public bool Dispose(InternalPlayerLoop _playerLoops)
            {
                var tmpPlayerLoops = Instance().playerLoops;
                for (int i = tmpPlayerLoops.Count - 1; i > 0; i--)//遍历剔除了第一个元素，保证静态类的InternalPlayerLoop对象不会被移除
                {
                    if (tmpPlayerLoops[i] == _playerLoops)
                    {
                        var temp = tmpPlayerLoops[i];
                        tmpPlayerLoops.RemoveAt(i);
                        temp.Dispose();
                        return true;
                    }
                }
                return false;
            }

            internal void Update()
            {
                var tmpPlayerLoops = Instance().playerLoops;
                for (int i = 0; i < tmpPlayerLoops.Count; i++)
                {
                    if (tmpPlayerLoops[i] != null)
                    {
                        tmpPlayerLoops[i].UpdataLogic();
                    }
                }
            }

            internal void FixedUpdate()
            {
                var tmpPlayerLoops = Instance().playerLoops;
                for (int i = 0; i < tmpPlayerLoops.Count; i++)
                {
                    if (tmpPlayerLoops[i] != null)
                    {
                        tmpPlayerLoops[i].FixedUpdateLogic();
                    }
                }
            }

            internal void LateUpdate()
            {
                var tmpPlayerLoops = Instance().playerLoops;
                for (int i = 0; i < tmpPlayerLoops.Count; i++)
                {
                    if (tmpPlayerLoops[i] != null)
                    {
                        tmpPlayerLoops[i].LateUpdateLogic();
                    }
                }
            }
        }

        public static void Update()
        {
            TickManager().Update();
            PlayerLoopManager().Update();
        }

        public static void LateUpdate()
        {
            PlayerLoopManager().LateUpdate();
        }

        public static void FixedUpdate()
        {
            PlayerLoopManager().FixedUpdate();
        }
    }
}
#endregion



















