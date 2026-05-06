
/// <summary>
/// 定义一个自定义 Data 的基类
/// 后期可能会扩展一些通用方法
/// @data: 2019-01-23
/// @author: LLL
/// </summary>
public class UIBaseData
{
    /// <summary>
    /// 奖励 动画 状态,是否在动画中
    /// </summary>
    public bool isRewardAnimation
    {
        get;
        set;
    }

    /// <summary>
    /// 初始化
    /// </summary>
    public virtual void OnInit()
    { }

    /// <summary>
    /// 更新
    /// </summary>
    public virtual void Update()
    { }

    /// <summary>
    /// 注销
    /// </summary>
    public virtual void OnDispose()
    { }
}
