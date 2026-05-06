using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Game.Examples;
using Game.RA2.Client;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using ZFrame;
using ZLockstep.Simulation.ECS.Components;
using ZLockstep.Sync;
using ZLockstep.Sync.Command;
using ZLockstep.Sync.Command.Commands;
using zUnity;

/// <summary>
/// 回放子面板控制器，负责回放文件列表展示、文件筛选排序、滚动布局刷新、点击关闭以及回放启动流程。
/// </summary>
public class MatchReplaySubPanel
{
    private const string ReplayFilePrefix = "replay_";
    private const string ReplayFileSuffix = ".txt";
    private const string ReplayFileTimeFormat = "yyyyMMdd_HHmmss";
    private const string HistoryFramesFilePrefix = "history_frames_";
    private const string HistoryFramesFileSuffix = ".json";

    /// <summary>
    /// 回放文件列表项数据，保存展示所需的文件名、玩家人数和录制时间。
    /// </summary>
    private sealed class ReplayFileInfo
    {
        /// <summary>
        /// 回放或历史帧文件名，用于列表展示和点击请求。
        /// </summary>
        public string FileName;

        /// <summary>
        /// 回放列表中的显示名称，服务器历史帧会使用便于阅读的短格式。
        /// </summary>
        public string DisplayName;

        /// <summary>
        /// 文件名中解析出的玩家人数，用于本地回放创建战斗世界。
        /// </summary>
        public int PlayerNumber;

        /// <summary>
        /// 本地回放文件名中解析出的录制时间。
        /// </summary>
        public DateTime RecordTime;

        /// <summary>
        /// 统一排序值，用于混排本地回放和服务器历史帧文件。
        /// </summary>
        public long SortOrder;

        /// <summary>
        /// 标记该列表项是否为服务器历史帧占位文件。
        /// </summary>
        public bool IsServerHistoryFile;
    }

    private readonly Func<bool> getUseLocalServer;
    private readonly GameObject root;
    private readonly Button rootCloseButton;
    private readonly ScrollRect listScrollRect;
    private readonly RectTransform viewportRect;
    private readonly Transform listContent;
    private readonly GameObject itemTemplate;
    private readonly List<GameObject> itemInstances = new();
    private readonly List<Button> itemButtons = new();

    /// <summary>
    /// 初始化回放子面板并绑定节点引用与关闭事件，要求父节点下包含 ReplaySubPanel 结构。
    /// </summary>
    /// <param name="panelRoot">MatchPanel 根节点 Transform。</param>
    /// <param name="getUseLocalServer">获取当前是否使用本地服务器的回调。</param>
    public MatchReplaySubPanel(Transform panelRoot, Func<bool> getUseLocalServer)
    {
        this.getUseLocalServer = getUseLocalServer;
        root = panelRoot?.Find("ReplaySubPanel")?.gameObject;
        if (root == null)
        {
            return;
        }

        rootCloseButton = root.GetComponent<Button>() ?? root.AddComponent<Button>();
        rootCloseButton.transition = Selectable.Transition.None;
        rootCloseButton.onClick.AddListener(OnRootClick);

        listScrollRect = root.transform.Find("ScrollView")?.GetComponent<ScrollRect>();
        viewportRect = root.transform.Find("ScrollView/Viewport") as RectTransform;
        listContent = root.transform.Find("ScrollView/Viewport/Content");
        itemTemplate = listContent?.Find("ItemTemplate")?.gameObject;
        if (itemTemplate != null)
        {
            itemTemplate.SetActive(false);
        }
    }

    /// <summary>
    /// 打开回放子面板并刷新文件列表，刷新后会重新计算滚动内容高度并定位到顶部。
    /// </summary>
    public void ShowAndRefresh()
    {
        if (root == null)
        {
            return;
        }

        root.SetActive(true);
        RefreshList();
    }

    /// <summary>
    /// 隐藏回放子面板，不销毁当前实例和缓存的节点引用。
    /// </summary>
    public void Hide()
    {
        if (root == null)
        {
            return;
        }

        root.SetActive(false);
    }

    /// <summary>
    /// 销毁回放子面板控制器的事件绑定与动态列表实例，防止重复打开时监听累积。
    /// </summary>
    public void Destroy()
    {
        if (rootCloseButton != null)
        {
            rootCloseButton.onClick.RemoveListener(OnRootClick);
        }

        ClearList();
    }

    /// <summary>
    /// 重新读取持久化目录中的回放文件，并刷新滚动列表展示内容。
    /// </summary>
    private void RefreshList()
    {
        ClearList();

        if (listContent == null || itemTemplate == null)
        {
            zUDebug.LogWarning("[ReplaySubPanel] 回放列表节点未找到，无法刷新回放列表。");
            return;
        }

        List<ReplayFileInfo> replayFiles = GetReplayFiles();
        if (replayFiles.Count == 0)
        {
            CreateListItem("暂无回放文件", "暂无回放文件", false);
            RefreshContentLayout();
            return;
        }

        for (int i = 0; i < replayFiles.Count; i++)
        {
            CreateListItem(replayFiles[i].DisplayName, replayFiles[i].FileName, true, replayFiles[i].PlayerNumber, replayFiles[i].IsServerHistoryFile);
        }

        RefreshContentLayout();
    }

    /// <summary>
    /// 从当前平台的持久化目录读取可用回放文件，并按录制时间倒序返回。
    /// </summary>
    /// <returns>已经解析出人数和录制时间的回放文件列表。</returns>
    private List<ReplayFileInfo> GetReplayFiles()
    {
        List<ReplayFileInfo> replayFiles = new();

        string[] files = CommandReader.GetPersistentDataFileNames();
        for (int i = 0; i < files.Length; i++)
        {
            string fileName = files[i];
            TryAddReplayFileInfo(replayFiles, fileName);
        }

        replayFiles.Sort((a, b) => b.SortOrder.CompareTo(a.SortOrder));
        return replayFiles;
    }

    /// <summary>
    /// 尝试解析回放文件名，并在格式合法时加入回放文件列表。
    /// </summary>
    /// <param name="replayFiles">用于展示的回放文件列表。</param>
    /// <param name="fileName">回放文件名。</param>
    private void TryAddReplayFileInfo(List<ReplayFileInfo> replayFiles, string fileName)
    {
        if (TryParseReplayFileName(fileName, out int playerNumber, out DateTime recordTime))
        {
            replayFiles.Add(new ReplayFileInfo
            {
                FileName = fileName,
                DisplayName = fileName,
                PlayerNumber = playerNumber,
                RecordTime = recordTime,
                SortOrder = recordTime.Ticks,
                IsServerHistoryFile = false
            });
            return;
        }

        if (!TryParseHistoryFramesFileName(fileName, out playerNumber, out long timestamp))
        {
            return;
        }

        replayFiles.Add(new ReplayFileInfo
        {
            FileName = fileName,
            DisplayName = BuildHistoryFramesDisplayName(playerNumber, timestamp),
            PlayerNumber = playerNumber,
            RecordTime = DateTime.MinValue,
            SortOrder = BuildHistoryFramesSortOrder(timestamp),
            IsServerHistoryFile = true
        });
    }

    /// <summary>
    /// 尝试解析本地回放文件名中的人数和录制时间。
    /// </summary>
    /// <param name="fileName">回放文件名。</param>
    /// <param name="playerNumber">解析出的玩家人数。</param>
    /// <param name="recordTime">解析出的录制时间。</param>
    /// <returns>文件名符合本地回放格式时返回 true，否则返回 false。</returns>
    private bool TryParseReplayFileName(string fileName, out int playerNumber, out DateTime recordTime)
    {
        playerNumber = 1;
        recordTime = DateTime.MinValue;
        if (string.IsNullOrEmpty(fileName))
        {
            return false;
        }

        if (!fileName.StartsWith(ReplayFilePrefix, StringComparison.Ordinal) ||
            !fileName.EndsWith(ReplayFileSuffix, StringComparison.Ordinal))
        {
            return false;
        }

        string contentText = fileName.Substring(ReplayFilePrefix.Length, fileName.Length - ReplayFilePrefix.Length - ReplayFileSuffix.Length);
        int splitIndex = contentText.IndexOf('_');
        if (splitIndex <= 0 || splitIndex >= contentText.Length - 1)
        {
            return false;
        }

        string playerNumberText = contentText.Substring(0, splitIndex);
        if (!int.TryParse(playerNumberText, out playerNumber) || (playerNumber != 1 && playerNumber != 2))
        {
            return false;
        }

        string dateText = contentText.Substring(splitIndex + 1);
        return DateTime.TryParseExact(dateText, ReplayFileTimeFormat, null, DateTimeStyles.None, out recordTime);
    }

    /// <summary>
    /// 尝试解析服务器历史帧占位文件名中的人数和时间戳。
    /// </summary>
    /// <param name="fileName">服务器历史帧文件名。</param>
    /// <param name="playerNumber">解析出的玩家人数。</param>
    /// <param name="timestamp">解析出的服务器时间戳。</param>
    /// <returns>文件名符合服务器历史帧格式时返回 true，否则返回 false。</returns>
    private bool TryParseHistoryFramesFileName(string fileName, out int playerNumber, out long timestamp)
    {
        playerNumber = 1;
        timestamp = 0L;
        if (string.IsNullOrEmpty(fileName))
        {
            return false;
        }

        if (!fileName.StartsWith(HistoryFramesFilePrefix, StringComparison.Ordinal) ||
            !fileName.EndsWith(HistoryFramesFileSuffix, StringComparison.Ordinal))
        {
            return false;
        }

        string contentText = fileName.Substring(HistoryFramesFilePrefix.Length, fileName.Length - HistoryFramesFilePrefix.Length - HistoryFramesFileSuffix.Length);
        string[] segments = contentText.Split('_');
        if (segments.Length < 3)
        {
            return false;
        }

        if (!int.TryParse(segments[0], out playerNumber) || playerNumber <= 0)
        {
            playerNumber = 1;
        }

        return long.TryParse(segments[1], out timestamp);
    }

    /// <summary>
    /// 将服务器历史帧文件名中的毫秒时间戳转换为可与本地录制时间混排的排序值。
    /// </summary>
    /// <param name="timestamp">服务器历史帧文件名中的毫秒时间戳。</param>
    /// <returns>转换后的 DateTime ticks；时间戳异常时返回原始时间戳。</returns>
    private long BuildHistoryFramesSortOrder(long timestamp)
    {
        try
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime.Ticks;
        }
        catch (ArgumentOutOfRangeException)
        {
            return timestamp;
        }
    }

    /// <summary>
    /// 根据服务器历史帧文件信息生成短显示名，格式为 s_人数_日期时间秒。
    /// </summary>
    /// <param name="playerNumber">历史帧文件名中解析出的玩家人数。</param>
    /// <param name="timestamp">历史帧文件名中解析出的 Unix 毫秒时间戳。</param>
    /// <returns>用于列表展示的短格式名称。</returns>
    private string BuildHistoryFramesDisplayName(int playerNumber, long timestamp)
    {
        try
        {
            string timeText = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).LocalDateTime.ToString(ReplayFileTimeFormat, CultureInfo.InvariantCulture);
            return $"s_{playerNumber}_{timeText}";
        }
        catch (ArgumentOutOfRangeException)
        {
            return $"s_{playerNumber}_{timestamp}";
        }
    }

    /// <summary>
    /// 创建回放文件列表项，并根据文件类型绑定本地回放或服务器历史帧回放点击事件。
    /// </summary>
    /// <param name="displayName">列表中显示的文件名。</param>
    /// <param name="fileName">实际用于读取或请求的原始文件名。</param>
    /// <param name="canReplay">当前列表项是否可点击回放。</param>
    /// <param name="playerNumber">文件名中记录的玩家人数。</param>
    /// <param name="isServerHistoryFile">是否为服务器历史帧文件。</param>
    private void CreateListItem(string displayName, string fileName, bool canReplay, int playerNumber = 1, bool isServerHistoryFile = false)
    {
        GameObject itemObject = UnityEngine.Object.Instantiate(itemTemplate, listContent);
        itemObject.name = $"ReplayItem_{displayName}";
        itemObject.SetActive(true);

        TMP_Text fileNameText = itemObject.transform.Find("FileName")?.GetComponent<TMP_Text>();
        if (fileNameText != null)
        {
            fileNameText.text = displayName;
        }

        Button replayBtn = itemObject.transform.Find("ReplayBtn")?.GetComponent<Button>();
        if (replayBtn != null)
        {
            if (canReplay)
            {
                replayBtn.interactable = true;
                replayBtn.onClick.AddListener(() => OnReplayItemClick(fileName, playerNumber, isServerHistoryFile));
                itemButtons.Add(replayBtn);
            }
            else
            {
                replayBtn.gameObject.SetActive(false);
            }
        }

        itemInstances.Add(itemObject);
    }

    /// <summary>
    /// 刷新滚动区域内容高度，确保动态生成的列表项完整显示。
    /// </summary>
    private void RefreshContentLayout()
    {
        RectTransform contentRect = listContent as RectTransform;
        RectTransform templateRect = itemTemplate.GetComponent<RectTransform>();
        if (contentRect == null || templateRect == null)
        {
            return;
        }

        int visibleItemCount = itemInstances.Count;
        float itemHeight = templateRect.rect.height;
        if (itemHeight <= 0f)
        {
            itemHeight = templateRect.sizeDelta.y;
        }

        float spacing = 0f;
        int paddingTop = 0;
        int paddingBottom = 0;
        VerticalLayoutGroup layoutGroup = contentRect.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup != null)
        {
            spacing = layoutGroup.spacing;
            paddingTop = layoutGroup.padding.top;
            paddingBottom = layoutGroup.padding.bottom;
        }

        float totalHeight = visibleItemCount * itemHeight + Mathf.Max(0, visibleItemCount - 1) * spacing + paddingTop + paddingBottom;
        if (viewportRect != null)
        {
            totalHeight = Mathf.Max(totalHeight, viewportRect.rect.height);
        }

        Vector2 sizeDelta = contentRect.sizeDelta;
        contentRect.sizeDelta = new Vector2(sizeDelta.x, totalHeight);
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        ScrollToTop();
    }

    /// <summary>
    /// 将回放列表滚动到顶部，保证刷新后优先展示最新文件。
    /// </summary>
    private void ScrollToTop()
    {
        if (listScrollRect != null)
        {
            listScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    /// <summary>
    /// 清理动态创建的列表项和按钮事件，避免重复刷新后残留旧节点。
    /// </summary>
    private void ClearList()
    {
        for (int i = 0; i < itemButtons.Count; i++)
        {
            Button replayBtn = itemButtons[i];
            if (replayBtn != null)
            {
                replayBtn.onClick.RemoveAllListeners();
            }
        }
        itemButtons.Clear();

        for (int i = 0; i < itemInstances.Count; i++)
        {
            GameObject itemObject = itemInstances[i];
            if (itemObject != null)
            {
                UnityEngine.Object.Destroy(itemObject);
            }
        }
        itemInstances.Clear();

        if (itemTemplate != null)
        {
            itemTemplate.SetActive(false);
        }
    }

    /// <summary>
    /// 处理根节点点击关闭行为，播放按钮音效并隐藏回放子面板。
    /// </summary>
    private void OnRootClick()
    {
        UISound.Instance.PlayClick();
        Hide();
    }

    /// <summary>
    /// 处理回放列表项点击，根据文件类型启动本地回放或请求服务器历史帧。
    /// </summary>
    /// <param name="replayFileName">被点击的回放文件名。</param>
    /// <param name="playerNumber">文件名中记录的玩家人数。</param>
    /// <param name="isServerHistoryFile">是否为服务器历史帧文件。</param>
    private void OnReplayItemClick(string replayFileName, int playerNumber, bool isServerHistoryFile)
    {
        UISound.Instance.PlayClick();
        if (isServerHistoryFile)
        {
            // 检查本地文件是否存在且非空
            if (IsFileEmpty(replayFileName))
            {
                // 文件为空或不存在，请求服务器获取数据
                RequestHistoryFramesReplay(replayFileName);
            }
            else
            {
                // 文件存在且非空，直接读取本地文件进行回放
                zUDebug.Log($"[ReplaySubPanel] 服务器历史帧文件已缓存，直接读取本地文件: {replayFileName}");
                StartReplayFromFile(replayFileName, playerNumber);
            }
            return;
        }

        StartReplayFromFile(replayFileName, playerNumber);
    }

    /// <summary>
    /// 从指定回放文件加载命令记录，并创建回放模式的战斗世界。
    /// </summary>
    /// <param name="replayFileName">回放文件名。</param>
    /// <param name="playerNumber">回放文件名中记录的玩家人数。</param>
    private void StartReplayFromFile(string replayFileName, int playerNumber)
    {
        if (string.IsNullOrEmpty(replayFileName) || !CommandReader.ReplayFileExistsByName(replayFileName))
        {
            zUDebug.LogWarning($"[ReplaySubPanel] 回放文件不存在: {replayFileName}");
            return;
        }

        zUDebug.Log($"[ReplaySubPanel] 正在读取命令记录文件: {replayFileName}");
        Utils.CommandReader commandReader = new();
        commandReader.LoadFromFile(replayFileName);

        zUDebug.Log($"[ReplaySubPanel] 总共加载了 {commandReader.FrameInputs.Count} 条 frameInput 记录。");
        var allFrames = commandReader.GetAllFrames();
        zUDebug.Log($"[ReplaySubPanel] 共有 {allFrames.Count} 个不同的帧号。");

        StartReplayFromFrameInputs(replayFileName, playerNumber, commandReader.FrameInputs);
    }

    /// <summary>
    /// 通过临时 WebSocket 连接请求服务器历史帧文件，成功后使用返回的帧数据启动回放。
    /// </summary>
    /// <param name="historyFileName">服务器历史帧文件名。</param>
    private void RequestHistoryFramesReplay(string historyFileName)
    {
        if (string.IsNullOrEmpty(historyFileName))
        {
            zUDebug.LogWarning("[ReplaySubPanel] 历史帧文件名为空，无法请求服务器历史帧。");
            return;
        }

        bool useLocalServer = getUseLocalServer?.Invoke() ?? false;
        zUDebug.LogInfo($"[ReplaySubPanel] 请求服务器历史帧: {historyFileName}, useLocalServer={useLocalServer}");
        NetworkManager.Instance.RequestHistoryFrames(historyFileName, useLocalServer, OnHistoryFramesReplayLoaded, OnHistoryFramesReplayFailed);
    }

    /// <summary>
    /// 处理服务器历史帧请求成功结果，打印元数据并转换 frameSync 消息为回放命令。
    /// </summary>
    /// <param name="historyData">服务器返回的历史帧完整数据。</param>
    private void OnHistoryFramesReplayLoaded(HistoryFramesReplayData historyData)
    {
        HistoryFramesStartData startData = historyData.StartData;
        int playerCount = startData.PlayerCount > 0 ? startData.PlayerCount : 1;
        int playersCount = startData.Players?.Count ?? 0;
        zUDebug.LogInfo($"[ReplaySubPanel] historyFramesStart: fileName={startData.FileName}, roomId={startData.RoomId}, playerCount={playerCount}, players={playersCount}");

        List<FrameInput> frameInputs = BuildFrameInputsFromHistoryFrames(historyData.Frames);
        if (frameInputs.Count == 0)
        {
            zUDebug.LogInfo($"[ReplaySubPanel] 历史帧没有命令数据，将启动空回放世界: {startData.FileName}");
        }

        // 将服务器历史帧写入本地文件缓存
        CommandRecorder.WriteFrameInputsToFile(startData.FileName, frameInputs);

        StartReplayFromFrameInputs(startData.FileName, playerCount, frameInputs);
    }

    /// <summary>
    /// 处理服务器历史帧请求失败结果，打印失败文件名和原因。
    /// </summary>
    /// <param name="failedData">服务器返回的历史帧失败数据。</param>
    private void OnHistoryFramesReplayFailed(HistoryFramesFailedData failedData)
    {
        zUDebug.LogError($"[ReplaySubPanel] 获取历史帧失败: fileName={failedData.FileName}, reason={failedData.Reason}");
    }

    /// <summary>
    /// 将服务器历史帧中的 frameSync JSON 文本转换为本地回放可提交的帧输入列表。
    /// </summary>
    /// <param name="historyFrames">服务器返回的历史帧条目列表。</param>
    /// <returns>转换后的帧输入列表。</returns>
    private List<FrameInput> BuildFrameInputsFromHistoryFrames(List<HistoryFrameEntryData> historyFrames)
    {
        List<FrameInput> frameInputs = new();
        if (historyFrames == null)
        {
            return frameInputs;
        }

        for (int i = 0; i < historyFrames.Count; i++)
        {
            HistoryFrameEntryData historyFrame = historyFrames[i];
            if (string.IsNullOrEmpty(historyFrame.Message))
            {
                continue;
            }

            try
            {
                JObject frameSyncMessage = JObject.Parse(historyFrame.Message);
                int frame = historyFrame.Frame > 0 ? historyFrame.Frame : frameSyncMessage["frame"]?.ToObject<int>() ?? 0;
                List<ICommand> commands = FrameSyncMessageParser.ParseCommands(frameSyncMessage, "[ReplaySubPanel]");
                FrameInput frameInput = new()
                {
                    type = "frameInput",
                    frame = frame,
                    data = new List<MyCommand>()
                };

                for (int j = 0; j < commands.Count; j++)
                {
                    ICommand command = commands[j];
                    command.ExecuteFrame = frame;
                    frameInput.data.Add(new MyCommand
                    {
                        commandType = CommandMapper.GetCommandType(command.GetType()),
                        command = command
                    });
                }

                frameInputs.Add(frameInput);
            }
            catch (Exception ex)
            {
                zUDebug.LogError($"[ReplaySubPanel] 解析历史帧 frameSync 失败: frame={historyFrame.Frame}, error={ex.Message}");
            }
        }

        return frameInputs;
    }

    /// <summary>
    /// 根据帧输入记录创建回放模式战斗世界，并把所有命令提交到回放命令队列。
    /// </summary>
    /// <param name="replaySourceName">回放来源文件名，用于日志输出。</param>
    /// <param name="playerNumber">回放玩家人数。</param>
    /// <param name="frameInputs">需要提交到回放世界的帧输入记录。</param>
    private void StartReplayFromFrameInputs(string replaySourceName, int playerNumber, List<FrameInput> frameInputs)
    {
        if (playerNumber <= 0)
        {
            playerNumber = 1;
        }

        frameInputs ??= new List<FrameInput>();

        // 重置重置全局随机状态快照
        zRandom.ResetGlobalDeterminismStateRaw(0L);

        Ra2Demo ra2Demo = UnityEngine.Object.FindObjectOfType<Ra2Demo>();
        if (ra2Demo == null)
        {
            zUDebug.LogError("[ReplaySubPanel] 未找到 Ra2Demo 实例，无法启动回放。");
            return;
        }

        ra2Demo.Mode = GameMode.Replay;
        ra2Demo.SetBattleGame(new BattleGame(ra2Demo.Mode, 20, 0, playerNumber));
        ra2Demo.ResetReplaySpeed();
        ra2Demo.GetBattleGame().Init();
        ra2Demo.InitializeUnityView();

        GlobalInfoComponent globalInfoComponent = new(1);
        ra2Demo.GetBattleGame().World.ComponentManager.AddGlobalComponent(globalInfoComponent);
        
        for (int i = 0; i < frameInputs.Count; i++)
        {
            FrameInput frameInput = frameInputs[i];
            if (frameInput?.data == null)
            {
                continue;
            }

            for (int j = 0; j < frameInput.data.Count; j++)
            {
                ICommand cmd = frameInput.data[j].command;
                if (cmd.ExecuteFrame < 0)
                {
                    cmd.ExecuteFrame = frameInput.frame;
                }
                else if (cmd.ExecuteFrame != frameInput.frame)
                {
                    zUDebug.LogWarning($"[ReplaySubPanel] 回放命令帧不一致，保留命令内帧: {cmd.ExecuteFrame}, 记录帧: {frameInput.frame}, 命令: {cmd.GetType().Name}");
                }

                ra2Demo.GetBattleGame().World.CommandManager.SubmitCommand(cmd);
            }
        }

        zUDebug.Log($"[ReplaySubPanel] 根据回放文件人数创建世界。PlayerNumber:{playerNumber}");
        ra2Demo.GetBattleGame().CreateWorldByConfig();

        StartDeterminismProbe(ra2Demo.GetBattleGame(), "replay");
        Frame.DispatchEvent(new ReplayGameStartEvent());

        Hide();
    }

    /// <summary>
    /// 启动回放确定性探针，把回放过程中的帧摘要写入持久化目录。
    /// </summary>
    /// <param name="battleGame">需要记录确定性摘要的战斗实例。</param>
    /// <param name="tag">探针文件名中的来源标签。</param>
    private void StartDeterminismProbe(BattleGame battleGame, string tag)
    {
        var probe = battleGame?.World?.DeterminismProbe;
        if (probe == null)
        {
            return;
        }

        string fileName = $"determinism_probe_{tag}_{DateTime.Now:yyyyMMdd_HHmmss}.jsonl";
        string path = Path.Combine(Application.persistentDataPath, fileName);
        probe.Start(path, truncate: true);
        zUDebug.Log($"[ReplaySubPanel] DeterminismProbe 已启动: {path}");
    }

    /// <summary>
    /// 检查指定文件是否为空或不存在。
    /// </summary>
    /// <param name="fileName">需要检查的文件名。</param>
    /// <returns>文件不存在或为空时返回 true，否则返回 false。</returns>
    private bool IsFileEmpty(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return true;
        }

        string filePath = Path.Combine(Application.persistentDataPath, fileName);
#if UNITY_WEBGL || WEIXINMINIGAME
        WeChatWASM.WXFileSystemManager fs = WeChatWASM.WX.GetFileSystemManager();
        if (!fs.AccessSync(filePath).Equals("access:ok"))
        {
            return true;
        }

        string content = fs.ReadFileSync(filePath, "utf-8");
        return string.IsNullOrEmpty(content);
#else
        if (!File.Exists(filePath))
        {
            return true;
        }

        string content = File.ReadAllText(filePath);
        return string.IsNullOrEmpty(content);
#endif
    }
}
