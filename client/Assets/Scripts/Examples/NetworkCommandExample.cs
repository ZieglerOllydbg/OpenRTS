using UnityEngine;
using System.Collections.Generic;
using ZLockstep.View;
using ZLockstep.Sync.Command;
using ZLockstep.Sync.Command.Commands;
using zUnity;
using Newtonsoft.Json.Linq;

/// <summary>
/// 网络命令示例
/// 演示如何在网络同步环境下使用Command系统
/// </summary>
public class NetworkCommandExample : MonoBehaviour
{
    [Header("设置")]
    [SerializeField] private GameWorldBridge worldBridge;
    [SerializeField] private int localPlayerId = 0;

    // 模拟：接收到的网络命令缓冲区（按帧号组织）
    private Dictionary<int, List<ICommand>> _receivedCommands = new Dictionary<int, List<ICommand>>();

    private void Start()
    {
        // 模拟：注册网络消息接收回调
        // 在真实项目中，这里应该连接到网络库（如Mirror、Photon等）
        SimulateNetworkReceive();
    }

    /// <summary>
    /// 模拟接收网络命令
    /// 在真实项目中，这个方法应该在收到网络消息时调用
    /// </summary>
    private void SimulateNetworkReceive()
    {
        // 模拟场景：假设在第100帧时收到远程玩家的创建单位命令
        int executeFrame = 100;

        var remoteCreateCommand = new CreateUnitCommand(
            campId: 2, // 远程玩家ID
            unitType: 1,
            position: new zVector3(5, 0, 5),
            prefabId: 1
        )
        {
            Source = CommandSource.Network,
            ExecuteFrame = executeFrame
        };

        // 提交到游戏世界
        // CommandManager会自动在正确的帧执行
        worldBridge?.SubmitCommand(remoteCreateCommand);

        Debug.Log($"[Network] 接收到远程命令：在第{executeFrame}帧创建单位");
    }

    /// <summary>
    /// 发送本地命令到网络
    /// 当本地玩家执行操作时调用
    /// </summary>
    public void SendLocalCommandToNetwork(ICommand command)
    {
        // 1. 设置命令属性
        command.CampId = localPlayerId;
        command.Source = CommandSource.Local;
        
        // 2. 计算预定执行帧（当前帧 + 网络延迟补偿）
        int currentFrame = worldBridge.LogicWorld.Tick;
        int networkDelay = 3; // 假设3帧的网络延迟
        command.ExecuteFrame = currentFrame + networkDelay;

        // 3. 序列化命令
        // var commandData = command.Serialize();

        // 4. 发送到网络（这里是模拟）
        SimulateSendToNetwork(null, command.ExecuteFrame);

        // 5. 本地也提交命令（确保一致性）
        worldBridge.SubmitCommand(command);

        Debug.Log($"[Network] 发送本地命令: 类型={command}, 执行帧={command.ExecuteFrame}");
    }

    /// <summary>
    /// 模拟发送数据到网络
    /// 在真实项目中，这里应该使用网络库发送数据
    /// </summary>
    private void SimulateSendToNetwork(JObject data, int executeFrame)
    {
        // 模拟：通过网络库发送
        // NetworkClient.Send(MessageType.GameCommand, data);
        
        Debug.Log($"[Network] 发送 {data} 字节到网络，执行帧: {executeFrame}");
    }

    #region 示例：使用方法

    /// <summary>
    /// 示例：本地玩家创建单位
    /// </summary>
    [ContextMenu("测试：发送创建单位命令")]
    public void TestSendCreateUnitCommand()
    {
        var command = new CreateUnitCommand(
            campId: localPlayerId,
            unitType: 1,
            position: new zVector3(0, 0, 0),
            prefabId: 1
        );

        SendLocalCommandToNetwork(command);
    }

    /// <summary>
    /// 示例：本地玩家移动单位
    /// </summary>
    [ContextMenu("测试：发送移动命令")]
    public void TestSendMoveCommand()
    {
        var command = new MoveCommand(
            campId: localPlayerId,
            entityIds: new[] { 0 }, // 假设Entity ID为0
            targetPosition: new zVector3(10, 0, 10)
        );

        SendLocalCommandToNetwork(command);
    }

    #endregion
}

