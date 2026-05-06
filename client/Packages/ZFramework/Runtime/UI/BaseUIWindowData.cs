using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI脚本 基类
/// </summary>
public class BaseUIWindowData : MonoBehaviour
{
    private bool _IsInit = false;

    /// <summary>
    /// 初始化
    /// </summary>
    public virtual void Init()
    {
        if (true == this._IsInit) return;
        this.OnInit();
        this._IsInit = true;
    }

    /// <summary>
    /// 初始化后回调函数
    /// </summary>
    public virtual void OnInit() { }

    /// <summary>
    /// 关闭
    /// </summary>
    public virtual void Close()
    {
        this.OnClose();
    }

    /// <summary>
    /// 关闭之前被调用
    /// </summary>
    public virtual void OnClose()
    {
    }
}
