using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZLockstep.Simulation.ECS.Components
{
    /// <summary>
    /// 全局信息组件
    /// 存储全局游戏信息，如本地玩家ID等
    /// </summary>
    public struct GlobalInfoComponent : IComponent
    {
        /// <summary>
        /// 本地玩家ID
        /// 用于标识当前客户端控制的玩家
        /// </summary>
        public int LocalPlayerCampId;
        
        /// <summary>
        /// 游戏帧数
        /// </summary>
        public int FrameCount;
        
        /// <summary>
        /// 游戏是否暂停
        /// </summary>
        public bool IsPaused;
        
        /// <summary>
        /// 游戏速度
        /// </summary>
        public float GameSpeed;
        
        public GlobalInfoComponent(int localPlayerCampId)
        {
            LocalPlayerCampId = localPlayerCampId;
            FrameCount = 0;
            IsPaused = false;
            GameSpeed = 1.0f;
        }
    }
}