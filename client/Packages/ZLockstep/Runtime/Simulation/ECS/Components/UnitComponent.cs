namespace ZLockstep.Simulation.ECS.Components
{
    /// <summary>
    /// 单位组件
    /// 存储单位的基础属性
    /// </summary>
    public struct UnitComponent : IComponent
    {
        /// <summary>
        /// 单位类型ID（用于区分不同单位）
        /// 例如：1=动员兵, 2=犀牛坦克, 3=矿车
        /// </summary>
        public UnitType UnitType;

        /// <summary>
        /// 移动速度（单位/秒）
        /// </summary>
        public zfloat MoveSpeed;

        /// <summary>
        /// 旋转速度（度/秒）
        /// </summary>
        public zfloat RotateSpeed;

        /// <summary>
        /// 单位所属玩家ID
        /// </summary>
        public int PlayerId;

        /// <summary>
        /// 单位选择半径（用于点选）
        /// </summary>
        public zfloat SelectionRadius;

        /// <summary>
        /// 是否可选中
        /// </summary>
        public bool IsSelectable;

        public int PrefabId;

        public static UnitComponent CreateInfantry(int playerId)
        {
            return new UnitComponent
            {
                UnitType = UnitType.Infantry,
                MoveSpeed = (zfloat)3.0f,
                RotateSpeed = (zfloat)180.0f,
                PlayerId = playerId,
                SelectionRadius = (zfloat)0.5f,
                IsSelectable = true
            };
        }

        public static UnitComponent CreateTank(int playerId)
        {
            return new UnitComponent
            {
                UnitType = UnitType.grizzlyTank,
                MoveSpeed = (zfloat)5.0f,
                RotateSpeed = (zfloat)120.0f,
                PlayerId = playerId,
                SelectionRadius = (zfloat)1.0f,
                IsSelectable = true
            };
        }

        public static UnitComponent CreateHarvester(int playerId)
        {
            return new UnitComponent
            {
                UnitType = UnitType.Harvester,
                MoveSpeed = (zfloat)4.0f,
                RotateSpeed = (zfloat)90.0f,
                PlayerId = playerId,
                SelectionRadius = (zfloat)1.5f,
                IsSelectable = true
            };
        }
    }
}

