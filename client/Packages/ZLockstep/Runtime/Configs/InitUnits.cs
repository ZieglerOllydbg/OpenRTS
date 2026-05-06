//
// Auto Generated Code By excel2json
// https://neil3d.gitee.io/coding/excel2json.html
// 1. 每个 Sheet 形成一个 Struct 定义, Sheet 的名称作为 Struct 的名称
// 2. 表格约定：第一行是变量名称，第二行是变量类型

// Generate From InitUnits.xlsx
using UnityEngine.Scripting;

[Preserve]
public class ConfInitUnits
{
	public ConfInitUnits() {}

	[Preserve]
	public const string JsonFileName = "InitUnits";
	public string ID; // 编号
	public int Camp; // 阵营
	public int Type; // 主类型：1=建筑；2=单位
	public int ConfID; // 配置ID
	public int PrefabId; // 资源ID-Deprecated
	public string Position; // 位置
	public string Note; // 备注
	public int Enabled; // 是否启动
}


// End of Auto Generated Code
