using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZLockstep.Simulation.ECS.Components
{
    /// <summary>
    /// 本地玩家组件
    /// 用于标识本地玩家控制的单位
    /// 
    /// 作用：
    /// 1. 标识本地玩家控制的单位实体
    /// 2. 区分本地玩家单位与AI或其他玩家的单位
    /// 3. 控制输入权限，只有带此组件的单位才能响应本地玩家的指令
    /// 
    /// 使用场合：
    /// 1. 多玩家游戏中区分不同玩家控制的单位
    /// 2. 输入控制系统，确保只有本地玩家单位响应玩家指令
    /// 3. UI显示，标识本地玩家单位
    /// 4. 相机控制，定位到本地玩家单位
    /// 
    /// 添加时机：
    /// 通常在创建单位时，通过比较单位的阵营ID与GlobalInfoComponent.LocalPlayerCampId来决定是否添加此组件
    /// 
    /// 查询方式：
    /// 使用World.ComponentManager.HasComponent<LocalPlayerComponent>(entity)检查实体是否为本地玩家控制
    /// </summary>
    public struct LocalPlayerComponent : IComponent
    {
    }
}