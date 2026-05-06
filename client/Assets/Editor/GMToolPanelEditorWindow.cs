using UnityEditor;
using UnityEngine;

/// <summary>
/// GM 工具面板，负责集中提供编辑器内调试开关，便于在不修改预制体的情况下切换常用调试流程。
/// </summary>
public class GMToolPanelEditorWindow : EditorWindow
{
    private bool _soloButtonStartsStandalone;

    /// <summary>
    /// 打开 GM 工具面板窗口，供开发者调整本地调试功能。
    /// </summary>
    [MenuItem("Tools/GM Tool Panel")]
    public static void ShowWindow()
    {
        GetWindow<GMToolPanelEditorWindow>("GM 工具面板");
    }

    /// <summary>
    /// 窗口启用时读取已保存的 Solo 按钮调试模式。
    /// </summary>
    private void OnEnable()
    {
        _soloButtonStartsStandalone = EditorPrefs.GetBool(MatchPanel.SoloButtonStartsStandalonePrefsKey, true);
    }

    /// <summary>
    /// 绘制 GM 工具面板界面，并在开关变化时立即保存配置。
    /// </summary>
    private void OnGUI()
    {
        EditorGUILayout.LabelField("GM 工具面板", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        DrawSoloButtonMode();
    }

    /// <summary>
    /// 绘制 Solo 按钮功能切换区，控制 MatchPanel 中 Solo 按钮点击后的启动路径。
    /// </summary>
    private void DrawSoloButtonMode()
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Solo 按钮功能", EditorStyles.boldLabel);

            bool next = EditorGUILayout.ToggleLeft(
                "点击 Solo 时直接调用 StartStandaloneGame()",
                _soloButtonStartsStandalone
            );

            if (next != _soloButtonStartsStandalone)
            {
                _soloButtonStartsStandalone = next;
                EditorPrefs.SetBool(MatchPanel.SoloButtonStartsStandalonePrefsKey, next);
            }

            string currentMode = _soloButtonStartsStandalone
                ? "当前：StartStandaloneGame()"
                : "当前：NetworkManager.Instance.ConnectToServer(RoomType.SOLO, GetIsLocalNet())";
            EditorGUILayout.HelpBox(currentMode, MessageType.Info);
        }
    }
}
