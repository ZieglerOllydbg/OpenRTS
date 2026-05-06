using System;
using System.Collections.Generic;
using System.Linq;

namespace ZLockstep.Simulation.ECS
{
    public class ComponentManager
    {
        // 存储每个组件类型对应的组件数组
        // Key: Component Type
        // Value: Dictionary<EntityId, IComponent>
        private readonly Dictionary<Type, Dictionary<int, IComponent>> _componentStores = new Dictionary<Type, Dictionary<int, IComponent>>();
        
        // 引用EntityManager以访问GlobalEntity
        private EntityManager _entityManager;

        /// <summary>
        /// 初始化ComponentManager并设置EntityManager引用
        /// </summary>
        /// <param name="entityManager">实体管理器</param>
        public void Init(EntityManager entityManager)
        {
            _entityManager = entityManager;
        }

        /// <summary>
        /// [新增] 获取所有拥有指定组件类型的实体ID，并按ID排序以保证确定性。
        /// 这是为System遍历专门设计的方法。
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>经过排序的只读实体ID集合</returns>
        public IEnumerable<int> GetAllEntityIdsWith<T>() where T : IComponent
        {
            var componentType = typeof(T);
            if (_componentStores.TryGetValue(componentType, out var store))
            {
                // 排序是保证确定性遍历的关键
                return store.Keys.OrderBy(id => id);
            }
            return Enumerable.Empty<int>();
        }

        public void AddComponent<T>(Entity entity, T component) where T : IComponent
        {
            var componentType = typeof(T);
            if (!_componentStores.ContainsKey(componentType))
            {
                _componentStores[componentType] = new Dictionary<int, IComponent>();
            }

            _componentStores[componentType][entity.Id] = component;
        }
        
        /// <summary>
        /// 为全局实体添加组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="component">组件实例</param>
        public void AddGlobalComponent<T>(T component) where T : IComponent
        {
            AddComponent(_entityManager.GlobalEntity, component);
        }

        public T GetComponent<T>(Entity entity) where T : IComponent
        {
            var componentType = typeof(T);
            if (_componentStores.TryGetValue(componentType, out var store) && store.TryGetValue(entity.Id, out var component))
            {
                return (T)component;
            }

            zUDebug.LogError($"ComponentManager: GetComponent failed. Entity: {entity.Id}, Component: {componentType.Name}");

            return default;
        }
        
        /// <summary>
        /// 获取全局实体的组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>组件实例</returns>
        public T GetGlobalComponent<T>() where T : IComponent
        {
            return GetComponent<T>(_entityManager.GlobalEntity);
        }

        public bool HasComponent<T>(Entity entity) where T : IComponent
        {
            var componentType = typeof(T);
            return _componentStores.ContainsKey(componentType) && _componentStores[componentType].ContainsKey(entity.Id);
        }
        
        /// <summary>
        /// 检查全局实体是否拥有指定组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>是否拥有组件</returns>
        public bool HasGlobalComponent<T>() where T : IComponent
        {
            return HasComponent<T>(_entityManager.GlobalEntity);
        }

        public void RemoveComponent<T>(Entity entity) where T : IComponent
        {
            var componentType = typeof(T);
            if (_componentStores.TryGetValue(componentType, out var store))
            {
                store.Remove(entity.Id);
            }
        }
        
        /// <summary>
        /// 移除全局实体的组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        public void RemoveGlobalComponent<T>() where T : IComponent
        {
            RemoveComponent<T>(_entityManager.GlobalEntity);
        }

        /// <summary>
        /// 获取满足指定条件的组件及其实体
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="condition">筛选条件的函数</param>
        /// <returns>满足条件的(组件, 实体)元组集合</returns>
        public IEnumerable<(T, Entity)> GetComponentsWithCondition<T>(Func<Entity, bool> condition) where T : IComponent
        {
            var entities = GetAllEntityIdsWith<T>();
            
            foreach (var entityId in entities)
            {
                var entity = new Entity(entityId);
                if (condition(entity))
                {
                    var component = GetComponent<T>(entity);
                    yield return (component, entity);
                }
            }
        }

        /// <summary>
        /// 获取满足指定条件的第一个组件及其实体
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="condition">筛选条件的函数</param>
        /// <returns>满足条件的第一个(组件, 实体)元组，如果未找到则返回默认值</returns>
        public (T Component, Entity Entity) GetComponentWithCondition<T>(Func<Entity, bool> condition) where T : IComponent
        {
            var entities = GetAllEntityIdsWith<T>();
            
            foreach (var entityId in entities)
            {
                var entity = new Entity(entityId);
                if (condition(entity))
                {
                    var component = GetComponent<T>(entity);
                    return (component, entity);
                }
            }
            
            return (default(T), new Entity(-1));
        }

        public void EntityDestroyed(Entity entity)
        {
            // 当一个实体被销毁时，移除其所有组件
            foreach (var store in _componentStores.Values)
            {
                store.Remove(entity.Id);
            }
        }
    }
}