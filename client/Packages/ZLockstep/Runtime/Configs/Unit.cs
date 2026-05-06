//
// Auto Generated Code By excel2json
// https://neil3d.gitee.io/coding/excel2json.html
// 1. 每个 Sheet 形成一个 Struct 定义, Sheet 的名称作为 Struct 的名称
// 2. 表格约定：第一行是变量名称，第二行是变量类型

// Generate From Unit.xlsx
using UnityEngine.Scripting;

[Preserve]
public class ConfUnit
{
	public ConfUnit() {}

	[Preserve]
	public const string JsonFileName = "Unit";
	public int ID; // 编号
	public int Type; // 单位类型
	public string Prefab; // 资源prefab
	public int Scale; // 缩放比例万分比(10000=1)
	public int Radius; // 半径10000=1米
	public int OutlineScale; // 选中Prefab缩放比例（万分比10000=1）
	public int Speed; // 移动速度10000=1米/秒
	public string Icon; // 图标
	public int Hp; // 血量
	public int AutoHeal; // 自动回血万分比
	public int Def; // 防御
	public int WarningRange; // 预警范围
	public int AtkRange; // 攻击范围
	public int ProjectileID; // 子弹类型（指向ConfProjectile表）
	public string ProjectileSpawnOffset; // 子弹初始偏移
	public int AtkInterval; // 攻击频率10000=1秒
	public int AtkCount; // 攻击目标数量
	public int CostMoney; // 消耗金钱
	public int ProduceTime; // 生产时间（帧数，每秒20帧）
	public string Name; // 名称
	public string Description; // 描述
}

[Preserve]
public class ConfProjectile
{
	public ConfProjectile() {}

	[Preserve]
	public const string JsonFileName = "Unit";
	public int ID; // 编号
	public int Type; // 子弹类型1=子弹；2=炮弹；
	public int Speed; // 飞行速度（最大15）
	public int IsHoming; // 追踪类型导弹
	public int EnableARC; // 是否使用抛物线
	public int HitDistance; // 命中距离（万分比，5000=0.5米）
	public int DamageRadius; // 伤害半径（万分比，0=单体）
	public int MaxDamageTargets; // 最大伤害目标数（-1=无限制）
	public int ShareDamage; // 均摊伤害（0=全额，1=均摊）
	public int Damage; // 伤害
	public string Prefab; // 资源prefab
	public int Scale; // 缩放比例万分比(10000=1)
	public string DeathEffectPrefab; // 特效
	public int DeathEffectScale; // 特效缩放比例（万分比10000=1）
	public string AudioClip; // 音频
	public string Note; // 备注
}

[Preserve]
public class ConfRestraint
{
	public ConfRestraint() {}

	[Preserve]
	public const string JsonFileName = "Unit";
	public int ID; // 编号
	public string Name; // 类型
	public int Infantry; // 大兵
	public int BadgerTank; // 獾式坦克
	public int GrizzlyTank; // 灰熊坦克
}


// End of Auto Generated Code
