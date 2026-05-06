using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using ZLockstep.Simulation;
using ZLockstep.Simulation.ECS;
using ZLockstep.Simulation.ECS.Components;
using ZLockstep.Sync.Command;
using zUnity;

namespace ZLockstep.Sync.Determinism
{
    /// <summary>
    /// 回放对比时的不一致类型，用于快速定位命令、随机数或状态层面的偏差来源。
    /// </summary>
    public enum ReplayMismatchType
    {
        None = 0,
        CommandMismatch = 1,
        RandomMismatch = 2,
        StateMismatch = 3
    }

    /// <summary>
    /// 单帧确定性采样摘要，记录命令签名、随机状态和关键状态哈希，用于跨运行结果对比。
    /// </summary>
    public struct FrameDigest
    {
        /// <summary>
        /// 当前采样对应的逻辑帧号。
        /// </summary>
        public int tick;
        /// <summary>
        /// 当前帧处于激活状态的实体数量。
        /// </summary>
        public int activeEntityCount;

        /// <summary>
        /// 当前帧已执行命令数量。
        /// </summary>
        public int commandCount;
        /// <summary>
        /// 当前帧命令类型列表，按执行顺序记录。
        /// </summary>
        public List<int> commandTypes;
        /// <summary>
        /// 当前帧命令类型签名文本，用于快速可读比对。
        /// </summary>
        public string commandTypeSignature;
        /// <summary>
        /// 当前帧命令序列的聚合哈希值。
        /// </summary>
        public ulong commandHash;

        /// <summary>
        /// 当前帧随机数状态原始值，用于随机序列一致性校验。
        /// </summary>
        public long randomStateRaw;

        /// <summary>
        /// 当前帧关键状态聚合哈希值。
        /// </summary>
        public ulong stateHash;
        /// <summary>
        /// 当前帧各阵营经济快照文本集合。
        /// </summary>
        public List<string> campEconomySnapshots;
        /// <summary>
        /// 当前帧关键状态样本文本集合。
        /// </summary>
        public List<string> stateSamples;
    }

    /// <summary>
    /// 确定性采样器接口，负责从指定世界与已执行命令中提取可回放对比的帧摘要。
    /// </summary>
    public interface IDeterminismSampler
    {
        /// <summary>
        /// 采集当前逻辑帧的摘要信息。
        /// </summary>
        /// <param name="world">当前锁步世界实例。</param>
        /// <param name="executedCommands">本帧已执行的命令集合。</param>
        /// <returns>当前帧的确定性摘要。</returns>
        FrameDigest Capture(zWorld world, IReadOnlyList<ICommand> executedCommands);
    }

    /// <summary>
    /// 回放差异结果对象，包含首个不一致帧、类型、摘要和可读明细。
    /// </summary>
    public class ReplayDiffResult
    {
        /// <summary>
        /// 获取或设置对比结果是否一致。
        /// </summary>
        public bool IsMatch;
        /// <summary>
        /// 获取或设置首个不一致的逻辑帧号，未发生不一致时为 -1。
        /// </summary>
        public int FirstMismatchTick = -1;
        /// <summary>
        /// 获取或设置不一致类型。
        /// </summary>
        public ReplayMismatchType MismatchType = ReplayMismatchType.None;
        /// <summary>
        /// 获取或设置结果摘要文本。
        /// </summary>
        public string Summary;
        /// <summary>
        /// 获取或设置差异明细列表。
        /// </summary>
        public List<string> Details = new List<string>();
    }

    /// <summary>
    /// 确定性探针，负责按采样间隔记录帧摘要到文件，供后续回放一致性校验使用。
    /// </summary>
    public sealed class DeterminismProbe
    {
        private readonly zWorld _world;
        private readonly IDeterminismSampler _sampler;
        private bool _isRecording;

#if UNITY_EDITOR
        /// <summary>
        /// 获取或设置编辑器下启动探针时是否自动启用记录，用于调试时避免手动修改实例默认值。
        /// </summary>
        public static bool EditorAutoEnable { get; set; }
#endif

        /// <summary>
        /// 获取或设置探针是否启用。禁用后即使处于记录状态也不会写入采样结果。
        /// </summary>
        public bool Enabled { get; set; } = false;
        /// <summary>
        /// 获取或设置采样间隔（单位：逻辑帧）。小于等于 0 时会在运行时修正为 1。
        /// </summary>
        public int SampleInterval { get; set; } = 1;
        /// <summary>
        /// 获取当前输出文件路径。
        /// </summary>
        public string OutputFilePath { get; private set; }
        /// <summary>
        /// 获取最近一次成功采样的帧摘要。
        /// </summary>
        public FrameDigest LastDigest { get; private set; }

        /// <summary>
        /// 创建确定性探针实例。
        /// </summary>
        /// <param name="world">要采样的锁步世界。</param>
        /// <param name="sampler">可选自定义采样器，未提供时使用默认采样器。</param>
        public DeterminismProbe(zWorld world, IDeterminismSampler sampler = null)
        {
            _world = world;
            _sampler = sampler ?? new DefaultDeterminismSampler();
        }

        /// <summary>
        /// 开始记录采样数据到文件。
        /// </summary>
        /// <param name="filePath">输出文件路径，空值时自动生成默认路径。</param>
        /// <param name="truncate">是否在开始前清空目标文件。</param>
        public void Start(string filePath = null, bool truncate = true)
        {
#if UNITY_EDITOR
            if (EditorAutoEnable)
            {
                Enabled = true;
            }
#endif

            if (!Enabled)
            {
                _isRecording = true;
                zUDebug.Log("[DeterminismProbe] Start: probe is disabled, no file will be created");
                return;
            }

            OutputFilePath = string.IsNullOrWhiteSpace(filePath)
                ? BuildDefaultPath()
                : filePath;

            string dir = Path.GetDirectoryName(OutputFilePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (truncate)
            {
                File.WriteAllText(OutputFilePath, string.Empty);
            }

            _isRecording = true;
            zUDebug.Log($"[DeterminismProbe] Start: {OutputFilePath}");
        }

        /// <summary>
        /// 停止记录采样数据。
        /// </summary>
        public void Stop()
        {
            _isRecording = false;
            zUDebug.Log("[DeterminismProbe] Stop");
        }

        /// <summary>
        /// 对当前帧执行一次采样并追加写入输出文件。
        /// </summary>
        /// <param name="executedCommands">当前帧已执行命令集合，空值会按空集合处理。</param>
        public void RecordFrame(IReadOnlyList<ICommand> executedCommands)
        {
            if (!_isRecording || !Enabled || _world == null)
            {
                return;
            }

            if (SampleInterval <= 0)
            {
                SampleInterval = 1;
            }

            if ((_world.Tick % SampleInterval) != 0)
            {
                return;
            }

            try
            {
                LastDigest = _sampler.Capture(_world, executedCommands ?? Array.Empty<ICommand>());
                string jsonLine = JsonConvert.SerializeObject(LastDigest);
                File.AppendAllText(OutputFilePath, jsonLine + Environment.NewLine);
            }
            catch (Exception ex)
            {
                zUDebug.LogError($"[DeterminismProbe] Record failed at tick={_world.Tick}: {ex.Message}");
            }
        }

        /// <summary>
        /// 生成默认的探针输出路径，文件名包含当前模式与本地时间戳，便于区分不同采样批次。
        /// </summary>
        /// <returns>默认的 jsonl 输出文件绝对路径。</returns>
        private string BuildDefaultPath()
        {
            string mode = _world?.GameInstance?.Mode.ToString() ?? "Unknown";
            string fileName = $"determinism_probe_{mode}_{DateTime.Now:yyyyMMdd_HHmmss}.jsonl";
            return Path.Combine(Application.persistentDataPath, fileName);
        }
    }

    /// <summary>
    /// 回放差异分析工具，提供摘要文件加载与逐帧对比能力。
    /// </summary>
    public static class ReplayDiffTool
    {
        /// <summary>
        /// 读取并对比两个摘要文件。
        /// </summary>
        /// <param name="baselinePath">基线摘要文件路径。</param>
        /// <param name="replayPath">回放摘要文件路径。</param>
        /// <returns>对比结果。</returns>
        public static ReplayDiffResult CompareFiles(string baselinePath, string replayPath)
        {
            var baseline = LoadDigests(baselinePath);
            var replay = LoadDigests(replayPath);
            return Compare(baseline, replay);
        }

        /// <summary>
        /// 对比两组帧摘要并返回首个不一致结果。
        /// </summary>
        /// <param name="baseline">基线帧摘要集合。</param>
        /// <param name="replay">回放帧摘要集合。</param>
        /// <returns>对比结果。</returns>
        public static ReplayDiffResult Compare(IReadOnlyList<FrameDigest> baseline, IReadOnlyList<FrameDigest> replay)
        {
            var result = new ReplayDiffResult { IsMatch = true, Summary = "No mismatch." };

            var baselineByTick = ToTickMap(baseline);
            var replayByTick = ToTickMap(replay);
            var ticks = baselineByTick.Keys.Union(replayByTick.Keys).OrderBy(x => x);

            foreach (int tick in ticks)
            {
                if (!baselineByTick.TryGetValue(tick, out FrameDigest left))
                {
                    return BuildMissingTickResult(tick, "baseline");
                }

                if (!replayByTick.TryGetValue(tick, out FrameDigest right))
                {
                    return BuildMissingTickResult(tick, "replay");
                }

                if (left.commandHash != right.commandHash || left.commandCount != right.commandCount)
                {
                    return BuildCommandMismatchResult(tick, left, right);
                }

                if (left.randomStateRaw != right.randomStateRaw)
                {
                    return BuildRandomMismatchResult(tick, left, right);
                }

                if (left.stateHash != right.stateHash || left.activeEntityCount != right.activeEntityCount)
                {
                    return BuildStateMismatchResult(tick, left, right);
                }
            }

            return result;
        }

        /// <summary>
        /// 从 jsonl 文件中加载帧摘要集合。
        /// </summary>
        /// <param name="filePath">摘要文件路径。</param>
        /// <returns>解析得到的帧摘要列表；文件不存在时返回空列表。</returns>
        public static List<FrameDigest> LoadDigests(string filePath)
        {
            var list = new List<FrameDigest>();
            if (!File.Exists(filePath))
            {
                return list;
            }

            string[] lines = File.ReadAllLines(filePath);
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                try
                {
                    list.Add(JsonConvert.DeserializeObject<FrameDigest>(line));
                }
                catch (Exception ex)
                {
                    zUDebug.LogWarning($"[ReplayDiffTool] skip invalid line: {ex.Message}");
                }
            }

            return list;
        }

        /// <summary>
        /// 将帧摘要集合按帧号建立索引，便于以 O(1) 方式定位指定逻辑帧数据。
        /// </summary>
        /// <param name="digests">待索引的帧摘要集合。</param>
        /// <returns>键为 tick、值为帧摘要的映射表。</returns>
        private static Dictionary<int, FrameDigest> ToTickMap(IReadOnlyList<FrameDigest> digests)
        {
            var map = new Dictionary<int, FrameDigest>();
            if (digests == null)
            {
                return map;
            }

            for (int i = 0; i < digests.Count; i++)
            {
                map[digests[i].tick] = digests[i];
            }

            return map;
        }

        /// <summary>
        /// 构造缺帧结果，用于描述基线或回放在指定帧号缺失数据的情况。
        /// </summary>
        /// <param name="tick">发生缺失的逻辑帧号。</param>
        /// <param name="missingSide">缺失侧标识（baseline 或 replay）。</param>
        /// <returns>包含缺帧信息的差异结果对象。</returns>
        private static ReplayDiffResult BuildMissingTickResult(int tick, string missingSide)
        {
            return new ReplayDiffResult
            {
                IsMatch = false,
                FirstMismatchTick = tick,
                MismatchType = ReplayMismatchType.StateMismatch,
                Summary = $"Tick {tick} missing in {missingSide}.",
                Details = new List<string> { $"missing={missingSide}", $"tick={tick}" }
            };
        }

        /// <summary>
        /// 构造命令不一致结果，输出命令数量与命令类型签名差异。
        /// </summary>
        /// <param name="tick">发生不一致的逻辑帧号。</param>
        /// <param name="left">基线帧摘要。</param>
        /// <param name="right">回放帧摘要。</param>
        /// <returns>包含命令差异明细的结果对象。</returns>
        private static ReplayDiffResult BuildCommandMismatchResult(int tick, FrameDigest left, FrameDigest right)
        {
            return new ReplayDiffResult
            {
                IsMatch = false,
                FirstMismatchTick = tick,
                MismatchType = ReplayMismatchType.CommandMismatch,
                Summary = $"Tick {tick} command mismatch.",
                Details = new List<string>
                {
                    $"baseline.commandCount={left.commandCount}",
                    $"replay.commandCount={right.commandCount}",
                    $"baseline.commandTypeSignature={left.commandTypeSignature}",
                    $"replay.commandTypeSignature={right.commandTypeSignature}"
                }
            };
        }

        /// <summary>
        /// 构造随机数状态不一致结果，定位同帧随机源状态偏差。
        /// </summary>
        /// <param name="tick">发生不一致的逻辑帧号。</param>
        /// <param name="left">基线帧摘要。</param>
        /// <param name="right">回放帧摘要。</param>
        /// <returns>包含随机状态差异的结果对象。</returns>
        private static ReplayDiffResult BuildRandomMismatchResult(int tick, FrameDigest left, FrameDigest right)
        {
            return new ReplayDiffResult
            {
                IsMatch = false,
                FirstMismatchTick = tick,
                MismatchType = ReplayMismatchType.RandomMismatch,
                Summary = $"Tick {tick} random state mismatch.",
                Details = new List<string>
                {
                    $"baseline.randomStateRaw={left.randomStateRaw}",
                    $"replay.randomStateRaw={right.randomStateRaw}"
                }
            };
        }

        /// <summary>
        /// 构造状态不一致结果，附带实体计数、状态哈希及关键字段样本差异。
        /// </summary>
        /// <param name="tick">发生不一致的逻辑帧号。</param>
        /// <param name="left">基线帧摘要。</param>
        /// <param name="right">回放帧摘要。</param>
        /// <returns>包含状态差异明细的结果对象。</returns>
        private static ReplayDiffResult BuildStateMismatchResult(int tick, FrameDigest left, FrameDigest right)
        {
            var details = new List<string>
            {
                $"baseline.activeEntityCount={left.activeEntityCount}",
                $"replay.activeEntityCount={right.activeEntityCount}",
                $"baseline.stateHash={left.stateHash}",
                $"replay.stateHash={right.stateHash}"
            };

            var leftMap = ToSampleMap(left.stateSamples);
            var rightMap = ToSampleMap(right.stateSamples);
            int appended = 0;
            foreach (string key in leftMap.Keys.Union(rightMap.Keys).OrderBy(x => x))
            {
                leftMap.TryGetValue(key, out string leftValue);
                rightMap.TryGetValue(key, out string rightValue);
                if (!string.Equals(leftValue, rightValue, StringComparison.Ordinal))
                {
                    details.Add($"{key}: baseline={leftValue ?? "<null>"} replay={rightValue ?? "<null>"}");
                    appended++;
                    if (appended >= 20)
                    {
                        break;
                    }
                }
            }

            return new ReplayDiffResult
            {
                IsMatch = false,
                FirstMismatchTick = tick,
                MismatchType = ReplayMismatchType.StateMismatch,
                Summary = $"Tick {tick} state mismatch.",
                Details = details
            };
        }

        /// <summary>
        /// 将状态样本行解析为键值映射，格式要求为 key=value。
        /// </summary>
        /// <param name="samples">待解析的样本字符串列表。</param>
        /// <returns>样本键值映射表。</returns>
        private static Dictionary<string, string> ToSampleMap(List<string> samples)
        {
            var map = new Dictionary<string, string>();
            if (samples == null)
            {
                return map;
            }

            foreach (string sample in samples)
            {
                if (string.IsNullOrEmpty(sample))
                {
                    continue;
                }

                int idx = sample.IndexOf('=');
                if (idx <= 0 || idx >= sample.Length - 1)
                {
                    continue;
                }

                string key = sample.Substring(0, idx);
                string value = sample.Substring(idx + 1);
                map[key] = value;
            }

            return map;
        }
    }

    /// <summary>
    /// 默认确定性采样器，基于关键组件字段生成稳定摘要与哈希用于一致性校验。
    /// </summary>
    public sealed class DefaultDeterminismSampler : IDeterminismSampler
    {
        private const ulong FnvOffsetBasis = 14695981039346656037UL;
        private const ulong FnvPrime = 1099511628211UL;

        /// <summary>
        /// 采集当前世界的命令、随机数与关键组件状态，生成帧级摘要。
        /// </summary>
        /// <param name="world">当前锁步世界实例。</param>
        /// <param name="executedCommands">本帧已执行的命令集合。</param>
        /// <returns>当前帧的确定性摘要。</returns>
        public FrameDigest Capture(zWorld world, IReadOnlyList<ICommand> executedCommands)
        {
            var digest = new FrameDigest
            {
                tick = world.Tick,
                activeEntityCount = world.EntityManager?.ActiveEntityCount ?? 0,
                commandTypes = new List<int>(),
                campEconomySnapshots = new List<string>(),
                stateSamples = new List<string>()
            };

            CaptureCommands(digest.commandTypes, executedCommands);
            digest.commandCount = digest.commandTypes.Count;
            digest.commandTypeSignature = digest.commandCount == 0
                ? string.Empty
                : string.Join(",", digest.commandTypes);
            digest.commandHash = HashCommandTypes(digest.commandTypes);

            digest.randomStateRaw = zRandom.GetGlobalDeterminismStateRaw();
            digest.stateHash = CaptureStateHash(world, digest);
            return digest;
        }

        /// <summary>
        /// 提取并排序命令类型编号，生成稳定的命令序列输入。
        /// </summary>
        /// <param name="commandTypes">命令类型输出列表。</param>
        /// <param name="executedCommands">本帧已执行命令集合。</param>
        private static void CaptureCommands(List<int> commandTypes, IReadOnlyList<ICommand> executedCommands)
        {
            if (executedCommands == null)
            {
                return;
            }

            foreach (ICommand command in executedCommands)
            {
                if (command == null)
                {
                    continue;
                }

                commandTypes.Add(CommandMapper.GetCommandType(command.GetType()));
            }

            commandTypes.Sort();
        }

        /// <summary>
        /// 基于命令类型序列计算 FNV 哈希，作为命令层一致性指纹。
        /// </summary>
        /// <param name="commandTypes">已排序的命令类型序列。</param>
        /// <returns>命令类型哈希值。</returns>
        private static ulong HashCommandTypes(List<int> commandTypes)
        {
            ulong hash = FnvOffsetBasis;
            for (int i = 0; i < commandTypes.Count; i++)
            {
                AddInt(ref hash, commandTypes[i]);
            }

            return hash;
        }

        /// <summary>
        /// 汇总随机状态与关键组件数据，计算整帧状态哈希。
        /// </summary>
        /// <param name="world">当前锁步世界实例。</param>
        /// <param name="digest">当前帧摘要容器。</param>
        /// <returns>状态哈希值。</returns>
        private static ulong CaptureStateHash(zWorld world, FrameDigest digest)
        {
            ulong hash = FnvOffsetBasis;
            AddInt(ref hash, digest.tick);
            AddInt(ref hash, digest.activeEntityCount);
            AddLong(ref hash, digest.randomStateRaw);

            ComponentManager cm = world.ComponentManager;
            if (cm == null)
            {
                return hash;
            }

            CaptureEconomy(cm, ref hash, digest);
            CaptureEntityState(cm, ref hash, digest);
            return hash;
        }

        /// <summary>
        /// 采集经济相关组件字段并写入状态样本与哈希。
        /// </summary>
        /// <param name="cm">组件管理器。</param>
        /// <param name="hash">状态哈希累加器。</param>
        /// <param name="digest">当前帧摘要容器。</param>
        private static void CaptureEconomy(ComponentManager cm, ref ulong hash, FrameDigest digest)
        {
            foreach (int entityId in cm.GetAllEntityIdsWith<EconomyComponent>())
            {
                Entity entity = new Entity(entityId);
                EconomyComponent economy = cm.GetComponent<EconomyComponent>(entity);
                int campId = cm.HasComponent<CampComponent>(entity)
                    ? cm.GetComponent<CampComponent>(entity).CampId
                    : -1;

                string snapshot = $"Camp:{campId},Money:{economy.Money},Power:{economy.Power},GMPower:{economy.GMPower}";
                digest.campEconomySnapshots.Add(snapshot);

                AddInt(ref hash, campId);
                AddInt(ref hash, economy.Money);
                AddInt(ref hash, economy.Power);
                AddInt(ref hash, economy.GMPower);
            }
        }

        /// <summary>
        /// 采集实体的核心组件状态并写入样本，作为回放差异定位依据。
        /// </summary>
        /// <param name="cm">组件管理器。</param>
        /// <param name="hash">状态哈希累加器。</param>
        /// <param name="digest">当前帧摘要容器。</param>
        private static void CaptureEntityState(ComponentManager cm, ref ulong hash, FrameDigest digest)
        {
            foreach (int entityId in cm.GetAllEntityIdsWith<TransformComponent>())
            {
                Entity entity = new Entity(entityId);
                AddInt(ref hash, entityId);

                TransformComponent transform = cm.GetComponent<TransformComponent>(entity);
                AddTransform(ref hash, digest.stateSamples, entityId, transform);

                if (cm.HasComponent<HealthComponent>(entity))
                {
                    HealthComponent health = cm.GetComponent<HealthComponent>(entity);
                    AddHealth(ref hash, digest.stateSamples, entityId, health);
                }

                if (cm.HasComponent<MoveCommandComponent>(entity))
                {
                    MoveCommandComponent move = cm.GetComponent<MoveCommandComponent>(entity);
                    AddMove(ref hash, digest.stateSamples, entityId, move);
                }

                if (cm.HasComponent<AttackComponent>(entity))
                {
                    AttackComponent attack = cm.GetComponent<AttackComponent>(entity);
                    AddAttack(ref hash, digest.stateSamples, entityId, attack);
                }

                if (cm.HasComponent<ProduceComponent>(entity))
                {
                    ProduceComponent produce = cm.GetComponent<ProduceComponent>(entity);
                    AddProduce(ref hash, digest.stateSamples, entityId, produce);
                }
            }
        }

        /// <summary>
        /// 将位姿组件字段追加到哈希与样本列表。
        /// </summary>
        /// <param name="hash">状态哈希累加器。</param>
        /// <param name="samples">状态样本输出列表。</param>
        /// <param name="entityId">实体编号。</param>
        /// <param name="value">位姿组件值。</param>
        private static void AddTransform(ref ulong hash, List<string> samples, int entityId, TransformComponent value)
        {
            AddVector3(ref hash, samples, entityId, "Transform.Position", value.Position);
            AddQuaternion(ref hash, samples, entityId, "Transform.Rotation", value.Rotation);
            AddVector3(ref hash, samples, entityId, "Transform.FuturePosition", value.FuturePosition);
            AddQuaternion(ref hash, samples, entityId, "Transform.FutureRotation", value.FutureRotation);
            AddInt(ref hash, value.FutureTick);
            samples.Add($"E:{entityId}:Transform.FutureTick={value.FutureTick}");
        }

        /// <summary>
        /// 将生命组件字段追加到哈希与样本列表。
        /// </summary>
        /// <param name="hash">状态哈希累加器。</param>
        /// <param name="samples">状态样本输出列表。</param>
        /// <param name="entityId">实体编号。</param>
        /// <param name="value">生命组件值。</param>
        private static void AddHealth(ref ulong hash, List<string> samples, int entityId, HealthComponent value)
        {
            AddLong(ref hash, value.MaxHealth.value);
            AddLong(ref hash, value.CurrentHealth.value);
            AddInt(ref hash, value.LastDamageTick);
            samples.Add($"E:{entityId}:Health.MaxHealth={value.MaxHealth.value}");
            samples.Add($"E:{entityId}:Health.CurrentHealth={value.CurrentHealth.value}");
            samples.Add($"E:{entityId}:Health.LastDamageTick={value.LastDamageTick}");
        }

        /// <summary>
        /// 将移动指令组件字段追加到哈希与样本列表。
        /// </summary>
        /// <param name="hash">状态哈希累加器。</param>
        /// <param name="samples">状态样本输出列表。</param>
        /// <param name="entityId">实体编号。</param>
        /// <param name="value">移动指令组件值。</param>
        private static void AddMove(ref ulong hash, List<string> samples, int entityId, MoveCommandComponent value)
        {
            AddVector3(ref hash, samples, entityId, "Move.TargetPosition", value.TargetPosition);
            AddBool(ref hash, value.HasReached);
            AddLong(ref hash, value.StopDistance.value);
            samples.Add($"E:{entityId}:Move.HasReached={(value.HasReached ? 1 : 0)}");
            samples.Add($"E:{entityId}:Move.StopDistance={value.StopDistance.value}");
        }

        /// <summary>
        /// 将攻击组件字段追加到哈希与样本列表。
        /// </summary>
        /// <param name="hash">状态哈希累加器。</param>
        /// <param name="samples">状态样本输出列表。</param>
        /// <param name="entityId">实体编号。</param>
        /// <param name="value">攻击组件值。</param>
        private static void AddAttack(ref ulong hash, List<string> samples, int entityId, AttackComponent value)
        {
            AddInt(ref hash, value.MaxTargets);
            AddLong(ref hash, value.WarningRange.value);
            AddLong(ref hash, value.Range.value);
            AddInt(ref hash, value.ConfProjectileID);
            AddLong(ref hash, value.AttackInterval.value);
            AddLong(ref hash, value.TimeSinceLastAttack.value);
            AddInt(ref hash, value.TargetEntityId);
            AddBool(ref hash, value.PlayAttackAudio);

            samples.Add($"E:{entityId}:Attack.MaxTargets={value.MaxTargets}");
            samples.Add($"E:{entityId}:Attack.WarningRange={value.WarningRange.value}");
            samples.Add($"E:{entityId}:Attack.Range={value.Range.value}");
            samples.Add($"E:{entityId}:Attack.ConfProjectileID={value.ConfProjectileID}");
            samples.Add($"E:{entityId}:Attack.AttackInterval={value.AttackInterval.value}");
            samples.Add($"E:{entityId}:Attack.TimeSinceLastAttack={value.TimeSinceLastAttack.value}");
            samples.Add($"E:{entityId}:Attack.TargetEntityId={value.TargetEntityId}");
            samples.Add($"E:{entityId}:Attack.PlayAttackAudio={(value.PlayAttackAudio ? 1 : 0)}");
        }

        /// <summary>
        /// 将生产队列数量与进度字段追加到哈希与样本列表。
        /// </summary>
        /// <param name="hash">状态哈希累加器。</param>
        /// <param name="samples">状态样本输出列表。</param>
        /// <param name="entityId">实体编号。</param>
        /// <param name="value">生产组件值。</param>
        private static void AddProduce(ref ulong hash, List<string> samples, int entityId, ProduceComponent value)
        {
            int queueCount = value.ProduceNumbers?.Values.Sum() ?? 0;
            AddInt(ref hash, queueCount);
            samples.Add($"E:{entityId}:Produce.QueueCount={queueCount}");

            if (value.ProduceNumbers != null)
            {
                foreach (var pair in value.ProduceNumbers.OrderBy(x => (int)x.Key))
                {
                    AddInt(ref hash, (int)pair.Key);
                    AddInt(ref hash, pair.Value);
                    samples.Add($"E:{entityId}:Produce.Number.{pair.Key}={pair.Value}");
                }
            }

            if (value.ProduceProgress != null)
            {
                foreach (var pair in value.ProduceProgress.OrderBy(x => (int)x.Key))
                {
                    AddInt(ref hash, (int)pair.Key);
                    AddInt(ref hash, pair.Value);
                    samples.Add($"E:{entityId}:Produce.Progress.{pair.Key}={pair.Value}");
                }
            }
        }

        /// <summary>
        /// 将定点三维向量写入哈希与样本列表。
        /// </summary>
        /// <param name="hash">状态哈希累加器。</param>
        /// <param name="samples">状态样本输出列表。</param>
        /// <param name="entityId">实体编号。</param>
        /// <param name="name">字段名称前缀。</param>
        /// <param name="value">向量值。</param>
        private static void AddVector3(ref ulong hash, List<string> samples, int entityId, string name, zVector3 value)
        {
            AddLong(ref hash, value.x.value);
            AddLong(ref hash, value.y.value);
            AddLong(ref hash, value.z.value);
            samples.Add($"E:{entityId}:{name}.x={value.x.value}");
            samples.Add($"E:{entityId}:{name}.y={value.y.value}");
            samples.Add($"E:{entityId}:{name}.z={value.z.value}");
        }

        /// <summary>
        /// 将定点四元数写入哈希与样本列表。
        /// </summary>
        /// <param name="hash">状态哈希累加器。</param>
        /// <param name="samples">状态样本输出列表。</param>
        /// <param name="entityId">实体编号。</param>
        /// <param name="name">字段名称前缀。</param>
        /// <param name="value">四元数值。</param>
        private static void AddQuaternion(ref ulong hash, List<string> samples, int entityId, string name, zQuaternion value)
        {
            AddLong(ref hash, value.x.value);
            AddLong(ref hash, value.y.value);
            AddLong(ref hash, value.z.value);
            AddLong(ref hash, value.w.value);
            samples.Add($"E:{entityId}:{name}.x={value.x.value}");
            samples.Add($"E:{entityId}:{name}.y={value.y.value}");
            samples.Add($"E:{entityId}:{name}.z={value.z.value}");
            samples.Add($"E:{entityId}:{name}.w={value.w.value}");
        }

        /// <summary>
        /// 将 32 位整数按 FNV 规则混入哈希。
        /// </summary>
        /// <param name="hash">状态哈希累加器。</param>
        /// <param name="value">待混入的整数值。</param>
        private static void AddInt(ref ulong hash, int value)
        {
            unchecked
            {
                hash ^= (uint)value;
                hash *= FnvPrime;
            }
        }

        /// <summary>
        /// 将 64 位整数按 FNV 规则混入哈希。
        /// </summary>
        /// <param name="hash">状态哈希累加器。</param>
        /// <param name="value">待混入的长整型值。</param>
        private static void AddLong(ref ulong hash, long value)
        {
            unchecked
            {
                hash ^= (ulong)value;
                hash *= FnvPrime;
            }
        }

        /// <summary>
        /// 将布尔值转换为 0/1 后混入哈希。
        /// </summary>
        /// <param name="hash">状态哈希累加器。</param>
        /// <param name="value">待混入的布尔值。</param>
        private static void AddBool(ref ulong hash, bool value)
        {
            AddInt(ref hash, value ? 1 : 0);
        }
    }
}
