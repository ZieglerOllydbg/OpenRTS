using UnityEngine;
using ZLockstep.View;
using ZLockstep.Sync.Command;
using ZLockstep.Sync.Command.Commands;
using ZLockstep.Simulation.ECS;
using ZLockstep.Simulation.ECS.Components;
using zUnity;

/// <summary>
/// AI命令示例
/// 演示如何通过Command系统实现AI逻辑
/// </summary>
public class AICommandExample : MonoBehaviour
{
    [Header("设置")]
    [SerializeField] private GameWorldBridge worldBridge;
    [SerializeField] private int aiPlayerId = 1; // AI玩家ID
    [SerializeField] private float aiThinkInterval = 2f; // AI思考间隔（秒）

    private float _lastThinkTime;

    private void Update()
    {
        if (worldBridge == null || worldBridge.LogicWorld == null)
            return;

        // 每隔一段时间让AI思考
        if (Time.time - _lastThinkTime >= aiThinkInterval)
        {
            _lastThinkTime = Time.time;
            AIThink();
        }
    }

    /// <summary>
    /// AI思考逻辑
    /// </summary>
    private void AIThink()
    {
        // 获取AI的所有单位
        var aiUnits = GetAIUnits();

        if (aiUnits.Count == 0)
        {
            // 没有单位，尝试创建
            TryCreateUnit();
        }
        else
        {
            // 有单位，让它们执行任务
            AssignTasks(aiUnits);
        }
    }

    /// <summary>
    /// 获取AI的所有单位
    /// </summary>
    private System.Collections.Generic.List<int> GetAIUnits()
    {
        var aiUnits = new System.Collections.Generic.List<int>();
        var allUnits = worldBridge.LogicWorld.ComponentManager
            .GetAllEntityIdsWith<UnitComponent>();

        foreach (var entityId in allUnits)
        {
            var entity = new Entity(entityId);
            var unit = worldBridge.LogicWorld.ComponentManager.GetComponent<UnitComponent>(entity);
            if (unit.PlayerId == aiPlayerId)
            {
                aiUnits.Add(entityId);
            }
        }

        return aiUnits;
    }

    /// <summary>
    /// AI尝试创建单位
    /// </summary>
    private void TryCreateUnit()
    {
        // 在随机位置创建单位
        zVector3 randomPosition = new zVector3(
            new zRandom(0).NextZFloat((zfloat)(-10), (zfloat)10),
            zfloat.Zero,
            new zRandom(0).NextZFloat((zfloat)(-10), (zfloat)10)
        );

        var createCommand = new CreateUnitCommand(
            campId: aiPlayerId,
            unitType: 2, // 创建坦克
            position: randomPosition,
            prefabId: 2
        )
        {
            Source = CommandSource.AI
        };

        worldBridge.SubmitCommand(createCommand);
        Debug.Log($"[AI] 创建单位命令: 位置={randomPosition}");
    }

    /// <summary>
    /// 给AI单位分配任务
    /// </summary>
    private void AssignTasks(System.Collections.Generic.List<int> aiUnits)
    {
        // 简单AI：让单位在地图上巡逻
        zVector3 randomTarget = new zVector3(
            new zRandom(0).NextZFloat((zfloat)(-15), (zfloat)15),
            zfloat.Zero,
            new zRandom(0).NextZFloat((zfloat)(-15), (zfloat)15)
        );

        var moveCommand = new MoveCommand(
            campId: aiPlayerId,
            entityIds: aiUnits.ToArray(),
            targetPosition: randomTarget
        )
        {
            Source = CommandSource.AI
        };

        worldBridge.SubmitCommand(moveCommand);
        Debug.Log($"[AI] 移动{aiUnits.Count}个单位到 {randomTarget}");
    }
}

