# GM系统设计与使用文档

## 1. 概述

GM（Game Master）系统是一个用于调试和测试游戏功能的命令系统。它允许开发者通过输入特定命令来触发游戏中的各种功能，如添加物品、修改等级、生成单位等。GM系统基于ECS架构设计，通过命令模式实现，具有良好的扩展性和易用性。

## 2. 系统架构

### 2.1 核心组件

#### GMManager
GMManager是GM系统的核心管理类，负责：
- 注册所有GM命令
- 解析和执行命令
- 记录命令执行日志

#### GMCommandAttribute
GMCommandAttribute是一个特性类，用于标记GM命令方法：
- 标记方法为GM命令
- 定义命令名称

### 2.2 工作原理

1. **命令注册**：GMManager通过反射机制扫描所有标记了[GMCommand]特性的方法，并将其注册到命令字典中。
2. **命令解析**：当用户输入命令时，系统将命令行字符串解析为命令名称和参数数组。
3. **命令执行**：根据命令名称在字典中查找对应的处理方法，并传入参数执行。
4. **日志记录**：记录命令执行过程和结果，便于调试。

## 3. 使用方法

### 3.1 执行命令

通过调用`GMManager.ExecuteCommand(string commandLine)`方法执行GM命令：

```csharp
// 示例
gmManager.ExecuteCommand("additem 1001 5");
gmManager.ExecuteCommand("setlevel 10");
gmManager.ExecuteCommand("addtank 1");
```

### 3.2 命令格式

命令格式为：`命令名称 参数1 参数2 ... 参数N`

例如：
- `additem 1001 5` - 添加ID为1001的物品5个
- `setlevel 10` - 设置等级为10
- `addtank 1` - 为玩家1添加坦克

## 4. 内置命令

### 4.1 additem
添加物品命令
- 用法：`additem <itemId> <amount>`
- 参数：
  - itemId：物品ID
  - amount：物品数量
- 示例：`additem 1001 5`

### 4.2 setlevel
设置玩家等级
- 用法：`setlevel <level>`
- 参数：
  - level：目标等级
- 示例：`setlevel 10`

### 4.3 god
切换无敌模式
- 用法：`god`
- 示例：`god`

### 4.4 addmoney
添加游戏货币
- 用法：`addmoney <amount>`
- 参数：
  - amount：货币数量
- 示例：`addmoney 1000`

### 4.5 addtank
在屏幕中心位置添加坦克单位
- 用法：`addtank [playerId]`
- 参数：
  - playerId：玩家ID（可选，默认为0）
- 示例：
  - `addtank` - 为玩家0添加坦克
  - `addtank 1` - 为玩家1添加坦克

### 4.6 help
显示所有可用命令列表
- 用法：`help`
- 示例：`help`

## 5. 扩展命令

### 5.1 添加新命令

要添加新的GM命令，只需在GMManager类中创建一个新方法，并使用[GMCommand]特性标记：

```csharp
[GMCommand("命令名称")]
private void CommandMethod(string[] args)
{
    // 命令实现
    // 可以通过args数组访问命令参数
    // 使用AddLog方法记录日志
}
```

### 5.2 命令实现示例

```csharp
[GMCommand("heal")]
private void Heal(string[] args)
{
    if (args.Length < 1)
    {
        AddLog("Usage: heal <amount>");
        return;
    }
    
    int amount = int.Parse(args[0]);
    // 在这里实现治疗逻辑
    AddLog($"Healed {amount} HP");
}
```

## 6. 日志系统

GM系统内置了日志记录功能：
- 所有命令执行都会被记录到`LogHistory`列表中
- 日志同时会输出到Unity控制台
- 可用于调试和追踪命令执行情况

## 7. 注意事项

1. **命令名称不区分大小写**：additem和ADDITEM是相同的命令
2. **参数解析**：参数通过空格分隔，不支持引号包裹包含空格的参数
3. **错误处理**：命令执行中的异常会被捕获并记录到日志中
4. **扩展性**：通过反射机制自动注册命令，新增命令只需添加标记了特性的方法

## 8. 与ECS系统的集成

GM系统与ECS系统紧密集成：
- GM命令可以通过提交Command到zWorld来影响游戏世界状态
- 命令执行遵循帧同步原则，确保多端一致性
- 可以直接操作Entity、Component和System

例如，[addtank](file:///c%3A/MyZiegler/github/ra2/client/Packages/ZLockstep/Runtime/Simulation/ECS/GMManager.cs#L148-L195)命令通过创建CreateTankCommand并提交给_world.GameInstance来生成单位，确保了操作的一致性和同步性。