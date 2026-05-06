//
// Auto Generated Code By excel2json
// https://neil3d.gitee.io/coding/excel2json.html
// 1. 每个 Sheet 形成一个 Struct 定义, Sheet 的名称作为 Struct 的名称
// 2. 表格约定：第一行是变量名称，第二行是变量类型

// Generate From Camp.xlsx
using UnityEngine.Scripting;

[Preserve]
public class ConfCamp
{
	public ConfCamp() {}

	[Preserve]
	public const string JsonFileName = "Camp";
	public int ID; // 阵营编号
	public string BarracksPosition; // 生产出生点
	public string VehicleFactoryPosition; // 生产出生点
	public int InitMoney; // 初始金币
	public int InitPower; // 初始能量
	public int AddMoneyPerSecond; // 每秒增加金币
	public int HealDelayTick; // 自动回血的冷却时间
}


// End of Auto Generated Code
