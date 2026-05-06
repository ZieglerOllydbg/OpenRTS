using System;
using System.Collections.Generic;
using System.Linq;

namespace ZLockstep.Simulation.Events
{
    /// <summary>
    /// 事件管理器
    /// 采用"拉"模式以保证确定性。System负责在自己的更新周期内主动查询和处理事件。
    /// 
    /// 时序说明：
    /// - Publish(): 在当前帧发布事件（由System调用）
    /// - GetEvents(): 获取事件（可在同帧或下一帧调用）
    /// - Clear(): 清空事件（应在下一帧开始前调用）
    /// 
    /// 典型流程：
    ///   Frame N:
    ///     1. Clear() - 清空上一帧的事件
    ///     2. CommandManager.Execute() → Publish事件
    ///     3. SystemManager.Update() → Publish事件
    ///     4. PresentationSystem.Update() → GetEvents处理
    ///   Frame N+1:
    ///     1. Clear() - 清空Frame N的事件
    ///     ...
    /// </summary>
    public class EventManager
    {
        // Key: Event Type, Value: List of event instances
        private readonly Dictionary<Type, List<IEvent>> _eventLists = 
            new Dictionary<Type, List<IEvent>>();

        /// <summary>
        /// 发布一个事件。事件会被加入对应类型的列表中，等待System来处理。
        /// </summary>
        public void Publish<T>(T gameEvent) where T : IEvent
        {
            var eventType = typeof(T);
            if (!_eventLists.ContainsKey(eventType))
            {
                _eventLists[eventType] = new List<IEvent>();
            }
            _eventLists[eventType].Add(gameEvent);
        }

        /// <summary>
        /// 获取指定类型的所有事件（只读）
        /// System应在自己的Update方法中调用此方法来处理事件。
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <returns>只读的事件列表</returns>
        public IReadOnlyList<T> GetEvents<T>() where T : IEvent
        {
            var eventType = typeof(T);
            if (_eventLists.TryGetValue(eventType, out var list))
            {
                // 直接转换，List支持协变
                return list.Cast<T>().ToList().AsReadOnly();
            }
            return Array.Empty<T>();
        }

        /// <summary>
        /// 清空所有事件
        /// 应在下一帧开始时调用（不是当前帧结束时）
        /// </summary>
        public void Clear()
        {
            foreach (var list in _eventLists.Values)
            {
                list.Clear();
            }
        }
    }
}
