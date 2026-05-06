//
// Auto Generated Code By excel2json
// https://neil3d.gitee.io/coding/excel2json.html
// 1. 每个 Sheet 形成一个 Struct 定义, Sheet 的名称作为 Struct 的名称
// 2. 表格约定：第一行是变量名称，第二行是变量类型

// Generate From ExampleData.xlsx
using UnityEngine.Scripting;

[Preserve]
public class ConfNPC
{
	public ConfNPC() {}

	[Preserve]
	public const string JsonFileName = "ExampleData";
	public string ID; // 编号
	public string Name; // 名称
	public string AssetName; // 资源编号
	public int HP; // 血
	public int Attack; // 攻击
	public int Defence; // 防御
	public string DateTest; // 测试日期
	public string TestJsonArray; // 测试单元格内的Json数组
	public string TestJsonObject; // 测试单元格内的Json对象
}

[Preserve]
public class ConfItem
{
	public ConfItem() {}

	[Preserve]
	public const string JsonFileName = "ExampleData";
	public string ID; // 编号
	public string Name; // 名称
	public string AssetName; // 资源编号
}


// End of Auto Generated Code
