using ZLockstep.Simulation.ECS;

/// <summary>
/// 生产列表项数据结构 - 心跳数据结构，用于MainProducerSubPanel显示生产信息
/// </summary>
public class ProducerItemData
{
    /// <summary>
    /// 生产名称
    /// </summary>
    public string Name;
    
    /// <summary>
    /// 所属工厂
    /// </summary>
    public string BelongFactory;
    
    /// <summary>
    /// 描述
    /// </summary>
    public string Description;
    
    /// <summary>
    /// 单位类型
    /// </summary>
    public UnitType UnitType;
    
    /// <summary>
    /// 工厂实体ID
    /// </summary>
    public int FactoryEntityId;
}