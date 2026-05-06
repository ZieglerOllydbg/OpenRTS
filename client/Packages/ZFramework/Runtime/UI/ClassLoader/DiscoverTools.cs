//using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// 数据模块发现机制
/// 若新加模块，只需扩展 BaseDiscover
/// @data: 2019-01-16
/// @author: LLL
/// </summary>
public static class DiscoverTools
{
    private class DiscoverItem
    {
        public DiscoverType DiscoverType;

        public BaseDiscover Discover;

        public void OnClear()
        {
            Discover?.OnClear();
        }
    }

    /// <summary>
    /// discover dic
    /// </summary>
    private static Dictionary<Type, DiscoverItem> discoverDic = new Dictionary<Type, DiscoverItem>();


    /// <summary>
    /// 发现模块
    /// </summary>
    public static void Discover(Assembly assembly)
    {
        var types = assembly.GetTypes();
        List<Type> typesList = new List<Type>(types);

        // 添加通用发现模块
        typesList.Add(typeof(UIModuleDiscover));
        typesList.Add(typeof(ModuleDiscover));

        for (var item = typesList.GetEnumerator(); item.MoveNext();)
        {
            var type = item.Current;

            if (type.IsSubclassOf(typeof(BaseDiscover)))
            {
                var typeAttributes = type.GetCustomAttributes(false);
                if (typeAttributes.Length > 0)
                {
                    DiscoverAttribute discover = typeAttributes[0] as DiscoverAttribute;
                    if (!discoverDic.ContainsKey(discover.attributeType))
                    {
                        var discoverInstance = Activator.CreateInstance(type) as BaseDiscover;
                        discoverDic[discover.attributeType] = new DiscoverItem { DiscoverType = discover.DiscoverType, Discover = discoverInstance };
                    }
                    else
                    {
                        Debugger.LogError(typeof(DiscoverTools).Name, $"已经包含 Type : {discover.attributeType}");
                    }
                }
                else
                {
                    Debugger.LogError(typeof(DiscoverTools).Name, $"未添加任何 Attribute，Type : {type}");
                }
            }
        }

        for (var item = types.GetEnumerator(); item.MoveNext();)
        {
            var type = item.Current as Type;

            for (var discover = discoverDic.GetEnumerator(); discover.MoveNext();)
            {
                var key = discover.Current.Key;
                var value = discover.Current.Value;

                if (value.DiscoverType == DiscoverType.Attribute)
                {
                    var typeAttribute = type.GetCustomAttributes(discover.Current.Key, false);
                    //找到了标记
                    if (typeAttribute.Length > 0)
                    {
                        discover.Current.Value.Discover.OnDiscoverModule(type, typeAttribute[0] as Attribute);
                    }
                }
                else if (value.DiscoverType == DiscoverType.Inherit_Type)
                {
                    try
                    {
                        //UnityEngine.Debug.LogError($"<color=orange> {type} </color>");
                        //添加一个判断，防止出现接口继承接口的问题
                        if (key.IsInterface && !type.IsInterface)
                        {
                            var interfaceTypes = type.GetInterfaces();
                            if (interfaceTypes.Length > 0)
                            {
                                foreach (var interfaceItem in interfaceTypes)
                                {
                                    var interfaceMap = type.GetInterfaceMap(interfaceItem);
                                    if (interfaceMap.InterfaceType == key)
                                    {
                                        discover.Current.Value.Discover.OnDiscoverModule(type, null);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (type.IsSubclassOf(key))
                            {
                                discover.Current.Value.Discover.OnDiscoverModule(type, null);
                            }
                        }
                    }
                    catch (Exception _e)
                    {
                        Debugger.LogError(typeof(DiscoverTools), _e.Message, _e);
                    }
                }
            }
        }

        /* 这里改为在 Discover 内部打印
         *Debugger.Log("Discover ", $"Module: { ModuleDiscover.DiscoverTypes.Count } " +
             $"Template: { TemplateDiscover.DiscoverTypes.Count } " +
             $"Table: { TableDiscover.DiscoverTypes.Count } " +
             $"UIModule: { UIModuleDiscover.DiscoverTypes.Count }" +
             $"TimelinePlay: { TimelineDiscover.DiscoverTypes.Count }" +
             $"Timeline: {TimelineDiscover.DiscoverTypes.Count}");*/
    }

    #region Assembly 类型相关

    /// <summary>
    /// 当前Assembly的全部类型
    /// </summary>
    /// <returns></returns>
    //public static Type[] DiscoverCurrentAssemblyTypes { get; } = typeof(DiscoverTools).Assembly.GetTypes();

    #endregion

    /// <summary>
    /// 清理
    /// </summary>
    public static void Clear()
    {
        for (var item = discoverDic.GetEnumerator(); item.MoveNext();)
        {
            item.Current.Value.OnClear();
        }

        discoverDic.Clear();
    }
}
