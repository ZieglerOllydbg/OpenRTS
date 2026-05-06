using ZLockstep.Simulation.ECS;


/// <summary>
/// 建筑列表项数据结构 - 心跳数据结构，用于MainBuildSubPanel显示建筑信息
/// </summary>
public class BuildItemData
{
    /// <summary>
    /// 配置ID
    /// </summary>
    public int ConfBuildingPlaceID;
    /// <summary>
    /// 建筑名称
    /// </summary>
    public string Name;
    
    /// <summary>
    /// 建筑描述
    /// </summary>
    public string Description;
    
    /// <summary>
    /// 建筑类型
    /// </summary>
    public BuildingType BuildingType;
    
}
