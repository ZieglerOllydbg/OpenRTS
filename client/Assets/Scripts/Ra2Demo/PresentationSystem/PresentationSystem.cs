using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using ZFrame;
using ZLockstep.Simulation.ECS;
using ZLockstep.Simulation.ECS.Components;
using ZLockstep.Simulation.Events;
using ZLockstep.Sync;
using ZLockstep.View.Components;
using static ZLockstep.Simulation.ECS.Systems.SettlementSystem;

namespace ZLockstep.View.Systems
{
    /// <summary>
    /// 表现层系�?
    /// 负责处理所有与Unity显示相关的逻辑
    /// 
    /// 职责�?
    /// 1. 监听逻辑层事件并创建/销毁视�?
    /// 2. 同步逻辑位置到Unity Transform
    /// 3. 处理动画播放
    /// 4. 管理UI显示
    /// 5. 管理单位描边效果
    /// </summary>
    public class PresentationSystem : PresentationBaseSystem
    {
        // Unity资源
        private const float ForwardReturnDelaySeconds = 2f;
        private const float ForwardReturnSpeedDegreesPerSecond = 120f;
        private Transform _viewRoot;

        /// <summary>
        /// 是否启用平滑插值（在FixedUpdate之间�?
        /// </summary>
        public bool EnableSmoothInterpolation { get; set; } = true;

        /// <summary>
        /// 是否启用表现系统（追帧时可以禁用以提升性能�?
        /// </summary>
        public bool Enabled { get; set; } = true;

        // 添加对Game对象的引�?
        private ZLockstep.Sync.Game _game;

        /// <summary>
        /// 初始化系统（由GameWorldBridge调用�?
        /// </summary>
        public void Initialize(Transform viewRoot)
        {
            _viewRoot = viewRoot;
        }

        /// <summary>
        /// 销毁所有已创建的视图对�?
        /// </summary>
        public void DestroyAllViews()
        {
            if (_viewRoot == null)
                return;

            // 销毁所有子对象（游戏对象）
            for (int i = _viewRoot.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(_viewRoot.GetChild(i).gameObject);
            }
            
            // 清理死亡实体列表
            _dyingEntities.Clear();
        }

        /// <summary>
        /// 设置Game对象引用（由GameWorldBridge调用�?
        /// </summary>
        public void SetGame(ZLockstep.Sync.Game game)
        {
            _game = game;
        }

        public override void Update()
        {
            // 1. 处理单位创建事件
            ProcessCreationEvents();

            // 2. 检查死亡动画状�?
            CheckDyingEntities();

            // 3. 处理死亡事件（ProcessEntityDiedEvents播放死亡动画�?
            ProcessEntityDiedEvents();

            // 4. 处理经济变化事件
            ProcessEconomyEvents();

            // 5. 处理游戏结束事件
            ProcessGameOverEvents();

            // 6. 同步已有的View
            SyncAllViews();

            // 7. 消息提示
            ProcessShowMessage();
        }

        private void ProcessShowMessage()
        {
            var events = EventManager.GetEvents<MessageEvent>();
            foreach (var evt in events)
            {
                // 显示消息提示
                zUDebug.Log($"[PresentationSystem] 显示消息：{evt.Message}");
                Frame.DispatchEvent(new ShowMessageEvent(evt.Message));
            }
        }


        /// <summary>
        /// 处理经济变化事件（金币和能量变化�?
        /// </summary>
        private void ProcessEconomyEvents()
        {
            // 处理金币变化事件
            var moneyEvents = EventManager.GetEvents<MoneyChangedEvent>();
            foreach (var evt in moneyEvents)
            {
                // 这里可以添加处理金币变化的逻辑
                // 例如：更新UI上的金币显示
                // Debug.Log($"[PresentationSystem] 阵营{evt.CampId}金币变化: {evt.OldMoney} -> {evt.NewMoney} ({evt.Reason})");
                Frame.DispatchEvent(new EconomyEvent());
            }
        }

        /// <summary>
        /// 处理实体死亡事件
        /// </summary>
        private void ProcessEntityDiedEvents()
        {
            var events = EventManager.GetEvents<EntityDiedEvent>();
            foreach (var evt in events)
            {
                var entity = new Entity(evt.EntityId);

                // 检查是否有ViewComponent
                if (!ComponentManager.HasComponent<ViewComponent>(entity))
                    continue;

                var viewComponent = ComponentManager.GetComponent<ViewComponent>(entity);
                if (evt.UnitType == UnitType.Projectile)
                {
                    CleanupProjectileView(entity, viewComponent);
                    PlayProjectileDeathEffectAsync(evt.Position, evt.ConfProjectileID).Forget();
                    continue;
                }

                // 触发死亡动画
                if (viewComponent.Animator != null)
                {
                    TrySetTrigger(viewComponent.Animator, "Death");
                }
                else
                {
                }
                
                // 添加到死亡列表，用于后续跟踪动画状�?
                _dyingEntities.Add((evt.EntityId, viewComponent.Animator));
            }
        }

        /// <summary>
        /// 检查正在播放死亡动画的实体
        /// 当动画播放完毕后，移除对应的视图组件
        /// </summary>
        private void CheckDyingEntities()
        {
            for (int i = _dyingEntities.Count - 1; i >= 0; i--)
            {
                var (entityId, animator) = _dyingEntities[i];
                Entity entity = new Entity(entityId);

                // 检查动画是否播放完�?
                if (IsDeathAnimationFinished(animator))
                {
                    // 动画播放完毕，移除ViewComponent
                    if (ComponentManager.HasComponent<ViewComponent>(entity))
                    {
                        var viewComponent = ComponentManager.GetComponent<ViewComponent>(entity);

                        // 销毁GameObject
                        if (viewComponent.GameObject != null)
                        {
                            AssetManager.ReleaseInstance(viewComponent.GameObject);
                        }

                        // 移除ViewComponent
                        ComponentManager.RemoveComponent<ViewComponent>(entity);
                        // Debug.Log($"[PresentationSystem] 实体{entityId}死亡动画播放完毕，已移除视图");

                        // 移除血�?
                        Frame.DispatchEvent(new HealthEvent(entityId, false, false));
                    }

                    // 从死亡列表中移除
                    _dyingEntities.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 判断死亡动画是否播放完毕
        /// </summary>
        private bool IsDeathAnimationFinished(Animator animator)
        {
            // 检查Animator是否仍然有效
            if (animator == null || !animator.gameObject.activeInHierarchy)
                return true;

            // 获取当前动画状�?
            AnimatorStateInfo nextStateInfo = animator.GetNextAnimatorStateInfo(0);
            if (nextStateInfo.IsName("Death"))
            {
                return false;
            }

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            // 检查是否是死亡动画状�?
            // 不同单位可能有不同的状态命名：Death、Cartridge_Bomb�?
            bool isDeathState = stateInfo.IsName("Death");
            if (isDeathState)
            {
                // normalizedTime大于等于1.0表示动画已经播放完成
                return stateInfo.normalizedTime >= 1.0f;
            }

            // 如果不是已知的死亡状态，可能动画已经切换，也可以认为完成
            return true;
        }

        /// <summary>
        /// 处理游戏结束事件
        /// </summary>
        private void ProcessGameOverEvents()
        {
            var events = EventManager.GetEvents<GameOverEvent>();
            foreach (var evt in events)
            {
                // 触发结算事件
                Frame.DispatchEvent(new SettleEvent(evt.IsVictory));
            }
        }

        /// <summary>
        /// 处理单位创建事件
        /// </summary>
        private void ProcessCreationEvents()
        {
            var events = EventManager.GetEvents<UnitCreatedEvent>();
            foreach (var evt in events)
            {
                CreateViewForEntityAsync(evt);
            }
        }

        /// <summary>
        /// 为实体创建Unity视图
        /// </summary>
        private async void CreateViewForEntityAsync(UnitCreatedEvent evt)
        {
            GameObject viewObject = null;
            String viewName = "";

            if (evt.ConfBuildingID > 0)
            {
                ConfBuilding confBuilding = ConfigManager.Get<ConfBuilding>(evt.ConfBuildingID);
                if (confBuilding != null)
                {
                    // 创建建筑模型
                    string buildingPrefabPath = confBuilding.BuildPrefab;
                    viewObject = await AssetManager.InstantiatePrefabAsync(buildingPrefabPath, _viewRoot);

                    viewName = "[" + evt.PlayerId + "]Building_" + confBuilding.Type + "_" + evt.EntityId;
                }
            }

            if (evt.ConfUnitID > 0)
            {
                ConfUnit confUnit = ConfigManager.Get<ConfUnit>(evt.ConfUnitID);
                if (confUnit != null)
                {
                    string unitPrefabPath = confUnit.Prefab;
                    viewObject = await AssetManager.InstantiatePrefabAsync(unitPrefabPath, _viewRoot);

                    viewName = "[" + evt.PlayerId + "]Unit_" + confUnit.Type + "_" + evt.EntityId;

                    // 添加选择轮廓组件
                    var outlineComponent = viewObject.AddComponent<OutlineComponent>();
                    outlineComponent.SetRingSizeMultiplier(confUnit.OutlineScale / 10000f); // 设置轮廓大小倍数（可根据需要调整）
                }
            }

            if (evt.ConfProjectileID > 0)
            {
                ConfProjectile confProjectile = ConfigManager.Get<ConfProjectile>(evt.ConfProjectileID);
                if (confProjectile != null)
                {
                    // 创建弹道模型
                    string projectilePrefabPath = confProjectile.Prefab;
                    viewObject = await AssetManager.InstantiatePrefabAsync(projectilePrefabPath, _viewRoot);

                    viewName = "[" + evt.PlayerId + "]Projectile_" + confProjectile.Type + "_" + evt.EntityId;
                }
            }

            if (viewObject == null)
                return;

            if (!IsCreatedViewStillValid(evt))
            {
                AssetManager.ReleaseInstance(viewObject);
                return;
            }

            if (evt.ConfProjectileID > 0)
            {
                AudioSource audioSource = viewObject.GetComponent<AudioSource>();
                if (audioSource != null)
                {
                    ConfProjectile confProjectile = ConfigManager.Get<ConfProjectile>(evt.ConfProjectileID);
                    if (confProjectile != null)
                    {
                        AudioClip clip = await AssetManager.GetAudioClipAsync("Audio/" + confProjectile.AudioClip);
                        audioSource.clip = clip;
                        audioSource.Play();
                    }
                }
            }

            if (!IsCreatedViewStillValid(evt))
            {
                AssetManager.ReleaseInstance(viewObject);
                return;
            }

            viewObject.name = viewName;
            viewObject.transform.position = evt.Position.ToVector3();
            viewObject.transform.rotation = evt.Rotation.ToQuaternion();

            // 创建并添加ViewComponent
            var entityForView = new Entity(evt.EntityId);
            var viewComponent = ViewComponent.Create(viewObject, EnableSmoothInterpolation);
            ComponentManager.AddComponent(entityForView, viewComponent);

            // 添加玩家单位指示器组件（弹道不需要）
            if (evt.UnitType != 100)
            {
                var indicator = viewObject.AddComponent<PlayerUnitIndicator>();
                indicator.Initialize(evt.PlayerId);
            }

            if (evt.ConfBuildingID > 0)
            {
                ConfBuilding confBuilding = ConfigManager.Get<ConfBuilding>(evt.ConfBuildingID);
                if (confBuilding != null && confBuilding.Hp > 0)
                {
                    Frame.DispatchEvent(new HealthEvent(evt.EntityId, evt.PlayerId == 1, true));
                }
            }
            else if (evt.ConfUnitID > 0)
            {
                Frame.DispatchEvent(new HealthEvent(evt.EntityId, evt.PlayerId == 1, true));
            }
        }

        /// <summary>
        /// 校验异步加载完成后的视图是否仍然对应有效逻辑实体，避免追帧期间已销毁的子弹延迟创建出残留视图。
        /// </summary>
        /// <param name="evt">触发视图创建的实体创建事件。</param>
        /// <returns>视图仍可绑定到实体时返回 true；实体已销毁或子弹配置不匹配时返回 false。</returns>
        private bool IsCreatedViewStillValid(UnitCreatedEvent evt)
        {
            Entity entity = new Entity(evt.EntityId);
            if (!ComponentManager.HasComponent<TransformComponent>(entity))
            {
                return false;
            }

            if (ComponentManager.HasComponent<ViewComponent>(entity))
            {
                return false;
            }

            if (evt.ConfProjectileID <= 0)
            {
                return true;
            }

            if (!ComponentManager.HasComponent<ProjectileComponent>(entity))
            {
                return false;
            }

            ProjectileComponent projectile = ComponentManager.GetComponent<ProjectileComponent>(entity);
            return projectile.ConfProjectileId == evt.ConfProjectileID;
        }

        /// <summary>
        /// 创建默认的弹道可视化
        /// </summary>
        private GameObject CreateDefaultProjectileView(UnitCreatedEvent evt)
        {
            // 创建弹道根节�?
            GameObject projectile = new GameObject($"Projectile_{evt.EntityId}");
            projectile.transform.SetParent(_viewRoot);
            projectile.transform.position = evt.Position.ToVector3();

            // 添加球体作为弹头
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "ProjectileHead";
            sphere.transform.SetParent(projectile.transform);
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localScale = Vector3.one * 0.3f; // 0.3米直�?

            // 设置颜色（根据阵营）
            var renderer = sphere.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = evt.PlayerId == 0 ? new Color(0.2f, 0.5f, 1f) : new Color(1f, 0.3f, 0.3f); // �?�?
                material.SetFloat("_Metallic", 0.5f);
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", material.color * 2f);
                renderer.material = material;
            }

            // 添加拖尾效果
            var trail = projectile.AddComponent<TrailRenderer>();
            trail.time = 0.5f; // 拖尾持续0.5�?
            trail.startWidth = 0.2f;
            trail.endWidth = 0.05f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.startColor = evt.PlayerId == 0 ? new Color(0.2f, 0.5f, 1f, 1f) : new Color(1f, 0.3f, 0.3f, 1f);
            trail.endColor = new Color(1f, 1f, 1f, 0f);

            // Debug.Log($"[PresentationSystem] 创建了默认弹道可视化: Entity_{evt.EntityId}");

            return projectile;
        }

        

        /// <summary>
        /// 同步所有View实体
        /// </summary>
        private void SyncAllViews()
        {
            var viewEntities = ComponentManager.GetAllEntityIdsWith<ViewComponent>();

            foreach (var entityId in viewEntities)
            {
                var entity = new Entity(entityId);

                // 只同步有Transform组件的实�?
                if (!ComponentManager.HasComponent<TransformComponent>(entity))
                    continue;

                SyncEntityToViewAsync(entity);
            }
        }

        /// <summary>
        /// 重新同步所有实体（追帧完成后调用）
        /// 用于处理追帧期间禁用渲染导致的视图缺�?
        /// </summary>
        public void ResyncAllEntities()
        {
            if (_viewRoot == null)
            {
                Debug.LogWarning("[PresentationSystem] ResyncAllEntities: 未初始化");
                return;
            }

            int syncedCount = 0;
            CleanupOrphanViews();

            // 获取所有逻辑实体
            var allEntities = ComponentManager.GetAllEntityIdsWith<TransformComponent>();

            foreach (var entityId in allEntities)
            {
                var entity = new Entity(entityId);

                if (ComponentManager.HasComponent<ViewComponent>(entity))
                {
                    // 已有视图，强制同步
                    var view = ComponentManager.GetComponent<ViewComponent>(entity);
                    var transform = ComponentManager.GetComponent<TransformComponent>(entity);

                    if (view.Transform != null)
                    {
                        view.Transform.position = transform.Position.ToVector3();
                        view.Transform.rotation = transform.Rotation.ToQuaternion();
                        view.Transform.localScale = transform.Scale.ToVector3();
                        SyncAttackForward(entity, view);
                        syncedCount++;
                    }
                }
                else
                {
                    zUDebug.LogWarning($"[PresentationSystem] ResyncAllEntities: Entity_{entity.Id} 缺少 ViewComponent");
                }
            }
        }

        /// <summary>
        /// 清理已经失去逻辑 TransformComponent 的孤儿视图，防止追帧期间漏处理死亡事件的子弹残留在场景中。
        /// </summary>
        private void CleanupOrphanViews()
        {
            var viewEntities = new List<int>(ComponentManager.GetAllEntityIdsWith<ViewComponent>());
            foreach (int entityId in viewEntities)
            {
                Entity entity = new Entity(entityId);
                if (ComponentManager.HasComponent<TransformComponent>(entity))
                {
                    continue;
                }

                ViewComponent viewComponent = ComponentManager.GetComponent<ViewComponent>(entity);
                if (viewComponent.GameObject != null)
                {
                    AssetManager.ReleaseInstance(viewComponent.GameObject);
                }

                ComponentManager.RemoveComponent<ViewComponent>(entity);
            }
        }

        /// <summary>
        /// 将逻辑实体的数据同步到Unity GameObject
        /// </summary>
        private async void SyncEntityToViewAsync(Entity entity)
        {
            var view = ComponentManager.GetComponent<ViewComponent>(entity);
            var transformComponent = ComponentManager.GetComponent<TransformComponent>(entity);

            if (view.Transform == null)
                return;

            // 检查是否是正在建造的建筑
            if (!ComponentManager.HasComponent<BuildingConstructionComponent>(entity))
            {
                if (ComponentManager.HasComponent<BuildingComponent>(entity))
                {
                    var buildingComponent = ComponentManager.GetComponent<BuildingComponent>(entity);
                    if (!view.BuildingOK)
                    {
                        ConfBuilding confBuilding = ConfigManager.Get<ConfBuilding>(buildingComponent.BuildingType);
                        if (confBuilding == null)
                        {
                            Debug.LogError($"[PresentationSystem] 找不到建筑配置：BuildingType={buildingComponent.BuildingType}");
                            return;
                        }

                        // 移除旧建筑模型（保存位置信息）
                        Vector3 oldPosition = Vector3.zero;
                        Quaternion oldRotation = Quaternion.identity;

                        String gameObjectName = view.GameObject.name;
                        
                        if (view.GameObject != null)
                        {
                            // 保存旧模型的位置和旋转信息
                            oldPosition = view.Transform.position;
                            oldRotation = view.Transform.rotation;
                            
                            // 回收到对象池
                            AssetManager.ReleaseInstance(view.GameObject);
                            view.GameObject = null;
                        }

                        // 创建建筑模型
                        string prefabPath = confBuilding.Prefab;
                        view.GameObject = await AssetManager.InstantiatePrefabAsync(prefabPath, _viewRoot);
                        view.GameObject.name = gameObjectName;

                        // 应用旧模型的位置
                        view.GameObject.transform.position = oldPosition;
                        view.GameObject.transform.rotation = oldRotation;
                        
                        // 更新 Transform 缓存
                        view.RefreshTransformCache();

                        int campId = ComponentManager.GetComponent<CampComponent>(entity).CampId;

                        var indicator = view.GameObject.AddComponent<PlayerUnitIndicator>();
                        indicator.Initialize(campId);

                        view.BuildingOK = true;
                        ComponentManager.AddComponent(entity, view);
                    }
                }
                
            }

            // 如果启用插值，记录上一帧的逻辑位置（用于下一帧插值）
            if (view.EnableInterpolation)
            {
                view.LastLogicPosition = view.Transform.position;
                view.LastLogicRotation = view.Transform.rotation;
            }

            // 直接同步（无插值）
            if (!EnableSmoothInterpolation)
            {
                // 获取当前逻辑位置和旋�?
                Vector3 currentLogicPos = transformComponent.Position.ToVector3();
                Quaternion currentLogicRot = transformComponent.Rotation.ToQuaternion();
                view.Transform.SetPositionAndRotation(currentLogicPos, currentLogicRot);
            }

            // 同步缩放
            view.Transform.localScale = transformComponent.Scale.ToVector3();
            SyncAttackForward(entity, view);

            // 写回 ViewComponent（如果有修改�?
            ComponentManager.AddComponent(entity, view);
        }

        /// <summary>
        /// Unity �?Update 中调用，用于平滑插值（可选）
        /// <param name="deltaTime">时间间隔</param>
        /// </summary>
        public void LerpUpdate(float deltaTime)
        {
            if (!Enabled || !EnableSmoothInterpolation)
                return;

            var viewEntities = ComponentManager.GetAllEntityIdsWith<ViewComponent>();

            foreach (var entityId in viewEntities)
            {
                var entity = new Entity(entityId);

                if (!ComponentManager.HasComponent<TransformComponent>(entity))
                    continue;

                var view = ComponentManager.GetComponent<ViewComponent>(entity);
                var transformComponent = ComponentManager.GetComponent<TransformComponent>(entity);

                if (view.GameObject == null || view.Transform == null || !view.EnableInterpolation)
                    continue;

                // 插值到目标位置：使�?LastLogicPosition 作为起点，而不是当前视觉位�?
                Vector3 targetPos = transformComponent.Position.ToVector3();
                Quaternion targetRot = transformComponent.Rotation.ToQuaternion();

                // 检测目标位置是否变化，如果变化则重置插值时�?
                bool targetChanged = targetPos != view.LastLogicPosition;
                
                if (targetChanged)
                {
                    zUDebug.Log($"[PresentationSystem] LerpUpdate: entityId={entity.Id}, targetPos:{targetPos}");
                    view.CurrentInterpolationTime = 0f;
                }

                // 累加插值时�?
                view.CurrentInterpolationTime += deltaTime;

                // 计算插值因子（0 �?1 之间），确保不超�?1
                float t = Mathf.Clamp01(view.CurrentInterpolationTime / view.InterpolationDuration);

                // 正确的插值逻辑：从上一帧的逻辑位置插值到当前帧的逻辑位置
                // 使用固定时间控制，最�?0.25 秒完成插�?
                if (view.LastLogicPosition != targetPos)
                {
                    view.Transform.position = Vector3.Lerp(
                        view.LastLogicPosition,
                        targetPos,
                        t
                    );
                }


                if (view.LastLogicRotation != targetRot)
                {
                    view.Transform.rotation = Quaternion.Lerp(
                        view.LastLogicRotation,
                        targetRot,
                        t
                    );
                }

            }
        }

        private void SyncAttackForward(Entity entity, ViewComponent view)
        {
            if (view.ForwardTransform == null || !ComponentManager.HasComponent<AttackForwardComponent>(entity))
                return;

            var attackForward = ComponentManager.GetComponent<AttackForwardComponent>(entity);
            if (!attackForward.HasDirection)
                return;

            if (attackForward.LastAttackTick != view.LastAppliedAttackTick)
            {
                Vector3 attackDirection = new Vector3((float)attackForward.Direction.x, 0f, (float)attackForward.Direction.y);
                if (attackDirection.sqrMagnitude <= 0.0001f)
                    return;

                view.ForwardTransform.rotation = Quaternion.LookRotation(attackDirection.normalized, Vector3.up);
                view.LastAppliedAttackTick = attackForward.LastAttackTick;
                view.LastAttackTime = Time.time;
                return;
            }

            if (Time.time - view.LastAttackTime < ForwardReturnDelaySeconds)
                return;

            Vector3 bodyForward = view.Transform.forward;
            bodyForward.y = 0f;
            if (bodyForward.sqrMagnitude <= 0.0001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(bodyForward.normalized, Vector3.up);
            float maxDegreesDelta = ForwardReturnSpeedDegreesPerSecond * Time.deltaTime;
            view.ForwardTransform.rotation = Quaternion.RotateTowards(view.ForwardTransform.rotation, targetRotation, maxDegreesDelta);
        }

        /// <summary>
        /// 同步动画状�?
        /// </summary>
        private void SyncAnimationState(Entity entity, ViewComponent view)
        {
            if (view.Animator == null)
                return;

            // 根据速度设置移动动画
            if (ComponentManager.HasComponent<VelocityComponent>(entity))
            {
                var velocity = ComponentManager.GetComponent<VelocityComponent>(entity);
                bool isMoving = velocity.IsMoving;
                float speed = velocity.SqrMagnitude.ToFloat();

                // view.Animator.SetBool("Move", isMoving);
                TrySetInteger(view.Animator, "speed", (int)speed);
            }

            // 根据生命值设置死亡动�?
            if (ComponentManager.HasComponent<HealthComponent>(entity))
            {
                var health = ComponentManager.GetComponent<HealthComponent>(entity);
                if (health.IsDead)
                {
                    TrySetTrigger(view.Animator, "Death");
                }
            }

            // 根据攻击组件设置攻击动画
            if (ComponentManager.HasComponent<AttackComponent>(entity))
            {
                var attack = ComponentManager.GetComponent<AttackComponent>(entity);
                if (attack.HasTarget)
                {
                    TrySetTrigger(view.Animator, "Fire");
                }
                else
                {
                    // view.Animator.SetBool("Fire", false);
                }
            }
        }

        /// <summary>
        /// 立即回收子弹视图，不进入普通死亡动画流程。
        /// </summary>
        private void CleanupProjectileView(Entity entity, ViewComponent viewComponent)
        {
            if (viewComponent.GameObject != null)
            {
                AssetManager.ReleaseInstance(viewComponent.GameObject);
            }

            ComponentManager.RemoveComponent<ViewComponent>(entity);
        }

        /// <summary>
        /// 在命中位置创建并播放子弹死亡特效。
        /// </summary>
        private async UniTaskVoid PlayProjectileDeathEffectAsync(zUnity.zVector3 position, int confProjectileId)
        {
            const string defaultEffectPrefab = "EffectExplosionA";
            string effectPrefab = defaultEffectPrefab;
            int effectScale = 10000;

            if (confProjectileId > 0)
            {
                ConfProjectile confProjectile = ConfigManager.Get<ConfProjectile>(confProjectileId);
                if (confProjectile != null)
                {
                    if (!string.IsNullOrWhiteSpace(confProjectile.DeathEffectPrefab))
                    {
                        effectPrefab = confProjectile.DeathEffectPrefab;
                    }

                    if (confProjectile.DeathEffectScale > 0)
                    {
                        effectScale = confProjectile.DeathEffectScale;
                    }
                }
            }

            GameObject effect = await AssetManager.InstantiatePrefabAsync(effectPrefab, _viewRoot);
            if (effect == null)
                return;

            effect.transform.position = position.ToVector3();
            effect.transform.rotation = Quaternion.identity;
            effect.transform.localScale = Vector3.one * (effectScale / 10000f);

            float lifetime = PlayEffectAndGetLifetime(effect);
            AssetManager.ReleaseInstance(effect, lifetime);
        }

        /// <summary>
        /// 播放特效中的所有粒子系统，并返回需要的回收延迟。
        /// </summary>
        private float PlayEffectAndGetLifetime(GameObject effect)
        {
            float maxLifetime = 0f;
            var particleSystems = effect.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var particleSystem in particleSystems)
            {
                var main = particleSystem.main;
                float particleLifetime = main.duration + main.startDelay.constantMax + main.startLifetime.constantMax;
                if (particleLifetime > maxLifetime)
                {
                    maxLifetime = particleLifetime;
                }

                particleSystem.Play(true);
            }

            return Mathf.Max(maxLifetime, 0.1f);
        }

        private static bool HasAnimatorParameter(Animator animator, string parameterName, AnimatorControllerParameterType parameterType)
        {
            if (animator == null || string.IsNullOrEmpty(parameterName))
                return false;

            foreach (var parameter in animator.parameters)
            {
                if (parameter.type == parameterType && parameter.name == parameterName)
                    return true;
            }

            return false;
        }

        private static bool TrySetTrigger(Animator animator, string triggerName)
        {
            if (!HasAnimatorParameter(animator, triggerName, AnimatorControllerParameterType.Trigger))
                return false;

            animator.SetTrigger(triggerName);
            return true;
        }

        private static bool TrySetInteger(Animator animator, string parameterName, int value)
        {
            if (!HasAnimatorParameter(animator, parameterName, AnimatorControllerParameterType.Int))
                return false;

            animator.SetInteger(parameterName, value);
            return true;
        }



        /// <summary>
        /// 正在播放死亡动画的实体列�?
        /// 用于跟踪动画播放状态，在动画播放完毕后移除视图组件
        /// </summary>
        private List<(int entityId, Animator animator)> _dyingEntities = new List<(int, Animator)>();
    }
}

