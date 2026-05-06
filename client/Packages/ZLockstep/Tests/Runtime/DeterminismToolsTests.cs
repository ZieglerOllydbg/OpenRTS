using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ZLockstep.Simulation;
using ZLockstep.Sync.Command;
using ZLockstep.Sync.Determinism;
using Utils;

/// <summary>
/// 确定性工具链相关回归测试。
/// 主要覆盖回放差异比对与命令录制帧号落盘行为，确保定位首个分歧帧与未来帧命令记录准确。
/// </summary>
public class DeterminismToolsTests
{
    /// <summary>
    /// 验证回放差异工具在随机状态首次不一致时，能够定位到第一个分歧帧并标记随机分歧类型。
    /// </summary>
    [Test]
    public void ReplayDiffTool_DetectsRandomMismatchAtFirstDifferentTick()
    {
        var baseline = new List<FrameDigest>
        {
            new FrameDigest { tick = 0, commandCount = 0, commandHash = 11, randomStateRaw = 100, stateHash = 200, activeEntityCount = 1, stateSamples = new List<string>() },
            new FrameDigest { tick = 1, commandCount = 1, commandHash = 22, randomStateRaw = 101, stateHash = 201, activeEntityCount = 1, stateSamples = new List<string>() }
        };

        var replay = new List<FrameDigest>
        {
            new FrameDigest { tick = 0, commandCount = 0, commandHash = 11, randomStateRaw = 100, stateHash = 200, activeEntityCount = 1, stateSamples = new List<string>() },
            new FrameDigest { tick = 1, commandCount = 1, commandHash = 22, randomStateRaw = 999, stateHash = 201, activeEntityCount = 1, stateSamples = new List<string>() }
        };

        ReplayDiffResult result = ReplayDiffTool.Compare(baseline, replay);

        Assert.IsFalse(result.IsMatch);
        Assert.AreEqual(1, result.FirstMismatchTick);
        Assert.AreEqual(ReplayMismatchType.RandomMismatch, result.MismatchType);
    }

    /// <summary>
    /// 验证回放差异工具在状态样本文本发生变化时，能够输出状态分歧并保留关键字段明细。
    /// </summary>
    [Test]
    public void ReplayDiffTool_DetectsStateSampleDifference()
    {
        var baseline = new List<FrameDigest>
        {
            new FrameDigest
            {
                tick = 7,
                commandCount = 0,
                commandHash = 10,
                randomStateRaw = 20,
                stateHash = 30,
                activeEntityCount = 1,
                stateSamples = new List<string> { "E:1:Health.CurrentHealth=10000" }
            }
        };

        var replay = new List<FrameDigest>
        {
            new FrameDigest
            {
                tick = 7,
                commandCount = 0,
                commandHash = 10,
                randomStateRaw = 20,
                stateHash = 31,
                activeEntityCount = 1,
                stateSamples = new List<string> { "E:1:Health.CurrentHealth=9000" }
            }
        };

        ReplayDiffResult result = ReplayDiffTool.Compare(baseline, replay);

        Assert.IsFalse(result.IsMatch);
        Assert.AreEqual(7, result.FirstMismatchTick);
        Assert.AreEqual(ReplayMismatchType.StateMismatch, result.MismatchType);
        Assert.IsTrue(result.Details.Exists(d => d.Contains("E:1:Health.CurrentHealth")));
    }

    /// <summary>
    /// 验证命令管理器录制未来帧命令时，会将命令写入其 ExecuteFrame 对应的帧输入记录。
    /// </summary>
    [Test]
    public void CommandManager_RecordsExecuteFrame_ForFutureCommand()
    {
        string fileName = $"command_record_test_{System.Guid.NewGuid():N}.txt";
        string filePath = Path.Combine(UnityEngine.Application.persistentDataPath, fileName);
        try
        {
            var world = new zWorld();
            world.Init(20);

            world.CommandManager.StartRecording(fileName);
            var command = new ProbeTestCommand(1) { ExecuteFrame = 15 };
            world.CommandManager.SubmitCommand(command);
            world.CommandManager.StopRecording();

            var reader = new CommandReader();
            reader.LoadFromFile(fileName);
            int frame = reader.FrameInputs.Count > 0 ? reader.FrameInputs[0].frame : -1;

            Assert.AreEqual(15, frame);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    /// <summary>
    /// 用于测试录制流程的探针命令，仅用于注入可控 ExecuteFrame 并触发命令写入路径。
    /// </summary>
    private class ProbeTestCommand : BaseCommand
    {
        /// <summary>
        /// 初始化探针命令并绑定阵营标识，供命令管理器序列化与回放测试使用。
        /// </summary>
        /// <param name="campId">命令所属阵营 ID。</param>
        public ProbeTestCommand(int campId) : base(campId)
        {
        }

        /// <summary>
        /// 测试命令执行体为空实现；本用例仅关注命令录制帧号，不验证具体逻辑副作用。
        /// </summary>
        /// <param name="world">命令执行时的世界上下文。</param>
        public override void Execute(zWorld world)
        {
        }
    }
}
