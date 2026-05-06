using System.Collections.Generic;

namespace ZLockstep.Simulation.ECS.Components
{
    /// <summary>
    /// AI 行为类型枚举
    /// </summary>
    public enum AIActionType
    {
        None = 0,
        Produce = 1,      // 生产行为
        Attack = 2        // 攻击/追击行为
    }

    /// <summary>
    /// AI 全局组件
    /// 存储 AI 相关的全局配置和状态信息
    /// </summary>
    public struct AIComponent : IComponent
    {
        /// <summary>
        /// AI 阵营 ID
        /// </summary>
        public int AICampId;
        
        /// <summary>
        /// 启用的 AI 行为列表
        /// </summary>
        public List<AIActionType> EnabledActions;
        
        /// <summary>
        /// AI 是否启用
        /// </summary>
        public bool IsEnabled;
        
        /// <summary>
        /// AI 难度等级（1-简单，2-中等，3-困难）
        /// </summary>
        public int DifficultyLevel;
        
        public AIComponent(int aiCampId, List<AIActionType> enabledActions = null)
        {
            AICampId = aiCampId;
            EnabledActions = enabledActions ?? new List<AIActionType> 
            { 
                AIActionType.Produce, 
                AIActionType.Attack 
            };
            IsEnabled = true;
            DifficultyLevel = 1;
        }
    }
}