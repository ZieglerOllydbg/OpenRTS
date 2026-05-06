using ZLockstep.Simulation.ECS.Components;
using zUnity;

namespace ZLockstep.Simulation.ECS.Systems
{
    /// <summary>
    /// 移动系统
    /// 处理所有单位的移动逻辑
    /// 1. 根据移动命令更新速度
    /// 2. 根据速度更新位置
    /// </summary>
    public class MovementSystem : BaseSystem
    {
        public override void Update()
        {
            // 处理所有有移动命令的实体
            ProcessMoveCommands();

            // 应用速度到位置
            ApplyVelocity();
        }

        /// <summary>
        /// 处理移动命令，更新速度和朝向
        /// </summary>
        private void ProcessMoveCommands()
        {
            var commandEntities = ComponentManager.GetAllEntityIdsWith<MoveCommandComponent>();

            foreach (var entityId in commandEntities)
            {
                var entity = new Entity(entityId);

                // 必须有Transform和Unit组件
                if (!ComponentManager.HasComponent<TransformComponent>(entity) ||
                    !ComponentManager.HasComponent<UnitComponent>(entity))
                    continue;

                var moveCmd = ComponentManager.GetComponent<MoveCommandComponent>(entity);
                var transform = ComponentManager.GetComponent<TransformComponent>(entity);
                var unit = ComponentManager.GetComponent<UnitComponent>(entity);

                // 计算到目标的方向和距离
                zVector3 toTarget = moveCmd.TargetPosition - transform.Position;
                zfloat distanceSqr = toTarget.sqrMagnitude;

                // 检查是否到达
                if (distanceSqr <= moveCmd.StopDistance * moveCmd.StopDistance)
                {
                    // 到达目标，移除移动命令和速度
                    ComponentManager.RemoveComponent<MoveCommandComponent>(entity);
                    
                    if (ComponentManager.HasComponent<VelocityComponent>(entity))
                    {
                        ComponentManager.AddComponent(entity, new VelocityComponent(zVector3.zero));
                    }
                    continue;
                }

                // 计算移动方向
                zVector3 direction = toTarget.normalized;

                // 设置期望旋转方向到RotationStateComponent（如果有）
                if (ComponentManager.HasComponent<RotationStateComponent>(entity))
                {
                    var rotationState = ComponentManager.GetComponent<RotationStateComponent>(entity);
                    zVector2 direction2D = new zVector2(direction.x, direction.z);
                    if (direction2D.magnitude > zfloat.Epsilon)
                    {
                        rotationState.DesiredDirection = direction2D.normalized;
                        ComponentManager.AddComponent(entity, rotationState);
                    }

                    // 检查是否正在原地转向
                    if (rotationState.IsInPlaceRotating)
                    {
                        // 原地转向时不移动，速度设为零
                        ComponentManager.AddComponent(entity, new VelocityComponent(zVector3.zero));
                        continue; // 跳过本次移动更新
                    }
                }

                // 更新或添加速度组件
                var velocity = new VelocityComponent(direction * unit.MoveSpeed);
                ComponentManager.AddComponent(entity, velocity);
            }
        }

        /// <summary>
        /// 应用速度到位置
        /// </summary>
        private void ApplyVelocity()
        {
            var velocityEntities = ComponentManager.GetAllEntityIdsWith<VelocityComponent>();

            foreach (var entityId in velocityEntities)
            {
                var entity = new Entity(entityId);

                if (!ComponentManager.HasComponent<TransformComponent>(entity))
                    continue;

                // 检查是否正在原地转向
                if (ComponentManager.HasComponent<RotationStateComponent>(entity))
                {
                    var rotationState = ComponentManager.GetComponent<RotationStateComponent>(entity);
                    if (rotationState.IsInPlaceRotating)
                    {
                        // 原地转向时不更新位置
                        continue;
                    }
                }

                var velocity = ComponentManager.GetComponent<VelocityComponent>(entity);
                var transform = ComponentManager.GetComponent<TransformComponent>(entity);

                // 更新位置
                transform.Position += velocity.Value * DeltaTime;

                // 写回
                ComponentManager.AddComponent(entity, transform);
            }
        }
    }
}

