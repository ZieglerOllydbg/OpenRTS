using System.Collections.Generic;
using NUnit.Framework;
using ZLockstep.RVO;
using zUnity;

/// <summary>
/// Simulator 稳定行为回归测试集合。
/// 关键用途是验证实例重建、代理增删、时间步进、查询边界、障碍物基础链路与 Clear 重置语义，
/// 并避免对 ORCA 内部迭代细节做脆弱断言。
/// </summary>
[TestFixture]
public class SimulatorTests
{
    private static readonly zVector2 DefaultVelocity = zVector2.zero;
    private static readonly zfloat DefaultNeighborDist = (zfloat)5;
    private static readonly int DefaultMaxNeighbors = 8;
    private static readonly zfloat DefaultTimeHorizon = (zfloat)3;
    private static readonly zfloat DefaultTimeHorizonObst = (zfloat)3;
    private static readonly zfloat DefaultRadius = (zfloat)1;
    private static readonly zfloat DefaultMaxSpeed = (zfloat)2;

    /// <summary>
    /// 每个测试前重建模拟器实例，避免单例状态串扰。
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        Simulator.Instance.Clear();
    }

    /// <summary>
    /// 验证未设置默认参数时，addAgent(position) 返回 -1。
    /// </summary>
    [Test]
    public void AddAgentWithoutDefaults_ReturnsMinusOne()
    {
        Simulator simulator = Simulator.Instance;

        int agentId = simulator.addAgent(zVector2.zero);

        Assert.AreEqual(-1, agentId);
        Assert.AreEqual(0, simulator.getNumAgents());
    }

    /// <summary>
    /// 验证设置默认参数后，addAgent(position) 会创建有效代理并继承默认属性。
    /// </summary>
    [Test]
    public void AddAgentWithDefaults_InheritsConfiguredValues()
    {
        Simulator simulator = Simulator.Instance;
        simulator.setAgentDefaults(DefaultNeighborDist, DefaultMaxNeighbors, DefaultTimeHorizon, DefaultTimeHorizonObst, DefaultRadius, DefaultMaxSpeed, DefaultVelocity);
        zVector2 startPos = new zVector2((zfloat)3, (zfloat)4);

        int agentId = simulator.addAgent(startPos);

        Assert.GreaterOrEqual(agentId, 0);
        Assert.AreEqual(startPos.x.value, simulator.getAgentPosition(agentId).x.value);
        Assert.AreEqual(startPos.y.value, simulator.getAgentPosition(agentId).y.value);
        Assert.AreEqual(DefaultRadius.value, simulator.getAgentRadius(agentId).value);
        Assert.AreEqual(DefaultMaxSpeed.value, simulator.getAgentMaxSpeed(agentId).value);
    }

    /// <summary>
    /// 验证 delAgent 仅标记删除，执行 doStep 后才真正移除代理并更新映射状态。
    /// </summary>
    [Test]
    public void DelAgent_TakesEffectAfterDoStep()
    {
        Simulator simulator = Simulator.Instance;
        simulator.setAgentDefaults(DefaultNeighborDist, DefaultMaxNeighbors, DefaultTimeHorizon, DefaultTimeHorizonObst, DefaultRadius, DefaultMaxSpeed, DefaultVelocity);
        int keepId = simulator.addAgent(new zVector2((zfloat)0, (zfloat)0));
        int removeId = simulator.addAgent(new zVector2((zfloat)2, (zfloat)0));

        simulator.delAgent(removeId);

        Assert.IsTrue(simulator.IsAgentNoExist(removeId));
        Assert.AreEqual(2, simulator.getNumAgents());

        simulator.doStep();

        Assert.IsFalse(simulator.IsAgentNoExist(removeId));
        Assert.IsTrue(simulator.IsAgentNoExist(keepId));
        Assert.AreEqual(1, simulator.getNumAgents());
    }

    /// <summary>
    /// 验证删除不存在的代理编号不会抛异常且不会改变代理数量。
    /// </summary>
    [Test]
    public void DelAgent_WhenAgentDoesNotExist_IsNoOp()
    {
        Simulator simulator = Simulator.Instance;
        simulator.setAgentDefaults(DefaultNeighborDist, DefaultMaxNeighbors, DefaultTimeHorizon, DefaultTimeHorizonObst, DefaultRadius, DefaultMaxSpeed, DefaultVelocity);
        simulator.addAgent(zVector2.zero);

        Assert.DoesNotThrow(() => simulator.delAgent(99999));
        Assert.AreEqual(1, simulator.getNumAgents());
    }

    /// <summary>
    /// 验证 setTimeStep 后，doStep 返回值与全局时间按步长累加一致。
    /// </summary>
    [Test]
    public void DoStep_AccumulatesGlobalTimeWithConfiguredTimeStep()
    {
        Simulator simulator = Simulator.Instance;
        simulator.setTimeStep((zfloat)0.5f);

        zfloat t1 = simulator.doStep();
        zfloat t2 = simulator.doStep();

        Assert.AreEqual(((zfloat)0.5f).value, t1.value);
        Assert.AreEqual(((zfloat)1.0f).value, t2.value);
        Assert.AreEqual(((zfloat)1.0f).value, simulator.getGlobalTime().value);
    }

    /// <summary>
    /// 验证空代理场景下 queryNearAgent 返回 -1。
    /// </summary>
    [Test]
    public void QueryNearAgent_WhenNoAgents_ReturnsMinusOne()
    {
        Simulator simulator = Simulator.Instance;

        int foundId = simulator.queryNearAgent(zVector2.zero, (zfloat)10);

        Assert.AreEqual(-1, foundId);
    }

    /// <summary>
    /// 验证在构建代理树后，queryNearAgent 能返回最近代理编号，并在超半径时返回 -1。
    /// </summary>
    [Test]
    public void QueryNearAgent_AfterDoStep_ReturnsExpectedIdOrMinusOne()
    {
        Simulator simulator = Simulator.Instance;
        simulator.setAgentDefaults(DefaultNeighborDist, DefaultMaxNeighbors, DefaultTimeHorizon, DefaultTimeHorizonObst, DefaultRadius, DefaultMaxSpeed, DefaultVelocity);
        int nearId = simulator.addAgent(new zVector2((zfloat)0, (zfloat)0));
        simulator.addAgent(new zVector2((zfloat)10, (zfloat)0));
        simulator.doStep();

        int foundNear = simulator.queryNearAgent(new zVector2((zfloat)0.2f, (zfloat)0), (zfloat)1);
        int foundFar = simulator.queryNearAgent(new zVector2((zfloat)100, (zfloat)100), (zfloat)1);

        Assert.AreEqual(nearId, foundNear);
        Assert.AreEqual(-1, foundFar);
    }

    /// <summary>
    /// 验证未处理障碍物时 queryVisibility 返回 true，处理后遇到阻挡线段返回 false。
    /// </summary>
    [Test]
    public void QueryVisibility_BeforeAndAfterProcessObstacles_HasExpectedResult()
    {
        Simulator simulator = Simulator.Instance;
        IList<zVector2> segment = new List<zVector2>
        {
            new zVector2((zfloat)0, (zfloat)(-1)),
            new zVector2((zfloat)0, (zfloat)1)
        };
        simulator.addObstacle(segment);

        bool visibleBeforeProcess = simulator.queryVisibility(new zVector2((zfloat)(-1), (zfloat)0), new zVector2((zfloat)1, (zfloat)0), zfloat.Zero);

        simulator.processObstacles();
        bool visibleAfterProcess = simulator.queryVisibility(new zVector2((zfloat)(-1), (zfloat)0), new zVector2((zfloat)1, (zfloat)0), zfloat.Zero);

        Assert.IsTrue(visibleBeforeProcess);
        Assert.IsFalse(visibleAfterProcess);
    }

    /// <summary>
    /// 验证 addObstacle 在非法顶点数量下返回 -1，并在合法多边形下建立正确顶点关系。
    /// </summary>
    [Test]
    public void AddObstacle_ValidatesVertexCountAndBuildsVertexLinks()
    {
        Simulator simulator = Simulator.Instance;
        int invalidResult = simulator.addObstacle(new List<zVector2> { zVector2.zero });
        IList<zVector2> square = new List<zVector2>
        {
            new zVector2((zfloat)(-1), (zfloat)(-1)),
            new zVector2((zfloat)1, (zfloat)(-1)),
            new zVector2((zfloat)1, (zfloat)1),
            new zVector2((zfloat)(-1), (zfloat)1)
        };

        int firstVertexId = simulator.addObstacle(square);

        Assert.AreEqual(-1, invalidResult);
        Assert.AreEqual(0, firstVertexId);
        Assert.AreEqual(4, simulator.getNumObstacleVertices());
        Assert.AreEqual(1, simulator.getNextObstacleVertexNo(0));
        Assert.AreEqual(3, simulator.getPrevObstacleVertexNo(0));
    }

    /// <summary>
    /// 验证 Clear 会重置代理与障碍物集合、全局时间与默认时间步。
    /// </summary>
    [Test]
    public void Clear_ResetsSimulationStateAndTimeStep()
    {
        Simulator simulator = Simulator.Instance;
        simulator.setAgentDefaults(DefaultNeighborDist, DefaultMaxNeighbors, DefaultTimeHorizon, DefaultTimeHorizonObst, DefaultRadius, DefaultMaxSpeed, DefaultVelocity);
        simulator.addAgent(new zVector2((zfloat)1, (zfloat)1));
        simulator.addObstacle(new List<zVector2>
        {
            new zVector2((zfloat)0, (zfloat)0),
            new zVector2((zfloat)1, (zfloat)0)
        });
        simulator.setGlobalTime((zfloat)9);
        simulator.setTimeStep((zfloat)0.25f);

        simulator.Clear();

        Assert.AreEqual(0, simulator.getNumAgents());
        Assert.AreEqual(0, simulator.getNumObstacleVertices());
        Assert.AreEqual(zfloat.Zero.value, simulator.getGlobalTime().value);
        Assert.AreEqual(new zfloat(0, 1000).value, simulator.getTimeStep().value);
        Assert.AreEqual(-1, simulator.addAgent(zVector2.zero));
    }

    /// <summary>
    /// 验证 Clear 会重置全局状态，且保持单例实例不变。
    /// </summary>
    [Test]
    public void Clear_ResetsStateWithoutReplacingSingletonInstance()
    {
        Simulator simulator = Simulator.Instance;
        simulator.setAgentDefaults(DefaultNeighborDist, DefaultMaxNeighbors, DefaultTimeHorizon, DefaultTimeHorizonObst, DefaultRadius, DefaultMaxSpeed, DefaultVelocity);
        simulator.addAgent(zVector2.zero);

        simulator.Clear();
        Simulator cleared = Simulator.Instance;

        Assert.AreSame(simulator, cleared);
        Assert.AreEqual(0, cleared.getNumAgents());
        Assert.AreEqual(zfloat.Zero.value, cleared.getGlobalTime().value);
        Assert.AreEqual(-1, cleared.addAgent(zVector2.zero));
    }

    /// <summary>
    /// 验证在相同参数下，单智能体移动后的位置在 Clear 重置前后保持一致，避免单例状态污染确定性结果。
    /// </summary>
    [Test]
    public void Clear_SameSingleAgentMovement_ReturnsSamePosition()
    {
        zVector2 firstRunPosition = RunSingleAgentMovementAndGetPosition();

        Simulator.Instance.Clear();

        zVector2 secondRunPosition = RunSingleAgentMovementAndGetPosition();

        Assert.AreEqual(firstRunPosition.x.value, secondRunPosition.x.value);
        Assert.AreEqual(firstRunPosition.y.value, secondRunPosition.y.value);
    }

    /// <summary>
    /// 验证设置期望速度后，在未执行 doStep 前，实际速度仍保持旧值，期望速度可被正确读取。
    /// </summary>
    [Test]
    public void SetAgentPrefVelocity_BeforeDoStep_DoesNotImmediatelyChangeActualVelocity()
    {
        Simulator simulator = Simulator.Instance;
        simulator.setAgentDefaults(
            DefaultNeighborDist,
            DefaultMaxNeighbors,
            DefaultTimeHorizon,
            DefaultTimeHorizonObst,
            DefaultRadius,
            (zfloat)10,
            DefaultVelocity);

        int agentId = simulator.addAgent(new zVector2((zfloat)1, (zfloat)1));
        zVector2 targetVelocity = new zVector2((zfloat)2, (zfloat)3);

        simulator.setAgentPrefVelocity(agentId, targetVelocity);

        zVector2 actualVelocityBeforeStep = simulator.getAgentVelocity(agentId);
        zVector2 prefVelocityBeforeStep = simulator.getAgentPrefVelocity(agentId);

        Assert.AreEqual(DefaultVelocity.x.value, actualVelocityBeforeStep.x.value);
        Assert.AreEqual(DefaultVelocity.y.value, actualVelocityBeforeStep.y.value);
        Assert.AreEqual(targetVelocity.x.value, prefVelocityBeforeStep.x.value);
        Assert.AreEqual(targetVelocity.y.value, prefVelocityBeforeStep.y.value);
    }

    /// <summary>
    /// 验证两次 Clear 后在相同输入下，doStep 后的实际速度与位置都保持一致。
    /// </summary>
    [Test]
    public void Clear_SameSingleAgentMovement_ReturnsSamePositionAndVelocity()
    {
        RunSingleAgentMovementAndGetState(out zVector2 firstRunPosition, out zVector2 firstRunVelocity);

        Simulator.Instance.Clear();

        RunSingleAgentMovementAndGetState(out zVector2 secondRunPosition, out zVector2 secondRunVelocity);

        Assert.AreEqual(firstRunPosition.x.value, secondRunPosition.x.value);
        Assert.AreEqual(firstRunPosition.y.value, secondRunPosition.y.value);
        Assert.AreEqual(firstRunVelocity.x.value, secondRunVelocity.x.value);
        Assert.AreEqual(firstRunVelocity.y.value, secondRunVelocity.y.value);
    }

    private static zVector2 RunSingleAgentMovementAndGetPosition()
    {
        RunSingleAgentMovementAndGetState(out zVector2 finalPosition, out _);
        return finalPosition;
    }

    private static void RunSingleAgentMovementAndGetState(out zVector2 finalPosition, out zVector2 finalVelocity)
    {
        Simulator simulator = Simulator.Instance;
        simulator.setAgentDefaults(
            DefaultNeighborDist,
            DefaultMaxNeighbors,
            DefaultTimeHorizon,
            DefaultTimeHorizonObst,
            DefaultRadius,
            (zfloat)10,
            DefaultVelocity);
        simulator.setTimeStep((zfloat)0.1f);

        int agentId = simulator.addAgent(new zVector2((zfloat)1, (zfloat)1));
        zVector2 targetVelocity = new zVector2((zfloat)2, (zfloat)3);
        simulator.setAgentPrefVelocity(agentId, targetVelocity);
        simulator.doStep();

        finalPosition = simulator.getAgentPosition(agentId);
        finalVelocity = simulator.getAgentVelocity(agentId);
    }
}
