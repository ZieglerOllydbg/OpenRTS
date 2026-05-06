using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZLockstep.Simulation.ECS
{
    /// <summary>
    /// 全局实体使用示例
    /// 演示如何使用全局实体存储和访问全局组件
    /// </summary>
    
    // 示例全局组件
    public struct GameTimeComponent : IComponent
    {
        public int TotalFrames;
        public float GameTime;
    }
    
    public struct GameStateComponent : IComponent
    {
        public enum State
        {
            Menu,
            Playing,
            Paused,
            GameOver
        }
        
        public State CurrentState;
        public int CurrentLevel;
    }
    
    // 示例系统，展示如何使用全局实体
    public class GlobalStateSystem
    {
        private ComponentManager _componentManager;
        private EntityManager _entityManager;
        
        public GlobalStateSystem(ComponentManager componentManager, EntityManager entityManager)
        {
            _componentManager = componentManager;
            _entityManager = entityManager;
        }
        
        public void Initialize()
        {
            // 方式1: 通过全局实体添加组件（原始方式）
            _componentManager.AddComponent(_entityManager.GlobalEntity, new GameTimeComponent 
            { 
                TotalFrames = 0, 
                GameTime = 0f 
            });
            
            // 方式2: 直接添加全局组件（新方式）
            _componentManager.AddGlobalComponent(new GameStateComponent 
            { 
                CurrentState = GameStateComponent.State.Menu, 
                CurrentLevel = 1 
            });
        }
        
        public void Update(float deltaTime)
        {
            // 方式1: 通过全局实体获取组件（原始方式）
            if (_componentManager.HasComponent<GameTimeComponent>(_entityManager.GlobalEntity))
            {
                var gameTime = _componentManager.GetComponent<GameTimeComponent>(_entityManager.GlobalEntity);
                gameTime.TotalFrames++;
                gameTime.GameTime += deltaTime;
                _componentManager.AddComponent(_entityManager.GlobalEntity, gameTime);
            }
            
            // 方式2: 直接获取全局组件（新方式）
            if (_componentManager.HasGlobalComponent<GameStateComponent>())
            {
                var gameState = _componentManager.GetGlobalComponent<GameStateComponent>();
                // 在这里可以更新游戏状态
            }
        }
        
        // 使用原始方式获取游戏状态
        public GameStateComponent.State GetGameStateWithEntity()
        {
            if (_componentManager.HasComponent<GameStateComponent>(_entityManager.GlobalEntity))
            {
                var gameState = _componentManager.GetComponent<GameStateComponent>(_entityManager.GlobalEntity);
                return gameState.CurrentState;
            }
            
            return GameStateComponent.State.Menu;
        }
        
        // 使用新方式获取游戏状态
        public GameStateComponent.State GetGameState()
        {
            if (_componentManager.HasGlobalComponent<GameStateComponent>())
            {
                var gameState = _componentManager.GetGlobalComponent<GameStateComponent>();
                return gameState.CurrentState;
            }
            
            return GameStateComponent.State.Menu;
        }
        
        // 使用原始方式设置游戏状态
        public void SetGameStateWithEntity(GameStateComponent.State state)
        {
            if (_componentManager.HasComponent<GameStateComponent>(_entityManager.GlobalEntity))
            {
                var gameState = _componentManager.GetComponent<GameStateComponent>(_entityManager.GlobalEntity);
                gameState.CurrentState = state;
                _componentManager.AddComponent(_entityManager.GlobalEntity, gameState);
            }
        }
        
        // 使用新方式设置游戏状态
        public void SetGameState(GameStateComponent.State state)
        {
            if (_componentManager.HasGlobalComponent<GameStateComponent>())
            {
                var gameState = _componentManager.GetGlobalComponent<GameStateComponent>();
                gameState.CurrentState = state;
                _componentManager.AddGlobalComponent(gameState);
            }
        }
    }
}