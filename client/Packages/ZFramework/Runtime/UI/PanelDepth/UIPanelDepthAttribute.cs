using System;

/// <summary>
/// 用来约束 UI 上下限的Attribute
/// @data: 2019-01-18
/// @author: LLL
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class UIPanelDepthAttribute : Attribute
{
    public ClientUIDepthTypeID DepthType { get; }

    public UIPanelDepthAttribute(ClientUIDepthTypeID _type)
        => this.DepthType = _type;
}
