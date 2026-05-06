using System.Collections.Generic;
using zUnity;
using ZLockstep.Simulation;
using ZLockstep.Simulation.ECS;
using ZLockstep.Simulation.ECS.Components;
using System.Text;

namespace ZLockstep.Sync.Command.Commands
{
    /// <summary>
    /// 实体移动命令
    /// 直接使用导航系统设置移动目标
    /// </summary>
    
    [CommandType(CommandTypes.EntityMove)]
    public class EntityMoveCommand : BaseCommand
    {
        /// <summary>
        /// 要移动的实体ID列表
        /// </summary>
        public int[] EntityIds { get; set; }

        /// <summary>
        /// 目标位置
        /// </summary>
        public zVector2 TargetPosition { get; set; }

        /// <summary>
        /// 是否启用流场寻路。
        /// 启用时会优先走编队分配/流场路径；关闭时按单体直接设置移动目标。
        /// </summary>
        public bool UseFlowfield { get; set; }

        /// <summary>
        /// 是否标记为用户输入来源（写入 MoveTargetComponent.UserInput）。
        /// </summary>
        public bool UserInput { get; set; }

        /// <summary>
        /// 创建实体移动命令。
        /// </summary>
        /// <param name="campId">命令所属阵营ID。</param>
        /// <param name="entityIds">需要移动的实体ID列表。</param>
        /// <param name="targetPosition">移动目标位置。</param>
        /// <param name="useFlowfield">是否启用流场寻路，默认启用。</param>
        /// <param name="userInput">是否标记为用户输入，默认启用。</param>
        public EntityMoveCommand(int campId, int[] entityIds, zVector2 targetPosition, bool useFlowfield = true, bool userInput = true)
            : base(campId)
        {
            EntityIds = entityIds;
            TargetPosition = targetPosition;
            UseFlowfield = useFlowfield;
            UserInput = userInput;
        }

        /// <summary>
        /// 执行移动命令并将目标下发到导航系统。
        /// </summary>
        /// <param name="world">当前逻辑世界实例。</param>
        public override void Execute(zWorld world)
        {
            if (EntityIds == null || EntityIds.Length == 0)
            {
                UnityEngine.Debug.LogWarning("[EntityMoveCommand] 没有指定要移动的实体！");
                return;
            }

            // 通过world获取Game实例
            var game = world.GameInstance;
            if (game != null)
            {
                // 通过Game实例获取NavSystem
                var navSystem = game.GetNavSystem();
                if (navSystem != null)
                {
                    // 启用流场且是多单位时，保持编队分配路径
                    if (UseFlowfield && EntityIds.Length > 1)
                    {
                        List<Entity> entities = new List<Entity>();
                        foreach (var entityId in EntityIds)
                        {
                            entities.Add(new Entity(entityId));
                        }

                        // 使用 CampId 进行格子分配
                        navSystem.SetMultipleTargets(entities, TargetPosition, CampId, UserInput);
                        zUDebug.Log($"[EntityMoveCommand] 移动{entities.Count}个单位到{TargetPosition}，阵营{CampId}，Flowfield={UseFlowfield}，UserInput={UserInput}");
                    }
                    else
                    {
                        // 单位逐个设置目标，支持关闭流场语义
                        for (int i = 0; i < EntityIds.Length; i++)
                        {
                            var entity = new Entity(EntityIds[i]);
                            navSystem.SetMoveTarget(entity, TargetPosition, UserInput, UseFlowfield);
                        }

                        zUDebug.Log($"[EntityMoveCommand] 移动{EntityIds.Length}个单位到{TargetPosition}，阵营{CampId}，Flowfield={UseFlowfield}，UserInput={UserInput}");
                    }
                }
                else
                {
                    UnityEngine.Debug.LogWarning("[EntityMoveCommand] NavSystem未找到！");
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning("[EntityMoveCommand] Game实例未找到！");
            }
        }

        /// <summary>
        /// 返回命令的可读字符串。
        /// </summary>
        /// <returns>命令描述字符串。</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"[EntityMoveCommand] 玩家{CampId} 命令{EntityIds.Length}个单位移动到 {TargetPosition}");
            return sb.ToString();
        }
    }
}
