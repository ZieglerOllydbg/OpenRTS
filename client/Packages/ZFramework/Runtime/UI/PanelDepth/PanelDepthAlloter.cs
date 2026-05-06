using System;
using System.Collections.Generic;

/// <summary>
/// 面板层次管理分配器
/// @author Ollydbg
/// @date 2018-2-24
/// </summary>
public static class PanelDepthAlloter
{
    /// <summary>
    /// 在添加新的面板类型的时候 我们需要为新的面板类型分配一个新的order in layer的范围
    /// </summary>


    [UIPanelDepth(ClientUIDepthTypeID.ClientDefault)]
    public static UIPanelDepthDefine ClientDefault = new UIPanelDepthDefine(-500, -300);

    [UIPanelDepth(ClientUIDepthTypeID.TeachDefault)]
    public static UIPanelDepthDefine TeachDefault = new UIPanelDepthDefine(-200, 0);

    [UIPanelDepth(ClientUIDepthTypeID.GameDefault)]
    public static UIPanelDepthDefine GameDefault = new UIPanelDepthDefine(10, 500);

    [UIPanelDepth(ClientUIDepthTypeID.GameMid)]
    public static UIPanelDepthDefine GameMid = new UIPanelDepthDefine(500, 1000);

    [UIPanelDepth(ClientUIDepthTypeID.GameConstant)]
    public static UIPanelDepthDefine GameConstant = new UIPanelDepthDefine(1000, 1500);

    [UIPanelDepth(ClientUIDepthTypeID.GameTop)]
    public static UIPanelDepthDefine GameTop = new UIPanelDepthDefine(1500, 2000);

    [UIPanelDepth(ClientUIDepthTypeID.GameMaxTop)]
    public static UIPanelDepthDefine GameMaxTop = new UIPanelDepthDefine(2000, 2500);

    [UIPanelDepth(ClientUIDepthTypeID.GameSpecial)]
    public static UIPanelDepthDefine GameSpecial = new UIPanelDepthDefine(2000, 2500);

    [UIPanelDepth(ClientUIDepthTypeID.TeachPop)]
    public static UIPanelDepthDefine TeachPop = new UIPanelDepthDefine(2500, 2900);

    [UIPanelDepth(ClientUIDepthTypeID.CanvasTeachPop1609)]
    public static UIPanelDepthDefine TeachPop1609 = new UIPanelDepthDefine(2900, 3000);

    [UIPanelDepth(ClientUIDepthTypeID.ClientPop)]
    public static UIPanelDepthDefine ClientPop = new UIPanelDepthDefine(3000, 3500);

    [UIPanelDepth(ClientUIDepthTypeID.ClientPop2048)]
    public static UIPanelDepthDefine ClientPop2048 = new UIPanelDepthDefine(3000, 3500);

    [UIPanelDepth(ClientUIDepthTypeID.TeachLoading)]
    public static UIPanelDepthDefine TeachLoading = new UIPanelDepthDefine(3500, 4000);

    [UIPanelDepth(ClientUIDepthTypeID.ClientLoading)]
    public static UIPanelDepthDefine ClientLoading = new UIPanelDepthDefine(3500, 4000);


    [UIPanelDepth(ClientUIDepthTypeID.TeachH5Web)]
    public static UIPanelDepthDefine TeachH5Web = new UIPanelDepthDefine(4000, 4500);
    /// <summary>
    /// 面板深度数据 dic
    /// </summary>
    private static Dictionary<ClientUIDepthTypeID, UIPanelDepthDefine> panelDepthDic = new Dictionary<ClientUIDepthTypeID, UIPanelDepthDefine>();

    ///// <summary>
    ///// 最低层次
    ///// </summary>
    //private static int minDepth;
    ///// <summary>
    ///// 最高层次
    ///// </summary>
    //private static int maxDepth;

    /// <summary>
    /// 两个 Panel 之间预留的空间
    /// </summary>
    public static int UI_Depth_Space { get; } = 5;

    static PanelDepthAlloter()
    {
        var panelFields = typeof(PanelDepthAlloter).GetFields();

        int count = 0;

        for (int i = 0; i < panelFields.Length; i++)
        {
            var panelField = panelFields[i];

            var attribute = panelField.GetCustomAttributes(typeof(UIPanelDepthAttribute), false);

            if (attribute.Length > 0)
            {
                if (attribute[0] is UIPanelDepthAttribute panelDepthAttribute)
                {
                    count++;

                    if (!panelDepthDic.TryGetValue(panelDepthAttribute.DepthType, out UIPanelDepthDefine uiDepthDefine))
                    {
                        var currentPanelField = (UIPanelDepthDefine)panelField.GetValue(typeof(PanelDepthAlloter));

                        panelDepthDic[panelDepthAttribute.DepthType] = currentPanelField;

                        //minDepth = minDepth > currentPanelField.LowerLimit ? currentPanelField.LowerLimit : minDepth;

                        //maxDepth = maxDepth < currentPanelField.UpperLimit ? currentPanelField.UpperLimit : maxDepth;
                    }
                    else
                        throw new Exception("One PanelDepthType has been inited many times!");
                }
            }
        }

        if (panelDepthDic.Count != count)
            throw new Exception("Some PanelDepthType has not been inited! Please Check!");
    }

    /// <summary>
    /// 根据面板类型来获取面板深度
    /// </summary>
    /// <param name="_panel"></param>
    /// <returns></returns>
    public static UIPanelDepthDefine GetDepthByType(BasePanel _panel)
    {
        if (panelDepthDic.TryGetValue(_panel.ModelData.PanelUIDepthType, out UIPanelDepthDefine panelDefine))
            return panelDefine;

        throw new Exception($"No corresponding type found! PanelID: { _panel.ModelData.currentPanelID } / PanelType: { _panel.ModelData.PanelUIDepthType }");
    }
}
