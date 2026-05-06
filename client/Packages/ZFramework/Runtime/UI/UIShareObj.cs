using UnityEngine;
using ZLib;

/// <summary>
/// UI 缓存结构
/// @data: 2019-01-18
/// @author: LLL
/// </summary>
public class UIShareObj : IShare
{
    public GameObject shareGameObject;

    public int count { get; set; }

    public float time { get; set; }

    public void AddRefInfo(object obj)
    {
    }

    public void DelRefInfo(object obj)
    {
    }

    public void Destroy()
    {
        Object.Destroy(shareGameObject);

        shareGameObject = null;
    }
}
public class UITextureObj : IShare
{
    public Texture shareGameObject;

    public int count { get; set; }

    public float time { get; set; }

    public void AddRefInfo(object obj)
    {
    }

    public void DelRefInfo(object obj)
    {
    }

    public void Destroy()
    {
        Object.Destroy(shareGameObject);
        shareGameObject = null;
    }
}