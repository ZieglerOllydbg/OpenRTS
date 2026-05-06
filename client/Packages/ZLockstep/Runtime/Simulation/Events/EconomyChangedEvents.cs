using System;

namespace ZLockstep.Simulation.Events
{
    /// <summary>
    /// 金币变化事件
    /// 当玩家的金币发生变化时触发
    /// </summary>
    public struct MoneyChangedEvent : IEvent
    {
        /// <summary>
        /// 玩家阵营ID
        /// </summary>
        public int CampId;

        /// <summary>
        /// 变化前的金币值
        /// </summary>
        public int OldMoney;

        /// <summary>
        /// 变化后的金币值
        /// </summary>
        public int NewMoney;

        /// <summary>
        /// 金币变化的原因（可选）
        /// </summary>
        public string Reason;
    }

    /// <summary>
    /// 能量变化事件
    /// 当玩家的能量发生变化时触发
    /// </summary>
    public struct PowerChangedEvent : IEvent
    {
        /// <summary>
        /// 玩家阵营ID
        /// </summary>
        public int CampId;

        /// <summary>
        /// 变化前的能量值
        /// </summary>
        public int OldPower;

        /// <summary>
        /// 变化后的能量值
        /// </summary>
        public int NewPower;

        /// <summary>
        /// 能量变化的原因（可选）
        /// </summary>
        public string Reason;
    }

    /// <summary>
    /// 建筑被摧毁事件
    /// </summary>
    public struct BuildingDestroyedEvent : IEvent
    {
        /// <summary>
        /// 玩家阵营 ID
        /// </summary>
        public int CampId;

        /// <summary>
        /// 建筑实体 ID
        /// </summary>
        public int EntityId;

        /// <summary>
        /// 建筑类型
        /// </summary>
        public int BuildingType;

        /// <summary>
        /// 摧毁原因
        /// </summary>
        public string Reason;
    }
}