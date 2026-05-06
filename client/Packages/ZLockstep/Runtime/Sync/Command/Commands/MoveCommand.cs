using System.Linq;
using zUnity;
using ZLockstep.Simulation;
using ZLockstep.Simulation.ECS;
using ZLockstep.Simulation.ECS.Components;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using ZLockstep.Flow;

namespace ZLockstep.Sync.Command.Commands
{
    /// <summary>
    /// 移动命令
    /// 让一个或多个单位移动到指定位置
    /// </summary>
    [CommandType(CommandTypes.Move)]
    public class MoveCommand : BaseCommand
    {
        // 移除CommandType属性，使用特性替代
        // public override int CommandType => CommandTypes.Move;

        /// <summary>
        /// 要移动的实体ID列表
        /// </summary>
        public int[] EntityIds { get; set; }

        /// <summary>
        /// 目标位置
        /// </summary>
        public zVector3 TargetPosition { get; set; }

        /// <summary>
        /// 停止距离
        /// </summary>
        public zfloat StopDistance { get; set; }

        public MoveCommand(int campId, int[] entityIds, zVector3 targetPosition, zfloat stopDistance = default)
            : base(campId)
        {
            EntityIds = entityIds;
            TargetPosition = targetPosition;
            StopDistance = stopDistance > zfloat.Zero ? stopDistance : (zfloat)0.1f;
        }

        public override void Execute(zWorld world)
        {
            if (EntityIds == null || EntityIds.Length == 0)
            {
                UnityEngine.Debug.LogWarning("[MoveCommand] 没有指定要移动的实体！");
                return;
            }

            // 获取 StopGridManager（通过 world.GameInstance.GetNavSystem()）
            var game = world.GameInstance;
            var navSystem = game?.GetNavSystem();
            var stopGridManager = navSystem?.GetStopGridManager();

            // 收集所有单位的阵营信息
            List<int> campIds = new List<int>();
            foreach (var entityId in EntityIds)
            {
                var entity = new Entity(entityId);
                if (world.ComponentManager.HasComponent<CampComponent>(entity))
                {
                    var camp = world.ComponentManager.GetComponent<CampComponent>(entity);
                    campIds.Add(camp.CampId);
                }
                else
                {
                    campIds.Add(CampId); // 使用命令的阵营ID作为默认值
                }
            }

            // 批量分配格子（按选中顺序）
            List<zVector2> assignedPositions = null;
            if (stopGridManager != null)
            {
                zVector2 targetPos2D = new zVector2(TargetPosition.x, TargetPosition.z);
                assignedPositions = stopGridManager.AssignGrids(EntityIds.ToList(), campIds, targetPos2D);
            }

            int successCount = 0;
            for (int i = 0; i < EntityIds.Length; i++)
            {
                var entityId = EntityIds[i];
                var entity = new Entity(entityId);

                // 检查实体是否存在且属于该玩家
                if (!world.ComponentManager.HasComponent<UnitComponent>(entity))
                {
                    UnityEngine.Debug.LogWarning($"[MoveCommand] Entity_{entityId} 不存在或不是单位！");
                    continue;
                }

                var unit = world.ComponentManager.GetComponent<UnitComponent>(entity);
                if (unit.PlayerId != CampId)
                {
                    UnityEngine.Debug.LogWarning($"[MoveCommand] Entity_{entityId} 不属于玩家{CampId}！");
                    continue;
                }

                // 使用分配后的格子中心点作为目标位置
                zVector3 finalTarget = TargetPosition;
                if (assignedPositions != null && i < assignedPositions.Count)
                {
                    zVector2 assigned = assignedPositions[i];
                    finalTarget = new zVector3(assigned.x, zfloat.Zero, assigned.y);
                }

                // 添加移动命令组件
                var moveCmd = new MoveCommandComponent(finalTarget, StopDistance);
                world.ComponentManager.AddComponent(entity, moveCmd);

                successCount++;
            }

            if (successCount > 0)
            {
                zUDebug.Log($"[MoveCommand] 玩家{CampId} 命令{successCount}个单位移动到 {TargetPosition}");
            }
        }

        // tostring
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"[MoveCommand] 玩家{CampId} 命令{EntityIds.Length}个单位移动到 {TargetPosition}");
            return sb.ToString();
        }

    }

}