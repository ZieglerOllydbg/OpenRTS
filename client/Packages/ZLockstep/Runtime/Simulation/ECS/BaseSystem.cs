namespace ZLockstep.Simulation.ECS
{
    /// <summary>
    /// 系统基类
    /// 提供系统的基本功能实现
    /// </summary>
    public abstract class BaseSystem : ISystem
    {
        /// <summary>
        /// 游戏世界引用
        /// </summary>
        protected zWorld World { get; private set; }

        /// <summary>
        /// 实体管理器（快捷访问）
        /// </summary>
        protected EntityManager EntityManager => World.EntityManager;

        /// <summary>
        /// 组件管理器（快捷访问）
        /// </summary>
        protected ComponentManager ComponentManager => World.ComponentManager;

        /// <summary>
        /// 事件管理器（快捷访问）
        /// </summary>
        protected Events.EventManager EventManager => World.EventManager;

        /// <summary>
        /// 时间管理器（快捷访问）
        /// </summary>
        protected TimeManager TimeManager => World.TimeManager;

        /// <summary>
        /// 当前逻辑帧
        /// </summary>
        protected int Tick => World.Tick;

        /// <summary>
        /// 逻辑帧间隔时间
        /// </summary>
        protected zfloat DeltaTime => TimeManager.DeltaTime;

        /// <summary>
        /// 设置世界引用（由SystemManager调用）
        /// </summary>
        public void SetWorld(zWorld world)
        {
            World = world;
            OnInitialize();
        }

        /// <summary>
        /// 系统初始化时调用（在SetWorld之后）
        /// 子类可以重写此方法进行初始化工作
        /// </summary>
        protected virtual void OnInitialize()
        {
        }

        /// <summary>
        /// 系统的主更新逻辑
        /// 子类必须实现此方法
        /// </summary>
        public abstract void Update();

        /// <summary>
        /// 系统销毁时调用
        /// 子类可以重写此方法进行清理工作
        /// </summary>
        public virtual void OnDestroy()
        {
        }
        
        /// <summary>
        /// 获取系统执行顺序（默认为普通系统）
        /// </summary>
        /// <returns>执行顺序编号</returns>
        public virtual int GetOrder() => (int)SystemOrder.Normal;
    }
}