// GMManager.cs
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using ZLockstep.Simulation;
using ZLockstep.Simulation.ECS;
using ZLockstep.Simulation.ECS.Components;  // 添加这个using语句
using ZLockstep.Simulation.ECS.Systems.AI;
using ZLockstep.Sync.Command.Commands;
using zUnity;

public class GMManager
{
    // 存储指令和对应方法的字典
    private readonly Dictionary<string, Action<string[]>> _commandDictionary = new(StringComparer.OrdinalIgnoreCase);

    private readonly zWorld _world;

    public List<string> LogHistory = new();
    
    public GMManager(zWorld world)
    {
        _world = world;
        RegisterCommands(); // 注册所有指令
    }

    // 注册指令的方法
    private void RegisterCommands()
    {
        _commandDictionary.Clear();
        
        // 使用反射注册所有带有GMCommandAttribute的方法
        MethodInfo[] methods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        
        foreach (MethodInfo method in methods)
        {
            GMCommandAttribute attribute = method.GetCustomAttribute<GMCommandAttribute>();
            if (attribute != null)
            {
                string commandName = attribute.CommandName;
                Action<string[]> action = (Action<string[]>)Delegate.CreateDelegate(typeof(Action<string[]>), this, method);
                _commandDictionary[commandName] = action;
            }
        }
        
        // 特殊处理help命令，因为它是动态生成的
        _commandDictionary.Add("help", (args) => { AddLog("Available Commands: " + string.Join(", ", _commandDictionary.Keys)); });
    }

    // 执行指令的核心方法
    public void ExecuteCommand(string commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine))
        {
            AddLog("Invalid Command: Empty");
            return;
        }

        AddLog($"> {commandLine}"); // 记录输入

        // 解析指令，例如 "additem 1 5" -> 命令是 "additem", 参数是 ["1", "5"]
        string[] parts = commandLine.Split(' ');
        string command = parts[0].ToLower();

        if (_commandDictionary.ContainsKey(command))
        {
            try
            {
                // 执行指令对应的Action，并传入参数（parts[1..]）
                _commandDictionary[command]?.Invoke(parts[1..]);
                AddLog($"Command '{command}' executed successfully.");
            }
            catch (Exception e)
            {
                AddLog($"Error executing '{command}': {e.Message}");
            }
        }
        else
        {
            AddLog($"Command not found: {command}. Type 'help' for list.");
        }
    }

    private void AddLog(string log)
    {
        LogHistory.Add($"GM: {log}");
        zUDebug.Log($"[GM] {log}"); // 同时输出到Unity控制台
    }

    // ========== 具体的GM指令方法 ==========
    [GMCommand("additem")]
    private void AddItem(string[] args)
    {
        if (args.Length < 2)
        {
            AddLog("Usage: additem <itemId> <amount>");
            return;
        }
        int itemId = int.Parse(args[0]);
        int amount = int.Parse(args[1]);
        // 调用你的游戏系统，例如：InventoryManager.Instance.AddItem(itemId, amount);
        AddLog($"GM: Added item {itemId} x {amount}");
    }

    [GMCommand("setlevel")]
    private void SetLevel(string[] args)
    {
        if (args.Length < 1)
        {
            AddLog("Usage: setlevel <level>");
            return;
        }
        int level = int.Parse(args[0]);
        // 调用你的游戏系统，例如：PlayerExperience.Instance.SetLevel(level);
        AddLog($"GM: Set level to {level}");
    }

    [GMCommand("god")]
    private void ToggleGodMode(string[] args)
    {
        // 切换无敌模式
        // bool isGodMode = !PlayerStats.Instance.isInvincible;
        // PlayerStats.Instance.isInvincible = isGodMode;
        AddLog($"GM: God Mode Toggled");
    }

    [GMCommand("addmoney")]
    private void AddMoney(string[] args)
    {
        if (args.Length < 1)
        {
            AddLog("Usage: addmoney <amount>");
            return;
        }
        int amount = int.Parse(args[0]);
        // CurrencyManager.Instance.AddCurrency(amount);
        AddLog($"GM: Added money: {amount}");
    }

    [GMCommand("addcampmoney")]
    private void AddCampMoney(string[] args)
    {
        if (args.Length < 2)
        {
            AddLog("Usage: addcampmoney <campId> <amount>");
            AddLog("Example: addcampmoney 0 1000  (Add 1000 money to camp 0)");
            return;
        }

        if (!int.TryParse(args[0], out int campId))
        {
            AddLog("Error: Invalid camp ID (must be a number)");
            return;
        }

        if (!int.TryParse(args[1], out int amount))
        {
            AddLog("Error: Invalid amount (must be a number)");
            return;
        }

        // 查找玩家的经济组件
        var (economyComponent, entity) = _world.ComponentManager.GetComponentWithCondition<EconomyComponent>(
            e => _world.ComponentManager.HasComponent<CampComponent>(e) && 
                 _world.ComponentManager.GetComponent<CampComponent>(e).CampId == campId);
        
        if (entity.Id != -1)
        {
            // 找到玩家的经济组件，增加金币
            int oldMoney = economyComponent.Money;
            economyComponent.Money += amount;
            int newMoney = economyComponent.Money;
            
            // 保存更新后的组件
            _world.ComponentManager.AddComponent(entity, economyComponent);
            
            AddLog($"GM: Added {amount} money to camp {campId} (Old: {oldMoney}, New: {newMoney})");
        }
        else
        {
            AddLog($"Error: Camp {campId} not found or doesn't have EconomyComponent");
        }
    }

    [GMCommand("setgmpower")]
    private void SetGMPower(string[] args)
    {
        if (args.Length < 2)
        {
            AddLog("Usage: setgmpower <campId> <powerAmount>");
            AddLog("Example: setgmpower 0 5000  (Set GM power to 5000 for camp 0)");
            return;
        }

        if (!int.TryParse(args[0], out int campId))
        {
            AddLog("Error: Invalid camp ID (must be a number)");
            return;
        }

        if (!int.TryParse(args[1], out int powerAmount))
        {
            AddLog("Error: Invalid power amount (must be a number)");
            return;
        }

        // 查找玩家的经济组件
        var (economyComponent, entity) = _world.ComponentManager.GetComponentWithCondition<EconomyComponent>(
            e => _world.ComponentManager.HasComponent<CampComponent>(e) && 
                 _world.ComponentManager.GetComponent<CampComponent>(e).CampId == campId);
        
        if (entity.Id != -1)
        {
            // 找到玩家的经济组件，设置 GM 能量
            int oldPower = economyComponent.Power;
            int oldGMPower = economyComponent.GMPower;
            economyComponent.GMPower = powerAmount;
            int newGMPower = economyComponent.GMPower;
            
            // 保存更新后的组件
            _world.ComponentManager.AddComponent(entity, economyComponent);
            
            AddLog($"GM: Set GMPower for camp {campId} (Old Power: {oldPower}, Old GMPower: {oldGMPower}, New GMPower: {newGMPower})");
        }
        else
        {
            AddLog($"Error: Camp {campId} not found or doesn't have EconomyComponent");
        }
    }

    [GMCommand("addunit")]
    private void AddUnit(string[] args)
    {
        // 获取BattleGame实例
        if (_world == null)
        {
            AddLog("Error: BattleGame instance not found");
            return;
        }

        // 检查参数
        if (args.Length < 1)
        {
            AddLog("Usage: addunit <unitType> [campId] [count]");
            AddLog("Available unit types: Infantry(1), badgerTank(2), grizzlyTank(3), Harvester(4)");
            AddLog("Example: addunit 3        (Add 1 grizzlyTank at screen center for player 0)");
            AddLog("Example: addunit 2 1      (Add 1 badgerTank at screen center for player 1)");
            AddLog("Example: addunit 3 0 5    (Add 5 grizzlyTanks at screen center for player 0)");
            AddLog("Example: addunit 1 0 10   (Add 10 Infantry at screen center for player 0)");
            return;
        }

        // 解析单位类型
        if (!int.TryParse(args[0], out int unitTypeInt))
        {
            AddLog("Error: Invalid unit type (must be a number)");
            return;
        }

        UnitType unitType = (UnitType)unitTypeInt;

        // 验证枚举值是否有效
        if (!Enum.IsDefined(typeof(UnitType), unitType) || unitType == UnitType.None || unitType == UnitType.Projectile)
        {
            AddLog($"Error: Invalid unit type {unitTypeInt}. Available: Infantry(1), badgerTank(2), grizzlyTank(3), Harvester(4)");
            return;
        }

        // 默认参数
        int campId = 0; // 默认为玩家0（本地玩家）
        int count = 1;  // 默认数量为1

        // 解析阵营参数
        if (args.Length >= 2)
        {
            if (!int.TryParse(args[1], out campId))
            {
                AddLog("Error: Invalid camp ID (must be a number)");
                return;
            }
        }

        // 解析数量参数
        if (args.Length >= 3)
        {
            if (!int.TryParse(args[2], out count) || count <= 0)
            {
                AddLog("Error: Invalid count (must be a positive number)");
                return;
            }
            if (count > 50)
            {
                AddLog("Warning: Count capped at 50 to prevent performance issues");
                count = 50;
            }
        }

        // 获取屏幕中心位置的世界坐标
        Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0);
        Ray ray = Camera.main.ScreenPointToRay(screenCenter);

        // 创建一个平面（y=0）来接收射线
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        float distance;
        Vector3 basePosition;

        if (groundPlane.Raycast(ray, out distance))
        {
            basePosition = ray.GetPoint(distance);
        }
        else
        {
            // 如果射线未击中平面，使用默认位置
            basePosition = new Vector3(64, 0, 64); // 地图中心
            AddLog("Warning: Could not raycast to ground, using default position (64, 0, 64)");
        }

        // 添加多个单位，位置稍微分散
        for (int i = 0; i < count; i++)
        {
            // 计算偏移位置，使单位分散排列
            float offsetX = (i % 5) * 3f - 6f; // 每行5个，间距3
            float offsetZ = (i / 5) * 3f;       // 行间距3

            Vector3 spawnPosition = basePosition + new Vector3(offsetX, 0, offsetZ);

            CreateTankCommand createTankCommand = new(
                campId: campId,
                unitType: unitType,
                position: new zVector3((zfloat)spawnPosition.x, (zfloat)spawnPosition.y, (zfloat)spawnPosition.z)
            );

            _world.GameInstance.SubmitCommand(createTankCommand);
        }

        AddLog($"GM: Added {count} {unitType} near ({basePosition.x:F2}, {basePosition.z:F2}) for player {campId}");
    }

    [GMCommand("setrotation")]
    private void SetEntityRotation(string[] args)
    {
        // 检查参数
        if (args.Length < 4)
        {
            AddLog("Usage: setrotation <entityId> <x> <y> <z>");
            AddLog("Example: setrotation 1 0 1 0  (face positive Z direction)");
            AddLog("Example: setrotation 1 1 0 0  (face positive X direction)");
            return;
        }

        // 解析参数
        if (!int.TryParse(args[0], out int entityId))
        {
            AddLog("Error: Invalid entity ID");
            return;
        }

        if (!float.TryParse(args[1], out float x) ||
            !float.TryParse(args[2], out float y) ||
            !float.TryParse(args[3], out float z))
        {
            AddLog("Error: Invalid rotation values (must be numbers)");
            return;
        }

        // 创建目标方向向量
        zVector3 targetDirection = new zVector3((zfloat)x, (zfloat)y, (zfloat)z);
        
        // 检查向量是否为零向量
        if (targetDirection.sqrMagnitude < new zfloat(0, 1))
        {
            AddLog("Error: Direction vector cannot be zero");
            return;
        }

        // 归一化方向向量
        targetDirection = targetDirection.normalized;

        // 创建实体
        Entity entity = new Entity(entityId);

        // 检查实体是否存在以及是否有TransformComponent
        if (!_world.ComponentManager.HasComponent<TransformComponent>(entity))
        {
            AddLog($"Error: Entity {entityId} not found or doesn't have TransformComponent");
            return;
        }

        // 获取当前的Transform组件
        var transform = _world.ComponentManager.GetComponent<TransformComponent>(entity);

        // 创建新的朝向四元数
        // 使用LookRotation创建朝向指定方向的四元数
        zQuaternion newRotation = zQuaternion.LookRotation(targetDirection);

        // 更新Transform组件
        transform.Rotation = newRotation;
        
        // 保存更新后的组件
        _world.ComponentManager.AddComponent(entity, transform);

        AddLog($"GM: Set entity {entityId} rotation to direction ({x:F2}, {y:F2}, {z:F2})");
        AddLog($"New rotation: {newRotation.ToString()}");
    }

    [GMCommand("setrotationy")]
    private void SetEntityRotationY(string[] args)
    {
        // 更简单的版本：只设置 Y 轴旋转（绕垂直轴旋转）
        if (args.Length < 2)
        {
            AddLog("Usage: setrotationy <entityId> <angle>");
            AddLog("Example: setrotationy 1 90  (rotate 90 degrees clockwise)");
            AddLog("Example: setrotationy 1 -45 (rotate 45 degrees counter-clockwise)");
            return;
        }

        // 解析参数
        if (!int.TryParse(args[0], out int entityId))
        {
            AddLog("Error: Invalid entity ID");
            return;
        }

        if (!float.TryParse(args[1], out float angle))
        {
            AddLog("Error: Invalid angle value (must be a number)");
            return;
        }

        // 创建实体
        Entity entity = new Entity(entityId);

        // 检查实体是否存在以及是否有 TransformComponent
        if (!_world.ComponentManager.HasComponent<TransformComponent>(entity))
        {
            AddLog($"Error: Entity {entityId} not found or doesn't have TransformComponent");
            return;
        }

        // 获取当前的 Transform 组件
        var transform = _world.ComponentManager.GetComponent<TransformComponent>(entity);

        // 创建绕 Y 轴旋转的四元数
        // 将角度转换为 zfloat 格式（放大 10000 倍）
        long angleScaled = (long)(angle * 100); // 转换为百分之一度
        zfloat angleZFloat = zfloat.FromRaw(angleScaled);
        
        zVector3 upAxis = new zVector3(zfloat.Zero, zfloat.One, zfloat.Zero);
        zQuaternion yRotation = zQuaternion.AngleAxis(angleZFloat, upAxis);

        zUDebug.Log("[攻击朝向调试]oldRotation: " + transform.Rotation.ToString() + ", newRotation: " + yRotation.ToString());
        // 应用旋转（相对于当前朝向）
        transform.Rotation = yRotation;
        transform.Scale = zVector3.one * 2;
        
        // 保存更新后的组件
        _world.ComponentManager.AddComponent(entity, transform);

        AddLog($"GM: Rotated entity {entityId} by {angle:F1} degrees around Y axis");
        AddLog($"New rotation: {transform.Rotation.ToString()}");
    }

    [GMCommand("addaiaction")]
    private void AddAIAction(string[] args)
    {
        if (args.Length < 1)
        {
            AddLog("Usage: addaiaction <actionType1> [actionType2] [actionType3] ...");
            AddLog("Available actions: Produce(1), Attack(2)");
            AddLog("Example: addaiaction 1  (Add Produce action)");
            AddLog("Example: addaiaction 1 2  (Add both Produce and Attack actions)");
            return;
        }

        // 获取全局 AIComponent
        var aiComponent = _world.ComponentManager.GetGlobalComponent<AIComponent>();
        
        List<AIActionType> addedActions = new();
        List<string> errorMessages = new();

        // 遍历所有参数，支持添加多个行为
        foreach (string arg in args)
        {
            if (!int.TryParse(arg, out int actionTypeInt))
            {
                errorMessages.Add($"Invalid action type: {arg}");
                continue;
            }

            AIActionType actionType = (AIActionType)actionTypeInt;

            // 验证枚举值是否有效
            if (!Enum.IsDefined(typeof(AIActionType), actionType))
            {
                errorMessages.Add($"Invalid action type {actionType}. Available: Produce(1), Attack(2)");
                continue;
            }

            // 检查是否已存在
            if (aiComponent.EnabledActions.Contains(actionType))
            {
                errorMessages.Add($"Action {actionType} is already enabled");
                continue;
            }

            // 添加行为
            aiComponent.EnabledActions.Add(actionType);
            addedActions.Add(actionType);
        }

        // 保存更新后的组件
        if (addedActions.Count > 0)
        {
            _world.ComponentManager.AddGlobalComponent(aiComponent);
        }
        
        // 输出结果
        if (addedActions.Count > 0)
        {
            AddLog($"GM: Added {addedActions.Count} AI action(s): {string.Join(", ", addedActions)}");
            AddLog($"Enabled actions: {string.Join(", ", aiComponent.EnabledActions)}");
        }
        
        // 输出错误信息（如果有）
        if (errorMessages.Count > 0)
        {
            foreach (string error in errorMessages)
            {
                AddLog($"Error: {error}");
            }
        }

        if (addedActions.Count == 0 && errorMessages.Count > 0)
        {
            AddLog("GM: No actions were added");
        }
    }

    [GMCommand("removeaiaction")]
    private void RemoveAIAction(string[] args)
    {
        if (args.Length < 1)
        {
            AddLog("Usage: removeaiaction <actionType1> [actionType2] [actionType3] ...");
            AddLog("Available actions: Produce(1), Attack(2)");
            AddLog("Example: removeaiaction 1  (Remove Produce action)");
            AddLog("Example: removeaiaction 1 2  (Remove both Produce and Attack actions)");
            return;
        }

        // 获取全局 AIComponent
        var aiComponent = _world.ComponentManager.GetGlobalComponent<AIComponent>();
        
        List<AIActionType> removedActions = new();
        List<string> errorMessages = new();

        // 遍历所有参数，支持移除多个行为
        foreach (string arg in args)
        {
            if (!int.TryParse(arg, out int actionTypeInt))
            {
                errorMessages.Add($"Invalid action type: {arg}");
                continue;
            }

            AIActionType actionType = (AIActionType)actionTypeInt;

            // 验证枚举值是否有效
            if (!Enum.IsDefined(typeof(AIActionType), actionType))
            {
                errorMessages.Add($"Invalid action type {actionType}. Available: Produce(1), Attack(2)");
                continue;
            }

            // 检查是否存在
            if (!aiComponent.EnabledActions.Contains(actionType))
            {
                errorMessages.Add($"Action {actionType} is not enabled");
                continue;
            }

            // 移除行为
            aiComponent.EnabledActions.Remove(actionType);
            removedActions.Add(actionType);
        }

        // 保存更新后的组件
        if (removedActions.Count > 0)
        {
            _world.ComponentManager.AddGlobalComponent(aiComponent);
        }
        
        // 输出结果
        if (removedActions.Count > 0)
        {
            AddLog($"GM: Removed {removedActions.Count} AI action(s): {string.Join(", ", removedActions)}");
            AddLog($"Remaining actions: {string.Join(", ", aiComponent.EnabledActions)}");
        }
        
        // 输出错误信息（如果有）
        if (errorMessages.Count > 0)
        {
            foreach (string error in errorMessages)
            {
                AddLog($"Error: {error}");
            }
        }

        if (removedActions.Count == 0 && errorMessages.Count > 0)
        {
            AddLog("GM: No actions were removed");
        }
    }

    [GMCommand("listaiactions")]
    private void ListAIActions(string[] args)
    {
        // 获取全局 AIComponent
        var aiComponent = _world.ComponentManager.GetGlobalComponent<AIComponent>();

        AddLog($"Current enabled AI actions: {string.Join(", ", aiComponent.EnabledActions)}");
        AddLog($"Available actions: Produce(1), Attack(2)");
    }
}