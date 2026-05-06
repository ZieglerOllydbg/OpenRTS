using UnityEngine;
using ZLockstep.View;
using ZLockstep.Sync;
using ZLockstep.Sync.Command;
using ZLockstep.Sync.Command.Commands;
using System.Collections.Generic;
using zUnity;

/// <summary>
/// 网络模式简单示例
/// 演示如何模拟帧同步
/// </summary>
public class NetworkExampleSimple : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private GameWorldBridge worldBridge;

    [Header("测试配置")]
    [Tooltip("模拟网络延迟（秒）")]
    [SerializeField] private float simulatedDelay = 0.1f;

    [Tooltip("自动确认帧（模拟服务器）")]
    [SerializeField] private bool autoConfirmFrames = true;

    // 模拟服务器
    private int _serverFrame = -1;
    private Dictionary<int, List<ICommand>> _serverCommandBuffer = new Dictionary<int, List<ICommand>>();

    // 简单的网络适配器
    private SimpleNetworkAdapter _networkAdapter;

    private void Start()
    {
        if (worldBridge == null)
        {
            worldBridge = FindObjectOfType<GameWorldBridge>();
        }

        // 创建网络适配器
        _networkAdapter = new SimpleNetworkAdapter(this);

        // 检查是否是网络模式
        if (worldBridge.Game.Mode == GameMode.NetworkClient)
        {
            Debug.Log("[NetworkExample] 网络客户端模式已启用");

            if (autoConfirmFrames)
            {
                // 模拟服务器：每帧自动确认
                InvokeRepeating(nameof(SimulateServerConfirmFrame), 1f, 0.05f); // 20FPS
            }
        }
        else
        {
            Debug.LogWarning("[NetworkExample] 当前不是网络模式，请在GameWorldBridge中设置GameMode为NetworkClient");
        }
    }

    /// <summary>
    /// 模拟服务器确认帧
    /// </summary>
    private void SimulateServerConfirmFrame()
    {
        if (worldBridge.Game.Mode != GameMode.NetworkClient)
            return;

        _serverFrame++;

        // 获取该帧的命令
        List<ICommand> commands = null;
        if (_serverCommandBuffer.TryGetValue(_serverFrame, out var cmdList))
        {
            commands = cmdList;
            _serverCommandBuffer.Remove(_serverFrame);
        }

        // 模拟网络延迟
        StartCoroutine(DelayedConfirm(_serverFrame, commands));
    }

    /// <summary>
    /// 延迟确认（模拟网络延迟）
    /// </summary>
    private System.Collections.IEnumerator DelayedConfirm(int frame, List<ICommand> commands)
    {
        yield return new WaitForSeconds(simulatedDelay);

        // 确认帧
        if (worldBridge.Game.FrameSyncManager != null)
        {
            worldBridge.Game.FrameSyncManager.ConfirmFrame(frame, commands);
            
            int cmdCount = commands?.Count ?? 0;
            Debug.Log($"[Server] 确认 Frame {frame}, 命令数={cmdCount}");
        }
    }

    /// <summary>
    /// 接收客户端命令（模拟服务器接收）
    /// </summary>
    public void OnClientCommand(ICommand command)
    {
        int targetFrame = _serverFrame + 1; // 下一帧执行

        if (!_serverCommandBuffer.ContainsKey(targetFrame))
        {
            _serverCommandBuffer[targetFrame] = new List<ICommand>();
        }

        _serverCommandBuffer[targetFrame].Add(command);

        Debug.Log($"[Server] 收到命令: {command.GetType().Name}, 将在 Frame {targetFrame} 执行");
    }

    private void OnGUI()
    {
        if (worldBridge.Game.Mode != GameMode.NetworkClient)
            return;

        // 显示帧同步状态
        GUILayout.BeginArea(new Rect(10, 10, 400, 200));
        GUILayout.Label("═══ 帧同步状态 ═══", new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold });

        if (worldBridge.Game.FrameSyncManager != null)
        {
            string status = worldBridge.Game.FrameSyncManager.GetStatusInfo();
            GUILayout.Label(status);
        }

        GUILayout.Label($"服务器帧: {_serverFrame}");
        GUILayout.Label($"网络延迟: {simulatedDelay * 1000}ms");
        GUILayout.Label($"待处理命令: {_serverCommandBuffer.Count}帧");

        GUILayout.EndArea();
    }

    /// <summary>
    /// 简单的网络适配器（模拟）
    /// </summary>
    private class SimpleNetworkAdapter : INetworkAdapter
    {
        private NetworkExampleSimple _example;

        public SimpleNetworkAdapter(NetworkExampleSimple example)
        {
            _example = example;
        }

        public void SendCommandToServer(ICommand command)
        {
            // 模拟发送到服务器
            _example.OnClientCommand(command);
        }
    }

    private void OnDestroy()
    {
        CancelInvoke();
    }
}

