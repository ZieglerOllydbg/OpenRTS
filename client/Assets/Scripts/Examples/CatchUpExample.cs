using UnityEngine;
using System.Collections.Generic;
using ZLockstep.View;
using ZLockstep.Sync.Command;
using ZLockstep.Sync.Command.Commands;
using zUnity;

/// <summary>
/// 追帧功能演示
/// 演示如何模拟断线重连和追帧
/// </summary>
public class CatchUpExample : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private GameWorldBridge worldBridge;

    [Header("测试配置")]
    [Tooltip("模拟掉线的帧数")]
    [SerializeField] private int simulatedMissedFrames = 50;

    [Tooltip("自动测试间隔（秒，0=不自动测试）")]
    [SerializeField] private float autoTestInterval = 10f;

    private float _lastTestTime;

    private void Start()
    {
        if (worldBridge == null)
        {
            worldBridge = FindObjectOfType<GameWorldBridge>();
        }

        _lastTestTime = Time.time;
    }

    private void Update()
    {
        // 按键测试
        if (Input.GetKeyDown(KeyCode.C))
        {
            TestCatchUp();
        }

        // 自动测试
        if (autoTestInterval > 0 && Time.time - _lastTestTime > autoTestInterval)
        {
            TestCatchUp();
            _lastTestTime = Time.time;
        }
    }

    /// <summary>
    /// 测试追帧
    /// </summary>
    public void TestCatchUp()
    {
        if (worldBridge == null || worldBridge.Game == null)
        {
            Debug.LogError("[CatchUpExample] GameWorldBridge 未初始化");
            return;
        }

        if (worldBridge.Game.Mode != ZLockstep.Sync.GameMode.NetworkClient)
        {
            Debug.LogWarning("[CatchUpExample] 只有网络模式才能测试追帧，请设置 GameMode = NetworkClient");
            return;
        }

        int currentFrame = worldBridge.LogicWorld.Tick;
        
        Debug.Log($"[CatchUpExample] 模拟掉线，当前帧: {currentFrame}");

        // 生成模拟的追帧数据
        var catchUpCommands = GenerateMockCatchUpCommands(currentFrame, simulatedMissedFrames);

        // 开始追帧
        worldBridge.StartCatchUp(catchUpCommands);

        Debug.Log($"[CatchUpExample] 开始追帧，目标帧: {currentFrame + simulatedMissedFrames}");
    }

    /// <summary>
    /// 生成模拟的追帧命令数据
    /// </summary>
    private Dictionary<int, List<ICommand>> GenerateMockCatchUpCommands(int startFrame, int count)
    {
        var result = new Dictionary<int, List<ICommand>>();

        for (int i = 1; i <= count; i++)
        {
            int frame = startFrame + i;
            var commands = new List<ICommand>();

            // 每10帧创建一个单位（模拟游戏中的操作）
            if (frame % 10 == 0)
            {
                var cmd = new CreateUnitCommand(
                    campId: 0,
                    unitType: 1,
                    position: new zVector3(
                        zfloat.FromRaw(new zRandom(0).NextInt(-5, 5) * 65536),
                        zfloat.Zero,
                        zfloat.FromRaw(new zRandom(0).NextInt(-5, 5) * 65536)
                    ),
                    prefabId: 1
                )
                {
                    Source = CommandSource.Network,
                    ExecuteFrame = frame
                };

                commands.Add(cmd);
            }

            // 即使没有命令，也要添加空帧
            result[frame] = commands;
        }

        return result;
    }

    private void OnGUI()
    {
        if (worldBridge == null || worldBridge.Game == null)
            return;

        // 显示测试按钮
        GUILayout.BeginArea(new Rect(10, Screen.height - 150, 300, 140));

        GUILayout.Label("═══ 追帧测试 ═══", new GUIStyle(GUI.skin.label) 
        { 
            fontSize = 16, 
            fontStyle = FontStyle.Bold 
        });

        GUILayout.Label($"当前帧: {worldBridge.LogicWorld?.Tick ?? 0}");
        GUILayout.Label($"模式: {worldBridge.Game.Mode}");

        if (worldBridge.Game.Mode == ZLockstep.Sync.GameMode.NetworkClient)
        {
            if (GUILayout.Button($"测试追帧（模拟掉线 {simulatedMissedFrames} 帧）", GUILayout.Height(30)))
            {
                TestCatchUp();
            }

            GUILayout.Label($"按 C 键测试追帧");
        }
        else
        {
            GUILayout.Label("请设置 GameMode = NetworkClient");
        }

        GUILayout.EndArea();
    }
}

