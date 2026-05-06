using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using ZLockstep.Sync.Command.Commands;

public class GMCommandRunnerEditor : EditorWindow
{
    private class CommandGroup
    {
        public string commandInput = "";
        public System.Action onExecute;
    }

    private List<CommandGroup> commandGroups = new List<CommandGroup>();
    private Vector2 scrollPosition;

    private const string EditorPrefsKey = "GMCommandRunner_CommandGroups";

    [MenuItem("Tools/GM Commands")]
    public static void ShowWindow()
    {
        GetWindow<GMCommandRunnerEditor>("独立 GM 工具");
    }

    private void OnEnable()
    {
        // 初始化时加载保存的命令组
        LoadCommandGroups();

        // 如果没有保存的数据，至少创建一个命令组
        if (commandGroups.Count == 0)
        {
            AddCommandGroup();
        }
    }

    private void OnDisable()
    {
        // 窗口关闭时保存命令组
        SaveCommandGroups();
    }

    private void OnGUI()
    {
        GUILayout.Label("独立 GM 命令工具", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        // 顶部操作栏
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("添加命令组", EditorStyles.toolbarButton, GUILayout.Width(100)))
        {
            AddCommandGroup();
        }

        GUILayout.FlexibleSpace();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // 可滚动区域
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // 显示所有命令组
        for (int i = 0; i < commandGroups.Count; i++)
        {
            DrawCommandGroup(i, commandGroups[i]);
            EditorGUILayout.Space(5);
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // 帮助信息
        EditorGUILayout.HelpBox("直接在编辑器中执行 GM 命令，无需依赖场景中的 GMCommandRunner 组件", MessageType.Info);
    }

    private void DrawCommandGroup(int index, CommandGroup group)
    {
        EditorGUILayout.BeginVertical("box");

        // 组标题栏
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label($"命令组 {index + 1}", EditorStyles.boldLabel, GUILayout.Width(80));

        GUILayout.FlexibleSpace();

        // 删除按钮，至少保留一个组
        if (commandGroups.Count > 1)
        {
            GUIStyle deleteButtonStyle = new GUIStyle(EditorStyles.toolbarButton);
            deleteButtonStyle.normal.textColor = Color.red;

            if (GUILayout.Button("删除", deleteButtonStyle, GUILayout.Width(60)))
            {
                RemoveCommandGroup(index);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(3);

        // 命令输入和执行按钮
        EditorGUILayout.BeginHorizontal();

        group.commandInput = EditorGUILayout.TextField("GM 指令:", group.commandInput, GUILayout.Height(30));

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.normal.textColor = Color.white;

        if (GUILayout.Button("执行命令", buttonStyle, GUILayout.Height(30), GUILayout.Width(120)))
        {
            ExecuteCommand(group.commandInput);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void AddCommandGroup()
    {
        commandGroups.Add(new CommandGroup());
        Debug.Log("[GM] 已添加新的命令组");
        SaveCommandGroups();
    }

    private void RemoveCommandGroup(int index)
    {
        if (index >= 0 && index < commandGroups.Count)
        {
            commandGroups.RemoveAt(index);
            Debug.Log($"[GM] 已删除命令组 {index + 1}");
            SaveCommandGroups();
        }
    }

    private void ExecuteCommand(string commandInput)
    {
        if (string.IsNullOrEmpty(commandInput))
        {
            Debug.LogWarning("[GM] 输入为空，未执行任何操作。");
            return;
        }

        Debug.Log($"[GM] 正在执行指令：{commandInput}");

        // 执行具体的命令逻辑
        ProcessCommand(commandInput.ToLower());
    }

    private void ProcessCommand(string cmd)
    {
        // 提交到游戏系统
        SubmitToGameSystem(cmd);
    }

    private void SubmitToGameSystem(string command)
    {
#if UNITY_EDITOR
        Ra2Demo ra2Demo = Object.FindObjectOfType<Ra2Demo>();
        if (ra2Demo != null)
        {
            var battleGame = ra2Demo.GetBattleGame();
            if (battleGame != null)
            {
                battleGame.SubmitCommand(new GMCommand(command));
                Debug.Log($"[GM] 命令已提交到游戏系统：{command}");
            }
            else
            {
                Debug.LogWarning("[GM] 未找到 BattleGame 实例");
            }
        }
        else
        {
            Debug.LogWarning("[GM] 未找到 Ra2Demo 实例，请确认场景中存在正确的游戏对象");
        }
#endif
    }

#region 持久化方法

    private void SaveCommandGroups()
    {
        // 提取所有命令组的输入内容
        List<string> commands = new List<string>();
        foreach (var group in commandGroups)
        {
            commands.Add(group.commandInput ?? "");
        }

        // 转换为 JSON 格式保存
        string json = JsonUtility.ToJson(new CommandGroupData { commands = commands.ToArray() });
        EditorPrefs.SetString(EditorPrefsKey, json);

        Debug.Log($"[GM] 已保存 {commandGroups.Count} 个命令组");
    }

    private void LoadCommandGroups()
    {
        commandGroups.Clear();

        // 从 EditorPrefs 加载数据
        if (EditorPrefs.HasKey(EditorPrefsKey))
        {
            try
            {
                string json = EditorPrefs.GetString(EditorPrefsKey);
                var data = JsonUtility.FromJson<CommandGroupData>(json);

                if (data != null && data.commands != null)
                {
                    foreach (var cmd in data.commands)
                    {
                        commandGroups.Add(new CommandGroup { commandInput = cmd });
                    }

                    Debug.Log($"[GM] 已加载 {commandGroups.Count} 个保存的命令组");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GM] 加载保存的命令组失败：{e.Message}");
                // 如果加载失败，创建一个空的命令组
                commandGroups.Clear();
            }
        }
    }

    [System.Serializable]
    private class CommandGroupData
    {
        public string[] commands;
    }

#endregion
}
