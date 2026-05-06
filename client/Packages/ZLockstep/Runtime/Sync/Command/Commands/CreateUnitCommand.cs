using zUnity;
using ZLockstep.Simulation;
using ZLockstep.Simulation.Events;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ZLockstep.Sync.Command.Commands
{
    /// <summary>
    /// 创建单位命令
    /// </summary>
    [CommandType(CommandTypes.CreateUnit)]
    public class CreateUnitCommand : BaseCommand
    {
        // 移除CommandType属性，使用特性替代
        // public override int CommandType => CommandTypes.CreateUnit;

        /// <summary>
        /// 单位类型ID
        /// 1=动员兵, 2=犀牛坦克, 3=矿车
        /// </summary>
        public int UnitType { get; set; }


        /// <summary>
        /// 创建位置
        /// </summary>
        public zVector3 Position { get; set; }

        /// <summary>
        /// 单位预制体ID（用于表现层）
        /// </summary>
        public int PrefabId { get; set; }

        public CreateUnitCommand(int campId, int unitType, zVector3 position, int prefabId = 0)
            : base(campId)
        {
            UnitType = unitType;
            Position = position;
            PrefabId = prefabId;
        }

        public override void Execute(zWorld world)
        {
            // 在逻辑层创建单位实体
            var entity = world.EntityManager.CreateEntity();

            // 添加Transform组件
            world.ComponentManager.AddComponent(entity, new Simulation.ECS.Components.TransformComponent
            {
                Position = Position,
                Rotation = zQuaternion.identity,
                Scale = zVector3.one
            });

            // 添加Unit组件（根据类型）
            var unitComponent = CreateUnitComponent(UnitType);
            world.ComponentManager.AddComponent(entity, unitComponent);

            // 添加Health组件
            var healthComponent = CreateHealthComponent(UnitType);
            world.ComponentManager.AddComponent(entity, healthComponent);

            // 如果单位有攻击能力，添加Attack组件
            if (CanAttack(UnitType))
            {
                var attackComponent = CreateAttackComponent(UnitType);
                world.ComponentManager.AddComponent(entity, attackComponent);
            }

            // 发送事件通知表现层创建视图
            // PresentationSystem会在同一帧内处理此事件并创建GameObject
            world.EventManager.Publish(new UnitCreatedEvent
            {
                EntityId = entity.Id,
                UnitType = UnitType,
                Position = Position,
                PlayerId = CampId,
                PrefabId = PrefabId
            });

            zUDebug.Log($"[CreateUnitCommand] 玩家{CampId} 创建了单位类型{UnitType} 在位置{Position}，Entity ID: {entity.Id}");
        }

        #region 辅助方法

        private Simulation.ECS.Components.UnitComponent CreateUnitComponent(int unitType)
        {
            switch (unitType)
            {
                case 1: // 动员兵
                    return Simulation.ECS.Components.UnitComponent.CreateInfantry(CampId);
                case 2: // 犀牛坦克
                    return Simulation.ECS.Components.UnitComponent.CreateTank(CampId);
                case 3: // 矿车
                    return Simulation.ECS.Components.UnitComponent.CreateHarvester(CampId);
                default:
                    return Simulation.ECS.Components.UnitComponent.CreateInfantry(CampId);
            }
        }

        private Simulation.ECS.Components.HealthComponent CreateHealthComponent(int unitType)
        {
            switch (unitType)
            {
                case 1: // 动员兵
                    return new Simulation.ECS.Components.HealthComponent((zfloat)50.0f);
                case 2: // 犀牛坦克
                    return new Simulation.ECS.Components.HealthComponent((zfloat)200.0f);
                case 3: // 矿车
                    return new Simulation.ECS.Components.HealthComponent((zfloat)150.0f);
                default:
                    return new Simulation.ECS.Components.HealthComponent((zfloat)100.0f);
            }
        }

        private Simulation.ECS.Components.AttackComponent CreateAttackComponent(int unitType)
        {
            switch (unitType)
            {
                case 1: // 动员兵
                    return new Simulation.ECS.Components.AttackComponent
                    {
                        Range = (zfloat)4.0f,
                        AttackInterval = (zfloat)1.0f,
                        TimeSinceLastAttack = zfloat.Zero,
                        TargetEntityId = -1
                    };
                case 2: // 犀牛坦克
                    return new Simulation.ECS.Components.AttackComponent
                    {
                        Range = (zfloat)8.0f,
                        AttackInterval = (zfloat)2.0f,
                        TimeSinceLastAttack = zfloat.Zero,
                        TargetEntityId = -1
                    };
                default:
                    return Simulation.ECS.Components.AttackComponent.CreateDefault();
            }
        }

        private bool CanAttack(int unitType)
        {
            switch (unitType)
            {
                case 1: // 动员兵
                case 2: // 犀牛坦克
                    return true;
                case 3: // 矿车
                    return false;
                default:
                    return false;
            }
        }

        #endregion
    

        // tostring
        public override string ToString()
        {
            return $"[CreateUnitCommand] 玩家{CampId} 创建了单位类型{UnitType} 在位置{Position} PrefabId: {PrefabId}";
        }
    }
}