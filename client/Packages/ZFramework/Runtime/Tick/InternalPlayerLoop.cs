using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZLib
{
    /// <summary>
    /// update类型枚举
    /// </summary>
    public enum UpdateType 
    {
        All = 0,
        Update = 1,
        LateUpdate = 2,
        FixedUpdate = 3,
    }

    #region 事件数据类
    class ActionData
    {
        protected static int ID ;
        public int id;
        public int oid;
        public Action Fun;

        public ActionData() 
        {
            id = ID++;
            oid = 0;
        }
        public void Execute() 
        {
            Fun();
        }
    }
    #endregion

    #region InternalPlayerLoop类
    public class InternalPlayerLoop
    {
        internal InternalPlayerLoop() 
        {
            updateList = new List<ActionData>();
            lateUpdateList = new List<ActionData>();
            fixedUpdate = new List<ActionData>();
        }
        List<ActionData> updateList = null;
        List<ActionData> lateUpdateList = null;
        List<ActionData> fixedUpdate = null;

        /// <summary>
        /// 添加每帧更新回调
        /// </summary>
        /// <param name="action">回调</param>
        public int SetUpdate(Action action) 
        {
            for (int i = updateList.Count - 1; i >= 0; i--)
            {
                var item = updateList[i];
                if (item != null)
                {
                    if (item.Fun == action)
                    {
                        if (item.id == -1) {
                            item.id = item.oid;
                        }
                        return item.id;
                    }
                }
            }
            ActionData _acitem = new ActionData();
            _acitem.Fun = action;
            updateList.Add(_acitem);
            return _acitem.id;
        }
        /// <summary>
        /// 移除每帧更新回调
        /// </summary>
        /// <param name="id">回调</param>
        public bool ClearUpdate(int id) 
        {
            for (int i = updateList.Count - 1; i >= 0; i--)
            {
                var item = updateList[i];
                if (item != null) 
                {
                    if (item.id == id) 
                    {
                        item.oid = item.id;
                        item.id = -1;
                        //updateList.RemoveAt(i);
                        return true;
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// 添加每帧更新回调
        /// </summary>
        /// <param name="action">回调</param>
        public int SetLateUpdate(Action action)
        {
            for (int i = lateUpdateList.Count - 1; i >= 0; i--)
            {
                var item = lateUpdateList[i];
                if (item != null)
                {
                    if (item.Fun == action)
                    {
                        if (item.id == -1)
                        {
                            item.id = item.oid;
                        }
                        return item.id;
                    }
                }
            }
            ActionData _acitem = new ActionData();
            _acitem.Fun = action;
            lateUpdateList.Add(_acitem);
            return _acitem.id;
        }
        /// <summary>
        /// 移除每帧更新回调
        /// </summary>
        /// <param name="id">回调</param>
        public bool ClearLateUpdate(int id)
        {
            for (int i = lateUpdateList.Count - 1; i >= 0; i--)
            {
                var item = lateUpdateList[i];
                if (item != null)
                {
                    if (item.id == id)
                    {
                        item.oid = item.id;
                        item.id = -1;
                        //lateUpdateList.RemoveAt(i);
                        return true;
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// 添加每帧更新回调
        /// </summary>
        /// <param name="action">回调</param>
        public int SetFixedUpdate(Action action)
        {
            for (int i = fixedUpdate.Count - 1; i >= 0; i--)
            {
                var item = fixedUpdate[i];
                if (item != null)
                {
                    if (item.Fun == action)
                    {
                        if (item.id == -1)
                        {
                            item.id = item.oid;
                        }
                        return item.id;
                    }
                }
            }
            ActionData _acitem = new ActionData();
            _acitem.Fun = action;
            fixedUpdate.Add(_acitem);
            return _acitem.id;
        }
        /// <summary>
        /// 移除每帧更新回调
        /// </summary>
        /// <param name="id">回调</param>
        public bool ClearFixedUpdate(int id)
        {
            for (int i = fixedUpdate.Count - 1; i >= 0; i--)
            {
                var item = fixedUpdate[i];
                if (item != null)
                {
                    if (item.id == id)
                    {
                        item.oid = item.id;
                        item.id = -1;
                        //fixedUpdate.RemoveAt(i);
                        return true;
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// 是否运行中
        /// </summary>
        private bool runUpdate = true;
        private bool runLateUpdate = true;
        private bool runFixedUpdate = true;
        /// <summary>
        /// 暂停（UpdateType.none默认全停）
        /// </summary>
        /// <param name="uptype"></param>
        public void Pause(UpdateType uptype = UpdateType.All) 
        {
            switch (uptype) 
            {
                case UpdateType.Update:
                {
                    runUpdate = false;
                    break;
                }
                case UpdateType.LateUpdate:
                {
                    runLateUpdate = false;
                    break;
                }
                case UpdateType.FixedUpdate:
                {
                    runFixedUpdate = false;
                    break;
                }
                default:
                    runUpdate = false;
                    runLateUpdate = false;
                    runFixedUpdate = false;
                break;
            }
        }
        /// <summary>
        /// 继续（UpdateType.none默认全恢复）
        /// </summary>
        /// <param name="uptype"></param>
        public void Resume(UpdateType uptype = UpdateType.All)
        {
            switch (uptype)
            {
                case UpdateType.Update:
                    {
                        runUpdate = true;
                        break;
                    }
                case UpdateType.LateUpdate:
                    {
                        runLateUpdate = true;
                        break;
                    }
                case UpdateType.FixedUpdate:
                    {
                        runFixedUpdate = true;
                        break;
                    }
                default:
                    runUpdate = true;
                    runLateUpdate = true;
                    runFixedUpdate = true;
                    break;
            }
        }
        /// <summary>
        /// 刷帧
        /// 需要外部主动调用
        /// </summary>
        internal void UpdataLogic() 
        {
            if (!runUpdate) { return; }

            for (int i = updateList.Count - 1; i >= 0; i--)
            {
                if (updateList[i].id == -1)
                {
                    updateList.RemoveAt(i);
                    continue;
                }
                try
                {
                    updateList[i].Execute();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
        /// <summary>
        /// 刷帧
        /// 需要外部主动调用
        /// </summary>
        internal void FixedUpdateLogic()
        {
            if (!runLateUpdate) { return; }
            for (int i = fixedUpdate.Count - 1; i >= 0; i--)
            {
                if (fixedUpdate[i].id == -1)
                {
                    fixedUpdate.RemoveAt(i);
                    continue;
                }
                try
                {
                    fixedUpdate[i].Execute();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

        }
        /// <summary>
        /// 刷帧
        /// 需要外部主动调用
        /// </summary>
        internal void LateUpdateLogic()
        {
            if (!runFixedUpdate) { return; }
            for (int i = lateUpdateList.Count - 1; i >= 0; i--)
            {
                if (lateUpdateList[i].id == -1)
                {
                    lateUpdateList.RemoveAt(i);
                    continue;
                }
                try
                {
                    lateUpdateList[i].Execute();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        internal void Dispose() 
        {
           updateList.Clear();
           updateList = null;
           lateUpdateList.Clear();
           lateUpdateList = null;
           fixedUpdate.Clear();
           fixedUpdate = null;
        }
    }
    #endregion
}
