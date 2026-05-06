using System.Collections.Generic;

namespace ZLockstep.Simulation.ECS
{
    /// <summary>
    /// 实体管理器
    /// 负责实体的创建、销毁和ID复用。
    /// </summary>
    public class EntityManager
    {
        /// <summary>
        /// 是否复用已销毁实体的 ID；关闭后新实体始终使用递增的新 ID。
        /// </summary>
        private const bool UseRecycledEntityIds = false;

        // 用于存储已销毁并可回收利用的实体ID
        private readonly Queue<int> _recycledEntityIds = new Queue<int>();
        
        // 用于生成新的唯一ID
        private int _nextEntityId;
        private int _activeEntityCount;
        private ComponentManager _componentManager;

        /// <summary>
        /// 全局实体，用于处理全局组件
        /// </summary>
        public Entity GlobalEntity { get; private set; }

        /// <summary>
        /// 当前活跃的实体总数
        /// </summary>
        public int ActiveEntityCount => _activeEntityCount;

        /// <summary>
        /// 创建实体管理器，并初始化实体 ID 生成状态。
        /// </summary>
        public EntityManager()
        {
            _nextEntityId = 0;
            _activeEntityCount = 0;
        }

        /// <summary>
        /// 初始化实体管理器与组件管理器的关联，并创建用于全局组件的特殊实体。
        /// </summary>
        /// <param name="componentManager">负责保存和移除实体组件数据的组件管理器。</param>
        public void Init(ComponentManager componentManager)
        {
            _nextEntityId = 0;
            _activeEntityCount = 0;
            _recycledEntityIds.Clear();
            _componentManager = componentManager;
            
            // 创建全局实体，使用特殊ID -1
            GlobalEntity = new Entity(-1);
            
            // 初始化ComponentManager中的EntityManager引用
            _componentManager.Init(this);
        }

        /// <summary>
        /// 创建一个新的实体
        /// </summary>
        /// <returns>返回创建的实体</returns>
        public Entity CreateEntity()
        {
            int entityId;
            if (UseRecycledEntityIds && _recycledEntityIds.Count > 0)
            {
                // 如果有可回收的ID，则优先使用
                entityId = _recycledEntityIds.Dequeue();
            }
            else
            {
                // 否则，生成一个新的ID
                entityId = _nextEntityId;
                _nextEntityId++;
            }

            _activeEntityCount++;
            return new Entity(entityId);
        }

        /// <summary>
        /// 销毁一个实体，将其ID回收到对象池以便复用
        /// </summary>
        /// <param name="entity">要销毁的实体</param>
        public void DestroyEntity(Entity entity)
        {
            // 全局实体不能被销毁
            if (entity.Id == GlobalEntity.Id)
                return;

            // 在销毁实体前，先移除其所有关联的组件
            _componentManager.EntityDestroyed(entity);

            if (_activeEntityCount > 0)
            {
                _activeEntityCount--;
            }

            if (UseRecycledEntityIds)
            {
                _recycledEntityIds.Enqueue(entity.Id);
            }
        }
    }
}
