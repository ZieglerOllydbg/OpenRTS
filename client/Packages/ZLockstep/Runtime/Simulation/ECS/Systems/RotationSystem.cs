using ZLockstep.Simulation.ECS.Components;
using zUnity;

namespace ZLockstep.Simulation.ECS.Systems
{
    /// <summary>
    /// 旋转系统
    /// 处理所有单位的车体旋转和炮塔旋转
    /// 负责原地转向判断和平滑旋转插值
    /// </summary>
    public class RotationSystem : BaseSystem
    {
        protected override void OnInitialize()
        {
            // 系统初始化
        }

        public override void Update()
        {
            // 处理所有有旋转状态组件的实体
            var rotationEntities = ComponentManager.GetAllEntityIdsWith<RotationStateComponent>();

            foreach (var entityId in rotationEntities)
            {
                Entity entity = new Entity(entityId);
                UpdateEntityRotation(entity);
            }
        }

        /// <summary>
        /// 更新单个实体的旋转
        /// </summary>
        private void UpdateEntityRotation(Entity entity)
        {
            // 检查是否有必需的组件
            if (!ComponentManager.HasComponent<TransformComponent>(entity) ||
                !ComponentManager.HasComponent<RotationStateComponent>(entity))
                return;

            var rotationState = ComponentManager.GetComponent<RotationStateComponent>(entity);

            // 如果旋转控制被禁用，跳过
            if (!rotationState.IsEnabled)
                return;

            // 获取Transform
            var transform = ComponentManager.GetComponent<TransformComponent>(entity);

            // 从当前Rotation提取朝向方向
            zVector3 forward = transform.Rotation * zVector3.forward;
            rotationState.CurrentDirection = new zVector2(forward.x, forward.z);
            if (rotationState.CurrentDirection.magnitude > zfloat.Epsilon)
            {
                rotationState.CurrentDirection = rotationState.CurrentDirection.normalized;
            }

            // 如果期望方向为零，跳过旋转
            if (rotationState.DesiredDirection.magnitude < zfloat.Epsilon)
            {
                rotationState.IsInPlaceRotating = false;
                ComponentManager.AddComponent(entity, rotationState);
                return;
            }

            // 计算角度差
            zfloat angleDifference = CalculateAngleDifference(rotationState.CurrentDirection, rotationState.DesiredDirection);

            // 获取载具类型组件（如果有）
            bool hasVehicleType = ComponentManager.HasComponent<VehicleTypeComponent>(entity);
            VehicleTypeComponent vehicleType = hasVehicleType 
                ? ComponentManager.GetComponent<VehicleTypeComponent>(entity) 
                : VehicleTypeComponent.CreateInfantry(); // 默认使用步兵类型

            // 判断是否需要原地转向
            bool needInPlaceRotation = angleDifference > vehicleType.InPlaceRotationThreshold;
            rotationState.IsInPlaceRotating = needInPlaceRotation;

            // 如果角速度为零（建筑），不旋转车体
            if (vehicleType.BodyRotationSpeed <= zfloat.Zero)
            {
                rotationState.IsInPlaceRotating = false;
                ComponentManager.AddComponent(entity, rotationState);
                
                // 但仍然处理炮塔旋转
                if (vehicleType.HasTurret && ComponentManager.HasComponent<TurretComponent>(entity))
                {
                    UpdateTurretRotation(entity, vehicleType, transform);
                }
                return;
            }

            // 计算本帧最大旋转角度
            zfloat maxRotationThisFrame = vehicleType.BodyRotationSpeed * DeltaTime;

            // 执行旋转
            if (angleDifference > zfloat.Epsilon)
            {
                // 计算插值因子 (0到1)
                zfloat t = maxRotationThisFrame / angleDifference;
                if (t > zfloat.One) t = zfloat.One;

                // 使用Slerp进行平滑旋转
                zQuaternion targetRotation = QuaternionFromDirection(rotationState.DesiredDirection);
                transform.Rotation = zQuaternion.Slerp(transform.Rotation, targetRotation, t);

                // 更新Transform
                ComponentManager.AddComponent(entity, transform);
            }
            else
            {
                // 已经对齐，不需要原地转向
                rotationState.IsInPlaceRotating = false;
            }

            // 保存旋转状态
            rotationState.LastRotationAngle = angleDifference;
            ComponentManager.AddComponent(entity, rotationState);

            // 处理炮塔旋转
            if (vehicleType.HasTurret && ComponentManager.HasComponent<TurretComponent>(entity))
            {
                UpdateTurretRotation(entity, vehicleType, transform);
            }
        }

        /// <summary>
        /// 更新炮塔旋转
        /// </summary>
        private void UpdateTurretRotation(Entity entity, VehicleTypeComponent vehicleType, TransformComponent transform)
        {
            var turret = ComponentManager.GetComponent<TurretComponent>(entity);

            // 如果没有目标，炮塔恢复到与车体一致
            if (!turret.HasTarget)
            {
                // 平滑回到0度
                if (turret.CurrentTurretAngle != zfloat.Zero)
                {
                    zfloat maxTurretRotation = vehicleType.TurretRotationSpeed * DeltaTime;
                    zfloat angleToZero = zMathf.Abs(turret.CurrentTurretAngle);
                    
                    if (angleToZero <= maxTurretRotation)
                    {
                        turret.CurrentTurretAngle = zfloat.Zero;
                    }
                    else
                    {
                        zfloat rotationDir = turret.CurrentTurretAngle > zfloat.Zero ? -zfloat.One : zfloat.One;
                        turret.CurrentTurretAngle += rotationDir * maxTurretRotation;
                    }
                }
                
                turret.DesiredTurretAngle = zfloat.Zero;
                ComponentManager.AddComponent(entity, turret);
                return;
            }

            // 计算炮塔期望角度（相对车体）
            // 获取车体朝向
            zVector3 bodyForward = transform.Rotation * zVector3.forward;
            zVector2 bodyDirection2D = new zVector2(bodyForward.x, bodyForward.z);
            if (bodyDirection2D.magnitude > zfloat.Epsilon)
            {
                bodyDirection2D = bodyDirection2D.normalized;
            }

            // 计算炮塔期望朝向相对车体的角度
            if (turret.DesiredTurretDirection.magnitude > zfloat.Epsilon)
            {
                zVector2 desiredDir = turret.DesiredTurretDirection.normalized;
                
                // 计算相对角度
                turret.DesiredTurretAngle = CalculateRelativeAngle(bodyDirection2D, desiredDir);

                // 如果有旋转限制，限制角度
                if (turret.HasRotationLimit)
                {
                    if (turret.DesiredTurretAngle < turret.MinTurretAngle)
                        turret.DesiredTurretAngle = turret.MinTurretAngle;
                    if (turret.DesiredTurretAngle > turret.MaxTurretAngle)
                        turret.DesiredTurretAngle = turret.MaxTurretAngle;
                }
            }

            // 平滑旋转到期望角度
            zfloat angleDiff = turret.DesiredTurretAngle - turret.CurrentTurretAngle;
            
            // 处理角度跨越±180度的情况
            if (angleDiff > new zfloat(180))
                angleDiff -= new zfloat(360);
            if (angleDiff < new zfloat(-180))
                angleDiff += new zfloat(360);

            zfloat maxRotation = vehicleType.TurretRotationSpeed * DeltaTime;
            
            if (zMathf.Abs(angleDiff) <= maxRotation)
            {
                turret.CurrentTurretAngle = turret.DesiredTurretAngle;
            }
            else
            {
                zfloat rotationDir = angleDiff > zfloat.Zero ? zfloat.One : -zfloat.One;
                turret.CurrentTurretAngle += rotationDir * maxRotation;
            }

            // 保持炮塔角度在 [-180, 180] 范围内
            NormalizeAngle(ref turret.CurrentTurretAngle);

            ComponentManager.AddComponent(entity, turret);
        }

        /// <summary>
        /// 计算两个方向之间的角度差（度数，绝对值）
        /// </summary>
        private zfloat CalculateAngleDifference(zVector2 from, zVector2 to)
        {
            // 确保向量归一化
            if (from.magnitude < zfloat.Epsilon || to.magnitude < zfloat.Epsilon)
                return zfloat.Zero;

            from = from.normalized;
            to = to.normalized;

            // 使用点积计算夹角
            zfloat dot = zVector2.Dot(from, to);
            
            // 限制dot在[-1, 1]范围内，避免浮点误差
            if (dot > zfloat.One) dot = zfloat.One;
            if (dot < -zfloat.One) dot = -zfloat.One;

            // 计算角度（弧度转度数）
            zfloat angle = zMathf.Acos(dot) * zMathf.Rad2Deg;

            return angle;
        }

        /// <summary>
        /// 计算相对角度（from到to的有符号角度，度数）
        /// 正值表示顺时针，负值表示逆时针
        /// </summary>
        private zfloat CalculateRelativeAngle(zVector2 from, zVector2 to)
        {
            if (from.magnitude < zfloat.Epsilon || to.magnitude < zfloat.Epsilon)
                return zfloat.Zero;

            from = from.normalized;
            to = to.normalized;

            // 使用atan2计算角度
            zfloat fromAngle = zMathf.Atan2(from.y, from.x) * zMathf.Rad2Deg;
            zfloat toAngle = zMathf.Atan2(to.y, to.x) * zMathf.Rad2Deg;

            zfloat angle = toAngle - fromAngle;

            // 归一化到 [-180, 180]
            NormalizeAngle(ref angle);

            return angle;
        }

        /// <summary>
        /// 将角度归一化到 [-180, 180] 范围
        /// </summary>
        private void NormalizeAngle(ref zfloat angle)
        {
            while (angle > new zfloat(180))
                angle -= new zfloat(360);
            while (angle < new zfloat(-180))
                angle += new zfloat(360);
        }

        /// <summary>
        /// 从2D方向创建四元数
        /// </summary>
        private zQuaternion QuaternionFromDirection(zVector2 direction)
        {
            if (direction.magnitude < zfloat.Epsilon)
                return zQuaternion.identity;

            zVector2 normalized = direction.normalized;
            zVector3 forward3D = new zVector3(normalized.x, zfloat.Zero, normalized.y);
            
            return zQuaternion.LookRotation(forward3D);
        }
    }
}

