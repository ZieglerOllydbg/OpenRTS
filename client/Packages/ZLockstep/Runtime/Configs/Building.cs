//
// Auto Generated Code By excel2json
// https://neil3d.gitee.io/coding/excel2json.html
// 1. 每个 Sheet 形成一个 Struct 定义, Sheet 的名称作为 Struct 的名称
// 2. 表格约定：第一行是变量名称，第二行是变量类型

// Generate From Building.xlsx
using UnityEngine.Scripting;

[Preserve]
public class ConfBuilding
{
	public ConfBuilding() {}

	[Preserve]
	public const string JsonFileName = "Building";
	public int ID; // 编号
	public int Type; // 建筑类型
	public int Manual; // 手动建造
	public string Prefab; // 资源prefab
	public string BuildPrefab; // 建造模型
	public int Scale; // 缩放比例万分比(10000=1)
	public int Rotation; // 旋转角度0-360
	public string SpawnOffset; // 出生点偏移量
	public int Size; // 占地尺寸（直径）建议奇数
	public int Hp; // 血量
	public int HpOffsetX; // 血条X偏移
	public int HpOffsetY; // 血条Y偏移
	public int AutoHeal; // 自动回血万分比
	public int CanAttack; // 是否可以攻击0=不攻击1=攻击
	public int ProjectileID; // 子弹类型（指向ConfProjectile表）
	public string ProjectileSpawnOffset; // 子弹初始偏移
	public int AtkRange; // 攻击范围
	public int AtkInterval; // 攻击频率10000=1秒
	public int AtkCount; // 攻击目标数量
	public int Def; // 防御
	public int ConstructionTime; // 建造时间（秒）
	public int CostMoney; // 消耗金币
	public int ProducePower; // 增加能量
	public int CostPower; // 消耗能量
	public string CreateUnitIDList; // 可以建造的单位列表
	public string Name; // 名称
	public string Description; // 描述
}

[Preserve]
public class ConfBuildingPlace
{
	public ConfBuildingPlace() {}

	[Preserve]
	public const string JsonFileName = "Building";
	public int ID; // 编号
	public int CampID; // 阵营
	public int Type; // 建筑类型
	public string Icon; // 图标
	public int Count; // 数量
	public string Position; // 位置
	public string Name; // 名称
	public int Enabled; // 是否启用
}


// End of Auto Generated Code
