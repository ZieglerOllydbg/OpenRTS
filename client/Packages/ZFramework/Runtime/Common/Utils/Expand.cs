using System;
using System.Collections.Generic;

/// <summary>
/// 扩展类
/// </summary>
public static class Expand
{
    #region	扩展C#的基本类
    /// <summary>
    /// 通过Value来找Key(注意：如果Value不唯一，则只返回第一个)
    /// 使用的判断方法为：Equals
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="__dictionary"></param>
    /// <param name="__value"></param>
    /// <returns></returns>
    public static TKey GetKey<TKey, TValue>(this Dictionary<TKey, TValue> __dictionary, TValue __value)
    {
        foreach (TKey key in __dictionary.Keys)
        {
            if (__dictionary[key].Equals(__value))
            {
                return key;
            }
        }
        return default(TKey);
    }
    /// <summary>
    /// 获取元素位置（注意：如果Value不唯一，则只返回第一个）
    /// 使用的判断方法为：Equals
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="__ts"></param>
    /// <param name="__t"></param>
    /// <returns></returns>
    public static int IndexOf<T>(this T[] __ts, T __t)
    {
        int index = -1;
        for (int i = 0; i < __ts.Length; i++)
        {
            if (__ts[i] == null) continue;
            if (__ts[i].Equals(__t))
            {
                index = i;
                break;
            }
        }
        return index;
    }
    /// <summary>
    /// 浅复制
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="__ts"></param>
    /// <returns></returns>
    public static List<T> Clone<T>(this List<T> __ts)
    {
        return __ts.GetRange(0, __ts.Count);
    }
    /// <summary>
    /// 安全的ForEach
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="__ts"></param>
    /// <param name="__action"></param>
    /// <returns></returns>
    public static void ForEach<T>(this T[] __ts, Action<T> __action)
    {
        for (int i = 0; i < __ts.Length; i++)
        {
            __action(__ts[i]);
        }
    }
    /// <summary>
    /// 安全的ForEach
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="__ts"></param>
    /// <param name="__action"></param>
    /// <returns></returns>
    public static void SafeForEach<T>(this T[] __ts, Action<T> __action)
    {
        __ts.ForEach(p => __action(p));
    }
    /// <summary>
    /// 安全的ForEach（注意：此方法因需要副本， 所以比原生的ForEach开销要大. 但原生的ForEach是不安全的）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="__ts"></param>
    /// <param name="__action"></param>
    /// <returns></returns>
    public static void SafeForEach<T>(this List<T> __ts, Action<T> __action)
    {
        var arr = __ts.ToArray();
        for (int i = 0; i < arr.Length; i++)
        {
            __action(arr[i]);
        }
    }
    /// <summary>
    /// 是否是某个类的实例
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static bool IsInstance<T>(this object __obj)
    {
        return __obj.IsInstance(typeof(T));
    }
    /// <summary>
    /// 是否是某个类的实例
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static bool IsInstance(this object __obj, Type __t)
    {
        return __obj.GetType() == __t;
    }
    #endregion
}