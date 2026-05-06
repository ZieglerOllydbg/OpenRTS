// Flow Field Pathfinding System
// 流场寻路系统
//
// 这是一个基于定点数的流场寻路系统，专为RTS游戏设计
// 支持ECS架构，与RVO2无缝集成
//
// 核心文件：
// - IFlowFieldMap.cs          地图接口
// - FlowFieldCore.cs          核心数据结构和算法
// - FlowFieldManager.cs       流场管理器
// - FlowFieldComponents.cs    ECS组件
// - FlowFieldSystem.cs        ECS系统
// - SimpleMapManager.cs       简单地图实现
// - FlowFieldExample.cs       使用示例
//
// 快速开始：
// 1. 查看 FLOW_FIELD_README.md 了解系统架构
// 2. 参考 FlowFieldExample.cs 学习如何使用
// 3. 实现自己的 IFlowFieldMap 接口
// 4. 创建 FlowFieldNavigationSystem 并集成到游戏中
//
// 典型用法：
//   var mapManager = new SimpleMapManager();
//   var flowFieldMgr = new FlowFieldManager();
//   var rvoSim = new RVO2Simulator();
//   var navSystem = new FlowFieldNavigationSystem();
//   
//   navSystem.Initialize(flowFieldMgr, rvoSim);
//   var entity = navSystem.CreateEntity(id, startPos, config);
//   navSystem.SetEntityTarget(entity, targetPos);
//   navSystem.Update(deltaTime); // 每帧调用

namespace ZLockstep.Flow
{
    // 命名空间包含所有流场相关的类和接口
    // 请参考各个文件的详细实现
}

