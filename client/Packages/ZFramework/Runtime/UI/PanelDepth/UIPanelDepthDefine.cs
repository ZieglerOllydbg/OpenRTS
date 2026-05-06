using System;

/// <summary>
/// UI面板渲染顺序上下限定义
/// @author Ollydbg
/// @date 2018-2-24
/// </summary>
public struct UIPanelDepthDefine
{
    /// <summary>
    /// 下限定义
    /// </summary>
    public int LowerLimit { get; }

    /// <summary>
    /// 上限定义
    /// </summary>
    public int UpperLimit { get; }

    public UIPanelDepthDefine(int _lowerLimit, int _upperLimit)
    {
        this.LowerLimit = _lowerLimit;

        this.UpperLimit = _upperLimit;

        if (LowerLimit >= UpperLimit)
            throw new Exception("Depth is wrong!");
    }
}
