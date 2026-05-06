using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using ZLockstep.Sync.Determinism;

/// <summary>
/// 回放差异编辑器窗口，负责离线对比确定性摘要文件，并提供编辑器内探针记录开关。
/// </summary>
public class ReplayDiffEditorWindow : EditorWindow
{
    private const string ProbeEnabledPrefsKey = "SimpleRTS.ReplayDiffEditorWindow.ProbeEnabled";

    private string _baselineFileName = string.Empty;
    private string _replayFileName = string.Empty;
    private string _statusMessage = string.Empty;
    private string _resultText = string.Empty;
    private Vector2 _resultScroll;

    private ReplayDiffResult _lastResult;
    private string _lastBaselinePath = string.Empty;
    private string _lastReplayPath = string.Empty;

    private double _copyHintUntilTime;
    private bool _probeEnabled;

    /// <summary>
    /// 打开回放差异对比工具窗口。
    /// </summary>
    [MenuItem("Tools/Replay Diff Tool")]
    public static void ShowWindow()
    {
        GetWindow<ReplayDiffEditorWindow>("Replay Diff Tool");
    }

    /// <summary>
    /// 窗口启用时读取编辑器持久化开关，并同步当前探针自动启用状态。
    /// </summary>
    private void OnEnable()
    {
        _probeEnabled = EditorPrefs.GetBool(ProbeEnabledPrefsKey, false);
        DeterminismProbe.EditorAutoEnable = _probeEnabled;
    }

    private void OnGUI()
    {
        DrawCopyHint();

        EditorGUILayout.LabelField("离线回放差异对比", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        DrawProbeToggle();
        EditorGUILayout.Space();

        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("输入文件名（位于 persistentDataPath）", EditorStyles.boldLabel);
            _baselineFileName = EditorGUILayout.TextField("Baseline 文件名", _baselineFileName);
            _replayFileName = EditorGUILayout.TextField("Replay 文件名", _replayFileName);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("persistentDataPath", Application.persistentDataPath);
        }

        EditorGUILayout.Space();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("对比", GUILayout.Height(28f)))
            {
                Compare();
            }

            if (GUILayout.Button("打开目录", GUILayout.Height(28f), GUILayout.Width(100f)))
            {
                OpenPersistentDataPathDirectory();
            }

            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_resultText)))
            {
                if (GUILayout.Button("复制结果", GUILayout.Height(28f), GUILayout.Width(110f)))
                {
                    CopyResult();
                }
            }
        }

        if (!string.IsNullOrEmpty(_statusMessage))
        {
            EditorGUILayout.HelpBox(_statusMessage, MessageType.Info);
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("结果", EditorStyles.boldLabel);
        _resultScroll = EditorGUILayout.BeginScrollView(_resultScroll, GUILayout.ExpandHeight(true));
        EditorGUILayout.TextArea(_resultText, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 绘制探针记录开关，并在状态变化时持久化到编辑器偏好设置。
    /// </summary>
    private void DrawProbeToggle()
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("探针记录", EditorStyles.boldLabel);
            bool next = EditorGUILayout.Toggle("启用探针记录", _probeEnabled);
            if (next != _probeEnabled)
            {
                _probeEnabled = next;
                EditorPrefs.SetBool(ProbeEnabledPrefsKey, next);
                DeterminismProbe.EditorAutoEnable = next;
                _statusMessage = next
                    ? "已启用编辑器探针记录：后续 Start 会自动开启采样。"
                    : "已关闭编辑器探针记录：后续 Start 保持默认关闭。";
            }

            EditorGUILayout.HelpBox(
                next
                    ? "当前编辑器会在探针 Start 时自动设置 Enabled = true。"
                    : "当前探针保持默认关闭，需要代码显式启用后才会写入。",
                next ? MessageType.Info : MessageType.None);
        }
    }

    private void Compare()
    {
        _resultText = string.Empty;

        if (string.IsNullOrWhiteSpace(_baselineFileName) || string.IsNullOrWhiteSpace(_replayFileName))
        {
            _statusMessage = "请输入 Baseline 与 Replay 文件名。";
            return;
        }

        _lastBaselinePath = Path.Combine(Application.persistentDataPath, _baselineFileName.Trim());
        _lastReplayPath = Path.Combine(Application.persistentDataPath, _replayFileName.Trim());

        if (!File.Exists(_lastBaselinePath))
        {
            _statusMessage = $"Baseline 文件不存在: {_lastBaselinePath}";
            return;
        }

        if (!File.Exists(_lastReplayPath))
        {
            _statusMessage = $"Replay 文件不存在: {_lastReplayPath}";
            return;
        }

        _lastResult = ReplayDiffTool.CompareFiles(_lastBaselinePath, _lastReplayPath);
        _resultText = BuildResultText(_lastResult, _lastBaselinePath, _lastReplayPath);
        _statusMessage = _lastResult != null && _lastResult.IsMatch
            ? "对比完成：未发现分叉。"
            : "对比完成：发现分叉，请查看详情。";
    }

    private void CopyResult()
    {
        if (string.IsNullOrEmpty(_resultText))
        {
            _statusMessage = "当前没有可复制的结果。";
            return;
        }

        EditorGUIUtility.systemCopyBuffer = _resultText;
        _copyHintUntilTime = EditorApplication.timeSinceStartup + 2.0d;
        _statusMessage = "结果已复制到剪贴板。";
        Repaint();
    }

    private void DrawCopyHint()
    {
        if (EditorApplication.timeSinceStartup <= _copyHintUntilTime)
        {
            EditorGUILayout.HelpBox("已复制", MessageType.None);
            EditorGUILayout.Space(4);
        }
    }

    private void OpenPersistentDataPathDirectory()
    {
        string path = Application.persistentDataPath;
        if (!Directory.Exists(path))
        {
            _statusMessage = $"目录不存在: {path}";
            return;
        }

        EditorUtility.RevealInFinder(path);
        _statusMessage = $"已打开目录: {path}";
    }

    private static string BuildResultText(ReplayDiffResult result, string baselinePath, string replayPath)
    {
        var sb = new StringBuilder(1024);
        sb.AppendLine("Replay Diff Result");
        sb.AppendLine($"Baseline Path: {baselinePath}");
        sb.AppendLine($"Replay Path: {replayPath}");
        sb.AppendLine();

        if (result == null)
        {
            sb.AppendLine("Result: <null>");
            return sb.ToString();
        }

        sb.AppendLine($"IsMatch: {result.IsMatch}");
        sb.AppendLine($"FirstMismatchTick: {result.FirstMismatchTick}");
        sb.AppendLine($"MismatchType: {result.MismatchType}");
        sb.AppendLine($"Summary: {result.Summary}");

        if (result.IsMatch)
        {
            sb.AppendLine("结论: 未发现分叉。");
        }

        sb.AppendLine("Details:");
        if (result.Details == null || result.Details.Count == 0)
        {
            sb.AppendLine("- <none>");
        }
        else
        {
            for (int i = 0; i < result.Details.Count; i++)
            {
                sb.AppendLine($"- {result.Details[i]}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// 从编辑器偏好设置读取探针开关，并同步到确定性探针的编辑器自动启用配置。
    /// </summary>
    internal static void SyncProbeAutoEnableFromPrefs()
    {
        DeterminismProbe.EditorAutoEnable = EditorPrefs.GetBool(ProbeEnabledPrefsKey, false);
    }
}

/// <summary>
/// 编辑器启动同步器，负责在不打开工具窗口时也让探针自动启用开关生效。
/// </summary>
[InitializeOnLoad]
internal static class ReplayDiffEditorProbeAutoEnableBootstrap
{
    /// <summary>
    /// 创建编辑器启动同步器，并延迟到编辑器初始化完成后读取持久化开关。
    /// </summary>
    static ReplayDiffEditorProbeAutoEnableBootstrap()
    {
        EditorApplication.delayCall += ReplayDiffEditorWindow.SyncProbeAutoEnableFromPrefs;
    }
}
