
using System;
using UnityEngine;
using ZFrame;

/// <summary>
/// 基本Processor（扩展，增加网络通讯消息处理的功能）
/// 这里其实更类似一个Controller，主要用于和外部其他Processor通信，向下控制Panel里的data，修改完后，刷新UI
/// </summary>
public abstract class BaseProcessor : Processor
{
    /// <summary>
    /// 重载构造函数
    /// </summary>
    /// <param name="_module"></param>
    public BaseProcessor(Module _module)
        : base(_module)
    {
    }

    /// <summary>
    /// 获取当前的Module 类型数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    protected T GetMoule<T>() where T : Module
    {
        var rModule = (T) module;

        if (rModule != null)
            return rModule;

        return default;
    }

    #region 事件处理

    /// <summary>
    /// 处理网络事件消息方法
    /// </summary>
    /// <param name="__msg"></param>
    protected abstract void ReceivedMessage(object __msg);

    /// <summary>
    /// 这里做一个消息过滤
    /// </summary>
    /// <param name="__me"></param>
    protected sealed override void ReceivedModuleEvent(ModuleEvent __me)
    {
        try
        {
            ReceiveModuleEvent(__me);
        }
        catch (Exception e)
        {
            //this.Tip("逻辑相关错误 <br> error :" + e.Message, $"{this.GetType().Name}:{__me.GetType().Name}");

            Debug.LogException(e);

            throw e;
        }
    }

    /// <summary>
    /// 事件监听
    /// </summary>
    /// <param name="__me"></param>
    protected abstract void ReceiveModuleEvent(ModuleEvent __me);

    #endregion
}