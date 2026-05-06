using zUnity;

namespace ZLockstep.Simulation.ECS.Components
{
    /// <summary>
    /// 载具类型组件
    /// 定义不同单位的旋转特性
    /// </summary>
    public struct VehicleTypeComponent : IComponent
    {
        /// <summary>
        /// 载具类型枚举
        /// </summary>
        public enum VehicleType
        {
            Infantry = 0,      // 步兵：无旋转限制，快速转向
            LightVehicle = 1,  // 轻型载具：快速旋转，较大的行进转向阈值
            HeavyTank = 2,     // 重型坦克：慢速旋转，小阈值需原地转向
            Building = 3       // 建筑：只有炮塔旋转，车体不转
        }

        /// <summary>
        /// 载具类型
        /// </summary>
        public VehicleType Type;

        /// <summary>
        /// 车体旋转角速度（度/秒）
        /// </summary>
        public zfloat BodyRotationSpeed;

        /// <summary>
        /// 原地转向阈值（度）
        /// 当期望朝向与当前朝向的角度差超过此值时，单位必须停止移动进行原地转向
        /// </summary>
        public zfloat InPlaceRotationThreshold;

        /// <summary>
        /// 是否有炮塔
        /// </summary>
        public bool HasTurret;

        /// <summary>
        /// 炮塔旋转角速度（度/秒）
        /// </summary>
        public zfloat TurretRotationSpeed;

        /// <summary>
        /// 创建步兵载具类型
        /// </summary>
        public static VehicleTypeComponent CreateInfantry()
        {
            return new VehicleTypeComponent
            {
                Type = VehicleType.Infantry,
                BodyRotationSpeed = new zfloat(360),  // 360度/秒，快速转向
                InPlaceRotationThreshold = new zfloat(180), // 180度，基本不需要原地转向
                HasTurret = false,
                TurretRotationSpeed = zfloat.Zero
            };
        }

        /// <summary>
        /// 创建轻型载具类型
        /// </summary>
        public static VehicleTypeComponent CreateLightVehicle()
        {
            return new VehicleTypeComponent
            {
                Type = VehicleType.LightVehicle,
                BodyRotationSpeed = new zfloat(180),  // 180度/秒
                InPlaceRotationThreshold = new zfloat(60), // 60度以上需要原地转向
                HasTurret = true,
                TurretRotationSpeed = new zfloat(240) // 240度/秒
            };
        }

        /// <summary>
        /// 创建重型坦克类型
        /// </summary>
        public static VehicleTypeComponent CreateHeavyTank()
        {
            return new VehicleTypeComponent
            {
                Type = VehicleType.HeavyTank,
                BodyRotationSpeed = new zfloat(120),  // 120度/秒，转向较慢
                InPlaceRotationThreshold = new zfloat(30), // 30度以上需要原地转向
                HasTurret = true,
                TurretRotationSpeed = new zfloat(180) // 180度/秒
            };
        }

        /// <summary>
        /// 创建建筑类型（只有炮塔旋转）
        /// </summary>
        public static VehicleTypeComponent CreateBuilding()
        {
            return new VehicleTypeComponent
            {
                Type = VehicleType.Building,
                BodyRotationSpeed = zfloat.Zero,  // 建筑不旋转
                InPlaceRotationThreshold = new zfloat(360), // 永远不原地转向
                HasTurret = true,
                TurretRotationSpeed = new zfloat(120) // 120度/秒
            };
        }
    }
}

