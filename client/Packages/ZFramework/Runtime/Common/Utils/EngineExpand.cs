using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 扩展类
/// </summary>
public static class EngineExpand
{
    #region	扩展Transform与GameObject
    /// <summary>
    /// 设置parent, 设置的同时, 会将孩子跟父亲在旋转角度,位置,缩放比例上进行同步
    /// </summary>
    /// <param name="__ts"></param>
    /// <param name="__parent"></param>
    public static void SetParentEx(this Transform __ts, Transform __parent)
    {
        var pos = __ts.position;
        var rot = __ts.rotation;
        var scale = __ts.lossyScale;
        //__ts.parent = __parent;
        __ts.SetParent(__parent, false);
        __ts.localScale = scale;
        __ts.localPosition = pos;
        __ts.localRotation = rot;
    }
    /// <summary>
    /// 设置parent
    /// </summary>
    /// <param name="__go"></param>
    /// <param name="__parent"></param>
    public static void SetParent(this GameObject __go, GameObject __parent)
    {
        __go.transform.SetParentEx(__parent.transform);
    }
    /// <summary>
    /// 初始化Transform（Position、Rotation、Scale初始化）
    /// </summary>
    /// <param name="__ts"></param>
    public static void Reset(this Transform __ts)
    {
        __ts.localPosition = Vector3.zero;
        __ts.localRotation = Quaternion.identity;
        __ts.localScale = Vector3.one;
    }
    #endregion


    #region	Material操作
    /// <summary>
    /// 设置显示状态
    /// 其实是设置Renderer的enable属性
    /// 注意：SetActive()方法会停掉逻辑，但这个不会。
    /// </summary>
    /// <param name="__go"></param>
    /// <param name="__enable"></param>
    public static void SetRender(this GameObject __go, bool __enable)
    {
        Renderer[] marr = __go.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < marr.Length; i++)
        {
            marr[i].enabled = __enable;
        }
    }
    /// <summary>
    /// 设置shader
    /// </summary>
    /// <param name="__ts"></param>
    /// <param name="__shader"></param>
    public static void SetShader(this Transform __ts, Shader __shader)
    {
        if (__ts == null)
            return;
        var render = __ts.gameObject.GetComponent<Renderer>();

        if (render != null)
        {
            for (int i = 0; i < render.materials.Length; i++)
            {
                var mt = render.materials[i];

                mt.shader = __shader;
            }
        }

        foreach (Transform child in __ts)
        {
            child.SetShader(__shader);
        }

    }
    /// <summary>
    /// 设置Texture
    /// </summary>
    /// <param name="__ts"></param>
    /// <param name="__propertyName"></param>
    /// <param name="_texture"></param>
    public static void SetTexture(this Transform __ts, string __propertyName, Texture __texture)
    {
        if (__ts == null)
            return;

        var render = __ts.gameObject.GetComponent<Renderer>();

        if (render != null)
        {
            for (int i = 0; i < render.materials.Length; i++)
            {
                var mt = render.materials[i];

                mt.SetTexture(__propertyName, __texture);
            }
        }

        foreach (Transform child in __ts)
        {
            child.SetTexture(__propertyName, __texture);
        }

    }
    /// <summary>
    /// 设置Color
    /// </summary>
    /// <param name="__ts"></param>
    /// <param name="__propertyName"></param>
    /// <param name="_color"></param>
    public static void SetColor(this Transform __ts, string __propertyName, Color __color)
    {
        if (__ts == null)
            return;

        var render = __ts.gameObject.GetComponent<Renderer>();

        if (render != null)
        {
            for (int i = 0; i < render.materials.Length; i++)
            {
                var mt = render.materials[i];

                mt.SetColor(__propertyName, __color);
            }
        }

        foreach (Transform child in __ts)
        {
            child.SetColor(__propertyName, __color);
        }

    }
    /// <summary>
    /// 设置Alpha
    /// </summary>
    /// <param name="__ts"></param>
    /// <param name="__propertyName"></param>
    /// <param name="_alpha"></param>
    public static void SetAlpha(this Transform __ts, string __propertyName, float __alpha)
    {
        if (__ts == null)
            return;

        var render = __ts.gameObject.GetComponent<Renderer>();

        if (render != null)
        {
            for (int i = 0; i < render.materials.Length; i++)
            {
                var mt = render.materials[i];

                if (mt.HasProperty(__propertyName))
                {
                    var c = mt.GetColor(__propertyName);
                    mt.SetColor(__propertyName, new Color(c.r, c.g, c.b, __alpha));
                }
            }
        }

        foreach (Transform child in __ts)
        {
            child.SetAlpha(__propertyName, __alpha);
        }

    }
    /// <summary>
    /// 替换Shader
    /// </summary>
    /// <param name="__ts"></param>
    /// <param name="__oldShader"></param>
    /// <param name="__newShader"></param>
    public static void ReplaceShader(this Transform __ts, Shader __oldShader, Shader __newShader)
    {
        if (__ts == null) return;

        var render = __ts.gameObject.GetComponent<Renderer>();

        if (render != null)
        {
            for (int i = 0; i < render.materials.Length; i++)
            {
                var mt = render.materials[i];

                if (mt.shader == __oldShader)
                {
                    mt.shader = __newShader;
                }
            }
        }

        foreach (Transform child in __ts)
        {
            child.ReplaceShader(__oldShader, __newShader);
        }
    }
    /// <summary>
    /// 替换Shader
    /// </summary>
    /// <param name="__ts"></param>
    /// <param name="__oldShaderName"></param>
    /// <param name="__newShader"></param>
    public static void ReplaceShader(this Transform __ts, string __oldShaderName, Shader __newShader)
    {
        if (__ts == null)
            return;

        var render = __ts.gameObject.GetComponent<Renderer>();

        if (render != null)
        {
            for (int i = 0; i < render.materials.Length; i++)
            {
                var mt = render.materials[i];

                if (mt.shader.name == __oldShaderName)
                {
                    mt.shader = __newShader;
                }
            }
        }

        foreach (Transform child in __ts)
        {
            child.ReplaceShader(__oldShaderName, __newShader);
        }
    }
    #endregion

    #region	提取组件


    /// <summary>
    /// 获取子元素（递归）(注意：如果不唯一，则只返回第一个)（深度优先）(检查自己)
    /// </summary>
    /// <param name="__ts"></param>
    /// <param name="__name"></param>
    /// <param name="__seachChildren"></param>
    /// <returns></returns>
    public static Transform GetChildByName(this Transform __ts, string __name, bool __seachChildren = true)
    {
        if (__ts.name.Equals(__name))
        {
            return __ts;
        }
        foreach (Transform ts in __ts)
        {
            if (__seachChildren)
            {
                Transform ttss = GetChildByName(ts, __name, __seachChildren);
                if (ttss != null)
                {
                    return ttss;
                }
            }
            else
            {
                if (ts.name.Equals(__name))
                {
                    return ts;
                }
            }
        }
        return null;
    }
    /// <summary>
    /// 获取子元素（递归）(注意：如果不唯一，则只返回第一个)（深度优先）(检查自己)
    /// </summary>
    /// <param name="__go"></param>
    /// <param name="__name"></param>
    /// <param name="__seachChildren"></param>
    /// <returns></returns>
    public static GameObject GetChildByName(this GameObject __go, string __name, bool __seachChildren = true)
    {
        var ts = GetChildByName(__go.transform, __name, __seachChildren);
        if (ts != null)
        {
            return ts.gameObject;
        }
        return null;
    }

    /// <summary>
    /// 获取子元素(检查自己)
    /// </summary>
    /// <param name="__ts"></param>
    /// <param name="__name"></param>
    /// <param name="__seachChildren"></param>
    /// <returns></returns>
    public static Transform[] GetChildrenByName(this Transform __ts, string __name, bool __seachChildren = true)
    {
        List<Transform> list = new List<Transform>();
        Transform[] tList = __ts.GetChildren(__seachChildren);
        tList.ForEach(p =>
        {
            if (p.name.Equals(__name))
            {
                list.Add(p);
            }
        });
        return list.ToArray();
    }
    /// <summary>
    /// 获取子元素(检查自己)
    /// </summary>
    /// <param name="__go"></param>
    /// <param name="__name"></param>
    /// <param name="__seachChildren"></param>
    /// <returns></returns>
    public static GameObject[] GetChildrenByName(this GameObject __go, string __name, bool __seachChildren = true)
    {
        List<GameObject> list = new List<GameObject>();
        Transform[] tList = __go.transform.GetChildrenByName(__name, __seachChildren);
        tList.ForEach(p =>
        {
            list.Add(p.gameObject);
        });
        return list.ToArray();
    }



    /// <summary>
    /// 获取所有子元素(包含自己)
    /// </summary>
    /// <param name="__ts"></param>
    /// <param name="__seachChildren"></param>
    /// <returns></returns>
    public static Transform[] GetChildren(this Transform __ts, bool __seachChildren = true)
    {
        Transform[] tList = null;
        if (__seachChildren)
        {
            tList = __ts.GetComponentsInChildren<Transform>(true);
        }
        else
        {
            List<Transform> tempList = new List<Transform>() { __ts };
            foreach (Transform ts in __ts)
            {
                tempList.Add(ts);
            }
            tList = tempList.ToArray();
        }
        return tList;
    }
    /// <summary>
    /// 获取所有子元素(包含自己)
    /// </summary>
    /// <param name="__go"></param>
    /// <param name="__seachChildren"></param>
    /// <returns></returns>
    public static GameObject[] GetChildren(this GameObject __go, bool __seachChildren = true)
    {
        List<GameObject> list = new List<GameObject>();
        Transform[] tList = __go.transform.GetChildren(__seachChildren);
        tList.ForEach(p =>
        {
            list.Add(p.gameObject);
        });
        return list.ToArray();
    }

    /// <summary>
    /// 获取所有子元素(包含自己)
    /// </summary>
    /// <param name="__go"></param>
    /// <param name="__seachChildren"></param>
    /// <returns></returns>
    public static GameObject[] GetChildrenTrueForAll(this GameObject __go, bool __seachChildren = true, Func<GameObject, bool> testFunction = null)
    {
        List<GameObject> list = new List<GameObject>();
        Transform[] tList = __go.transform.GetChildren(__seachChildren);
        tList.ForEach(p =>
        {
            if (testFunction != null)
            {
                var go = p.gameObject;

                if (testFunction(go))
                {
                    list.Add(p.gameObject);
                }
            }
            else
                list.Add(p.gameObject);
        });
        return list.ToArray();
    }

    /// <summary>
    /// 获取Component
    /// </summary>
    /// <param name="__go"></param>
    /// <param name="__creat">如果不存在是否新创建</param>
    /// <returns></returns>
    public static T GetComponent<T>(this GameObject __go, bool __creat) where T : Component
    {
        T t = __go.GetComponent<T>();
        if (t == null && __creat)
        {
            t = __go.AddComponent<T>();
        }
        return t;
    }
    /// <summary>
    /// 获取Component
    /// </summary>
    /// <param name="__ts"></param>
    /// <param name="__creat">如果不存在是否新创建</param>
    /// <returns></returns>
    public static T GetComponent<T>(this Component __ts, bool __creat) where T : Component
    {
        return __ts.gameObject.GetComponent<T>(__creat);
    }


    /// <summary>
    /// 通过递归方式(包含自己)    在自己下面的树中寻找 T类型的组件
    /// 无论组件是否enable, 都会查找出来
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="__go"></param>
    /// <returns></returns>
    public static T GetComponentInChildrenByRecursion<T>(this GameObject __go) where T : Component
    {
        T t = null;
        //如果就在自身上挂着  则直接获取
        if (__go.GetComponent<T>() != null)
        {
            t = __go.GetComponent<T>();
        }
        //否则 递归往下寻找(儿子-->孙子-->....)
        else
        {
            //找到所有儿子, 遍历子对象本身
            GameObject[] gos = __go.GetChildren();

            for (int i = 0; i < gos.Length; i++)
            {
                var gooo = gos[i];

                if (!gooo.Equals(__go))
                {
                    t = gooo.GetComponent<T>();
                    if (t != null)
                        break;
                }

            }
        }

        return t;
    }

    #endregion
}