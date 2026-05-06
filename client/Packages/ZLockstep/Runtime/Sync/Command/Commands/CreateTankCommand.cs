using zUnity;
using ZLockstep.Simulation;
using ZLockstep.Simulation.Events;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ZLockstep.Simulation.ECS.Components;
using ZLockstep.Flow;
using ZLockstep.Simulation.ECS;


namespace ZLockstep.Sync.Command.Commands
{
    /// <summary>
    /// 创建坦克命令
    /// </summary>
    [CommandType(CommandTypes.ProduceUnit)]
    public class CreateTankCommand : BaseCommand
    {

        /// <summary>
        /// 单位类型ID
        /// 1=动员兵, 2=犀牛坦克, 3=矿车
        /// </summary>
        public UnitType UnitType { get; set; }

        /// <summary>
        /// 创建位置
        /// </summary>
        public zVector3 Position { get; set; }

        public CreateTankCommand(int campId, UnitType unitType, zVector3 position)
            : base(campId)
        {
            UnitType = unitType;
            Position = position;
        }

        public override void Execute(zWorld world)
        {
            // 使用EntityCreationManager创建单位实体
            var unitType = (UnitType)this.UnitType;
            var unitCreatedEvent = EntityCreationManager.CreateUnitEntity(
                world,
                CampId,
                unitType,
                Position,
                0);

            // 发布事件
            if (unitCreatedEvent.HasValue)
            {
                world.EventManager.Publish(unitCreatedEvent.Value);
                zUDebug.Log($"[CreateTankCommand] 玩家{CampId} 阵营{CampId} 创建了坦克在位置{Position}");
            }
            else
            {
                zUDebug.LogError($"[CreateTankCommand] 玩家{CampId} 创建坦克失败");
            }
        }

        // tostring
        public override string ToString()
        {
            return $"[CreateTankCommand] 玩家{CampId} 阵营{CampId} 创建了坦克在位置{Position}";
        }
    }
}