using System.Collections.Generic;
using ZLockstep.Simulation.ECS.Components;
using ZLockstep.Simulation.Events;
using ZLockstep.Sync.Command.Commands;
using zUnity;

namespace ZLockstep.Simulation.ECS.Systems
{
    /// <summary>
    /// 结算系统
    /// 负责在战斗开始后延迟检测胜负条件，并在满足条件时发布游戏结束事件。
    /// 当本地玩家阵营没有任何有效存活建筑时判定失败；当所有敌方阵营都没有有效存活建筑时判定胜利。
    /// 结算完成后会发布 <see cref="GameOverEvent"/>，并通过 <see cref="ExitCommand"/> 提交停止标记，避免后续逻辑继续运行。
    /// 建筑是否有效存活由 <see cref="BuildingComponent"/>、<see cref="CampComponent"/>、<see cref="DeathComponent"/> 和 <see cref="HealthComponent"/> 共同判定。
    /// </summary>
    public class SettlementSystem : BaseSystem
    {

        private bool _gameOver = false;
        private bool _canCheckGameOver = false;
        private int _startTick;

        public SettlementSystem()
        {
        
        }

        /// <summary>
        /// 游戏结束事件
        /// </summary>
        public class GameOverEvent : IEvent
        {
            /// <summary>
            /// 是否胜利
            /// true表示本地玩家胜利，false表示失败
            /// </summary>
            public bool IsVictory;

            /// <summary>
            /// 胜利方阵营ID
            /// </summary>
            public int WinningCampId;
        }
        
        public override void Update()
        {
            // 记录起始帧
            if (_startTick == 0)
                _startTick = Tick;
            
            // 10秒后才允许检测游戏结束条件
            if (!_canCheckGameOver)
            {
                // 计算经过的时间（秒）
                zfloat elapsedTime = (Tick - _startTick) * DeltaTime;
                if (elapsedTime >= new zfloat(3)) // 10秒后
                {
                    _canCheckGameOver = true;
                }
            }

            // 如果游戏已经结束，不再检查结束条件
            if (_gameOver)
                return;
                
            // 10秒后开始检查游戏结束条件
            if (_canCheckGameOver)
                CheckGameOverCondition();
        }
        
        /// <summary>
        /// 检查游戏结束条件
        /// </summary>
        private void CheckGameOverCondition()
        {
            if (!ComponentManager.HasGlobalComponent<GlobalInfoComponent>())
            {
                // 可能是单机模式
                return;
            }

            // 获取本地玩家阵营 ID
            int localPlayerCampId = ComponentManager.GetGlobalComponent<GlobalInfoComponent>().LocalPlayerCampId;
            
            // 获取所有参与游戏的阵营
            var campIds = GetAllCampIds();
            
            // 检查本地玩家是否失败（自己的所有建筑都被摧毁）
            bool hasAliveBuilding = HasAnyAliveBuilding(localPlayerCampId);
            if (!hasAliveBuilding)
            {
                // 本地玩家失败
                var gameOverEvent = new GameOverEvent
                {
                    IsVictory = false,
                    WinningCampId = GetFirstAliveCamp(campIds, localPlayerCampId)
                };
                EventManager.Publish(gameOverEvent);
                var exitCommand = new ExitCommand(localPlayerCampId);
                World.CommandManager.SubmitCommand(exitCommand);
                _gameOver = true;
                return;
            }
            
            // 检查本地玩家是否胜利（所有敌方都没有任何建筑存活）
            bool allEnemiesDefeated = true;
            foreach (var campId in campIds)
            {
                if (campId != localPlayerCampId)
                {
                    if (HasAnyAliveBuilding(campId))
                    {
                        allEnemiesDefeated = false;
                        break;
                    }
                }
            }
            
            if (allEnemiesDefeated)
            {
                // 本地玩家胜利
                var gameOverEvent = new GameOverEvent
                {
                    IsVictory = true,
                    WinningCampId = localPlayerCampId
                };
                EventManager.Publish(gameOverEvent);
                var exitCommand = new ExitCommand(localPlayerCampId);
                World.CommandManager.SubmitCommand(exitCommand);
                _gameOver = true;
            }
        }
        
        /// <summary>
        /// 检查指定阵营是否有任何存活的建筑
        /// </summary>
        /// <param name="campId">阵营 ID</param>
        /// <returns>true表示至少有一个建筑存活，false表示所有建筑都被摧毁</returns>
        private bool HasAnyAliveBuilding(int campId)
        {
            // 查找该阵营的所有建筑
            var buildingEntities = ComponentManager.GetAllEntityIdsWith<BuildingComponent>();
            
            foreach (var entityId in buildingEntities)
            {
                var entity = new Entity(entityId);
                
                // 检查是否属于指定阵营
                if (!ComponentManager.HasComponent<CampComponent>(entity))
                    continue;
                    
                var camp = ComponentManager.GetComponent<CampComponent>(entity);
                if (camp.CampId != campId)
                    continue;
                
                // 检查建筑是否被标记为死亡
                if (ComponentManager.HasComponent<DeathComponent>(entity))
                    continue;
                    
                // 检查建筑血量
                if (ComponentManager.HasComponent<HealthComponent>(entity))
                {
                    var health = ComponentManager.GetComponent<HealthComponent>(entity);
                    if (health.CurrentHealth <= zfloat.Zero)
                        continue;
                } else
                {
                    // 没有血量的油田
                    continue;
                }
                
                // 找到一个存活的建筑
                return true;
            }
            
            // 没有任何存活的建筑
            return false;
        }
        
        /// <summary>
        /// 获取第一个存活的阵营ID（除了本地玩家阵营）
        /// </summary>
        /// <param name="campIds">所有阵营ID列表</param>
        /// <param name="localPlayerCampId">本地玩家阵营ID</param>
        /// <returns>第一个存活的阵营ID，如果没有则返回-1</returns>
        private int GetFirstAliveCamp(List<int> campIds, int localPlayerCampId)
        {
            foreach (var campId in campIds)
            {
                if (campId != localPlayerCampId && HasAnyAliveBuilding(campId))
                {
                    return campId;
                }
            }
            return -1;
        }
        
        /// <summary>
        /// 获取所有参与游戏的阵营 ID
        /// </summary>
        /// <returns>阵营 ID 列表</returns>
        private List<int> GetAllCampIds()
        {
            var campIds = new List<int>();
            var economyEntities = ComponentManager.GetAllEntityIdsWith<EconomyComponent>();
            
            foreach (var entityId in economyEntities)
            {
                var entity = new Entity(entityId);
                if (ComponentManager.HasComponent<CampComponent>(entity))
                {
                    var camp = ComponentManager.GetComponent<CampComponent>(entity);
                    campIds.Add(camp.CampId);
                }
            }
            
            return campIds;
        }
        
    }
}
