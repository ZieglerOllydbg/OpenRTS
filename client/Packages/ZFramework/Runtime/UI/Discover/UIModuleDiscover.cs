using System;
using System.Collections.Generic;

/// <summary>
/// UI Module 发现
/// @data: 2019-01-16
/// @author: LLL
/// </summary>
[Discover(typeof(UIModelAttribute))]
public class UIModuleDiscover : BaseDiscover
{
    /// <summary>
    /// 发现的数据
    /// </summary>
    public static SortedList<UIModelAttribute, Type> DiscoverTypes { get; } = new SortedList<UIModelAttribute, Type>(new AttributeCompare<UIModelAttribute>());
    /// <summary>
    /// panel 字典
    /// </summary>
    private static Dictionary<string, UIModelData> panelDic;
    /// <summary>
    /// UIModelData List
    /// </summary>
    private static List<UIModelData> panelDataList;
    /// <summary>
    /// 缓存数据
    /// </summary>
    private static List<UIModelData> cacheModelDataList = new List<UIModelData>();

    /// <summary>
    /// 获取 UIModel Data
    /// </summary>
    /// <param name="_panelID"></param>
    /// <returns></returns>
    public static UIModelData GetUIModel(string _panelID)
    {
        if (panelDic == null)
        {
            panelDic = new Dictionary<string, UIModelData>();
            Debugger.Log("UIModuleDiscover",$" InItUIModelData :{_panelID}");
            panelDataList = InitUIModelData();

            for (int i = 0; i < panelDataList.Count; i++)
            {
                var panelData = panelDataList[i];

                if (!panelDic.ContainsKey(panelData.currentPanelID))
                {
                    panelDic[panelData.currentPanelID] = panelData;
                }
                else
                {
                    throw new Exception($"PanelID can't define more times. { panelData.currentPanelID }");
                }
            }
        }

        UIModelData result;

        if (panelDic.TryGetValue(_panelID, out result))
        {
            return result;
        }
        else
        {
            throw new Exception($"Can't find panels define! { _panelID }");
        }
    }

    /// <summary>
    /// 获取 UIModel Data（支持PanelID枚举，向后兼容）
    /// </summary>
    /// <param name="_panelID"></param>
    /// <returns></returns>
    public static UIModelData GetUIModel(PanelID _panelID)
    {
        return GetUIModel(_panelID.ToString());
    }

    /// <summary>
    /// 数据驱动初始化 UIModel
    /// </summary>
    /// <returns></returns>
    private static List<UIModelData> InitUIModelData()
    {
        List<UIModelData> uiModelList = new List<UIModelData>();

        for (var item = DiscoverTypes.GetEnumerator(); item.MoveNext();)
        {
            var attribute = item.Current.Key;

            //var tableConfig = Table_Client_Ui_Config.GetPrimary((int)attribute.panelID);

            //if (tableConfig == null)
                //Debugger.LogError(typeof(UIModuleDiscover), $"Can't get Table_Client_Ui_Config by panelID: { attribute.panelID }");

            UIModelData data = new UIModelData(item.Current.Key, item.Current.Value);

            uiModelList.Add(data);
        }

        return uiModelList;
    }

    /// <summary>
    /// 获得父面板的所有子面板
    /// </summary>
    /// <param name="_parentPanelID"></param>
    /// <returns></returns>
    public static List<UIModelData> GetSubPanel(string _parentPanelID)
    {
        cacheModelDataList.Clear();

        for (int i = 0; i < panelDataList.Count; i++)
        {
            var panelData = panelDataList[i];

            if (panelData.ParentID == _parentPanelID)
                cacheModelDataList.Add(panelData);
        }

        //排序
        cacheModelDataList.Sort((p, s) => p.TabIndex - s.TabIndex);

        for (int i = 0; i < cacheModelDataList.Count; i++)
        {
            var cacheModelData = cacheModelDataList[i];

            if (cacheModelData.TabIndex != i)
                throw new Exception($"Parent is the child panel with parentPanelID: { _parentPanelID }, there is wrong with tabIndex: { i }");
        }

        return cacheModelDataList;
    }

    /// <summary>
    /// 获得父面板的所有子面板（支持PanelID枚举，向后兼容）
    /// </summary>
    /// <param name="_parentPanelID"></param>
    /// <returns></returns>
    public static List<UIModelData> GetSubPanel(PanelID _parentPanelID)
    {
        return GetSubPanel(_parentPanelID.ToString());
    }

    public override void OnDiscoverModule(Type _type, Attribute _attribute)
    {
        if (_type.IsSubclassOf(typeof(BasePanel)) && !_type.IsAbstract)
        {
            var uiModelAttribute = _attribute as UIModelAttribute;

            if (!DiscoverTypes.ContainsKey(uiModelAttribute))
            {
                DiscoverTypes.Add(uiModelAttribute, _type);
            }
            else
            {
                //this.Tip($"PanelID : { uiModelAttribute.panelID } 重复，type : { _type.FullName } <br> SVN检查谁重复用了这个PanelID");
            }
        }
        else
        {
            throw new Exception($"Not Allow a template not extend Template! { _type.FullName }");
        }
    }

    public override void OnClear()
    {
        DiscoverTypes.Clear();
    }
}
