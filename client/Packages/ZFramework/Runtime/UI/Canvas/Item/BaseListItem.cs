using UnityEngine;
using ZLib;

/// <summary>
/// 动态创建UI元素时可以使用  利用缓存池取元素
/// </summary>
public class BaseListItem : ICache
{
    /// <summary>
    /// 当前环节object
    /// </summary>
    protected GameObject obj;

    /// <summary>
    /// 目标容器
    /// </summary>
    public GameObject targetObj
    {
        get
        {
            return this.obj;
        }
    }

    private BaseListItemTemplete itemTemplete;

    public bool isStrong
    {
        get
        {
            return true;
        }
    }

    public const string KEY = "BaseListItem";

    public string UseKey = "";

    public string key
    {
        get
        {
            if (!string.IsNullOrEmpty(UseKey))
            {
                return UseKey;
            }
            return this.GetType().ToString();
        }
    }

    public BaseListItem(GameObject __target)
    {
        this.obj = __target;

        if (this.obj == null)
        {
            //this.Log("构造函数传入的__obj不能为空");
            return;
        }

        this.itemTemplete = this.obj.GetComponent<BaseListItemTemplete>() as BaseListItemTemplete;
    }

    /// <summary>
    /// 刷新数据
    /// </summary>
    public virtual void UpdateDataAsync()
    {

    }

    public virtual void SetActive(bool __isShow)
    {

    }

    public virtual void Close()
    {


    }

    public virtual void Destroy()
    {
        itemTemplete = null;

        if (this.obj != null && this.obj.transform != null)
        {
            this.obj.SetActive(false);
            this.obj.transform.SetParent(PanelManager.HideRoot, false);
            //this.obj.transform.parent = GameInstance.HideRoot;
            GameObject.Destroy(this.obj);
        }
        this.obj = null;
    }
}

