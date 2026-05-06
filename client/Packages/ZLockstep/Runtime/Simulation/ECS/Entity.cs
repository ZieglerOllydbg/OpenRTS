namespace ZLockstep.Simulation.ECS
{
    /// <summary>
    /// 代表游戏世界中的一个实体。
    /// 它只是一个唯一的标识符，不包含任何数据或逻辑。
    /// </summary>
    public struct Entity
    {
        public readonly int Id;

        public Entity(int id)
        {
            Id = id;
        }

        public override bool Equals(object obj)
        {
            return obj is Entity entity && Id == entity.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static bool operator ==(Entity left, Entity right)
        {
            return left.Id == right.Id;
        }

        public static bool operator !=(Entity left, Entity right)
        {
            return left.Id != right.Id;
        }
    }
}
