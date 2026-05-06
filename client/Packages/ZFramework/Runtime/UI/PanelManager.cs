//#define LOAD_LOCAL
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using ZLib;

/// <summary>
/// Panel 管理器
/// 用于管理 Panel 的
///     栈
///     层级分配
///     互斥
///     共存
///     等等
/// @author Ollydbg
/// @date 2018-2-23
/// </summary>
public class PanelManager : Singleton<PanelManager>
{
    public PanelManager()
    {
        SpriteAtlasManager.atlasRequested += OnAtlasRequest;
        ObjectShare<UIShareObj>.destroyDelay = 5.0f;//UI延迟销毁时间
    }

    private static Transform _uiRoot;

    public static Transform UIRoot
    {
        get
        {
            if (_uiRoot == null)
            {
                var uiRootObj = GameObject.Find("UIRoot");
                
                if (uiRootObj != null)
                {
                    _uiRoot = uiRootObj.transform;
                }
                else
                {
                    // 创建 UIRoot
                    var uiGo = new GameObject("UIRoot");
                    
                    // 配置 Canvas
                    var canvas = uiGo.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    
                    // 配置 CanvasScaler
                    var scaler = uiGo.AddComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    scaler.matchWidthOrHeight = 0.5f;
                    
                    uiGo.AddComponent<GraphicRaycaster>();
                    
                    // 不销毁
                    GameObject.DontDestroyOnLoad(uiGo);

                    _uiRoot = uiGo.transform;
                }
            }

            return _uiRoot;
        }
    }

    private static Transform _hideRoot;
    public static Transform HideRoot
    {
        get
        {
            if (_hideRoot == null)
            {
                var hideRootObj = GameObject.Find("UIHideRoot");
                
                if (hideRootObj != null)
                {
                    _hideRoot = hideRootObj.transform;
                }
                else
                {
                    // 创建 UIHideRoot
                    var hideGo = new GameObject("UIHideRoot");
                    hideGo.SetActive(false);
                    
                    // 不销毁
                    GameObject.DontDestroyOnLoad(hideGo);

                    _hideRoot = hideGo.transform;
                }
            }
            return _hideRoot;
        }
    }

    public static string UI_RES_PATH = "UI/";

    public static string atlasConfigPath;

    public override void Dispose()
    {
        base.Dispose();
        SpriteAtlasManager.atlasRequested -= OnAtlasRequest;
    }

    /// <summary>
    /// 所有注册的面板列表
    /// </summary>
    private Dictionary<string, BasePanel> allPanelDic = new Dictionary<string, BasePanel>();

    /// <summary>
    /// 当前正在显示的 Panel 列表
    /// </summary>
    private List<BasePanel> showPanelList = new List<BasePanel>();

    /// <summary>
    /// TestGUI专用，禁止其他地方调用
    /// </summary>
    /// <returns></returns>
    public List<BasePanel> GetShowPanelList()
    {
        return this.showPanelList;
    }
    /// <summary>
    /// 当前正在加载
    /// </summary>
    private Dictionary<string, LoadUICache> currentLoadDic = new Dictionary<string, LoadUICache>();

    #region 面板管理

    /// <summary>
    /// 添加一个 Panel 到管理器
    /// </summary>
    /// <param name="_panel"></param>
    public void AddPanel(BasePanel _panel)
    {
        if (_panel == null)
            throw new Exception("Can't add null Panel To PanelManager!");

        if (_panel.ModelData == null)
            throw new Exception("Can't add Panel with null modelData to PanelManager!");

        if (!string.IsNullOrEmpty(_panel.ModelData.currentPanelID) && !allPanelDic.ContainsKey(_panel.ModelData.currentPanelID))
            allPanelDic[_panel.ModelData.currentPanelID] = _panel;
        else
            throw new Exception($"PanelManager exist the same panel: {_panel.ModelData.currentPanelID}");
    }

    /// <summary>
    /// 从管理器中移除一个 Panel
    /// </summary>
    /// <param name="_panel"></param>
    public void RemovePanel(BasePanel _panel)
    {
        if (!object.ReferenceEquals(_panel, null) && _panel.ModelData != null)
        {
            if (!allPanelDic.Remove(_panel.ModelData.currentPanelID))
                this.LogError($"Complete Panel failed! {_panel.ModelData.currentPanelID}");
        }
        else
            throw new Exception("Panel or ModelData can't be null!");
    }
    /// <summary>
    /// 根据类型获取一个面板界面
    /// </summary>
    /// <param name="__panelID"></param>
    public BasePanel GetPanelByID(string __panelID)
    {
        if (allPanelDic.TryGetValue(__panelID, out BasePanel __panel))
        {
            return __panel;
        }
        //else
        //   this.LogError("Panel or ModelData can't be null!");

        return null;
    }

    /// <summary>
    /// 根据类型获取一个面板界面（支持PanelID枚举，向后兼容）
    /// </summary>
    /// <param name="__panelID"></param>
    public BasePanel GetPanelByID(PanelID __panelID)
    {
        return GetPanelByID(__panelID.ToString());
    }

    /// <summary>
    /// 管理分配层次问题
    /// 处理 互斥与共存
    /// </summary>
    /// <param name="_panel"></param>
    public void OpenPanel(BasePanel _panel)
    {
        var range = PanelDepthAlloter.GetDepthByType(_panel);

        _panel.UIDepth = GetNeedUIDepthByType(_panel);

        if (_panel.UIDepth >= range.UpperLimit)
            throw new Exception($"Panel {_panel.ModelData.PanelName}'s depth {_panel.UIDepth} out of range");

        if (!showPanelList.Contains(_panel))
            showPanelList.Add(_panel);

        //TODO 其他检测或处理 如果需要处理共存等关系，添加 3D摄像机，刷帧检测，新手引导检测

        #region 局部方法

        //获得下个面板深度
        int GetNeedUIDepthByType(BasePanel _targetPanel)
        {
            var depth = GetTopPanelUIDepthByType(_targetPanel);

            return depth + PanelDepthAlloter.UI_Depth_Space;
        }

        // 获取当前最高层的 Panel 的 Depth
        int GetTopPanelUIDepthByType(BasePanel _targetPanel)
        {
            var modelData = _targetPanel.ModelData;

            if (modelData != null)
            {
                var depth = PanelDepthAlloter.GetDepthByType(_targetPanel).LowerLimit;

                for (var i = 0; i < showPanelList.Count; i++)
                {
                    var showPanel = showPanelList[i];

                    if (showPanel != null && showPanel.ModelData.currentPanelID != modelData.currentPanelID)
                    {
                        if (showPanel.UIDepth > depth && showPanel.ModelData.PanelUIDepthType == modelData.PanelUIDepthType)
                            depth = showPanel.UIDepth;
                    }
                }

                return depth;
            }

            throw new Exception("ModelData can't be null!");
        }

        #endregion
    }

    /// <summary>
    /// 关闭 Panel ，恢复深度信息
    /// </summary>
    /// <param name="_panel"></param>
    public void ClosePanel(BasePanel _panel)
    {
        if (_panel == null || _panel.ModelData == null)
            throw new Exception("Panel or Panel's modelData is null!");

        if (showPanelList.Contains(_panel))
            showPanelList.Remove(_panel);
    }

    /// <summary>
    /// 根据类型参数关闭面板
    /// </summary>
    /// <param name="_types"></param>
    public void ClosePanelByType(ClientUITypeID[] _types)
    {
        for (int i = showPanelList.Count - 1; i >= 0; i--)
        {
            var showPanel = showPanelList[i];

            if (_types.IndexOf(showPanel.ModelData.PanelUIType) != -1)
                showPanel.Close();
        }

    }

    /// <summary>
    /// 关闭全部打开的面板
    /// </summary>
    public void CloseAll()
    {
        BasePanel panel = null;

        while (showPanelList.Count > 0)
        {
            panel = showPanelList[0];

            panel.Close();
        }

        showPanelList.Clear();
    }

    /// <summary>
    /// 销毁全部的 Panel
    /// </summary>
    public void DestroyAll()
    {
        List<BasePanel> list = new List<BasePanel>();
        list.AddRange(allPanelDic.Values);

        for (int i = 0; i < list.Count; i++)
        {
            var panel = list[i];
            panel.Close();
            panel.Destroy();
        }

        list.Clear();
    }

    #endregion

    #region 外部调用方法

    /// <summary>
    /// 根据面板深度类型获取不同的 Parent
    /// </summary>
    /// <param name="_type"></param>
    /// <returns></returns>
    public Transform GetParentByPanelDepthType(ClientUIDepthTypeID _type)
    {
        return UIRoot;
        //return GameInstance.GetCanvasTransform(_type);
        /* switch (_type)
         {
             case ClientUIDepthTypeID.Default:

                 return GameInstance.GetCanvasTransform(CanvasTransformType.Bottom);

             case ClientUIDepthTypeID.CanvasTop:

                 return GameInstance.GetCanvasTransform(CanvasTransformType.Top);

             case ClientUIDepthTypeID.CanvasMain:

                 return GameInstance.GetCanvasTransform(CanvasTransformType.Constant);

             case ClientUIDepthTypeID.CanvasMaxTop:

                 return GameInstance.GetCanvasTransform(CanvasTransformType.MaxTop);
             case ClientUIDepthTypeID.CanvasTeacher:

                 return GameInstance.GetCanvasTransform(CanvasTransformType.Teacher);

             case ClientUIDepthTypeID.CanvasTeachClient:

                 return GameInstance.GetCanvasTransform(CanvasTransformType.TeachClient);

             default:
                 {
                     this.LogError("Unknow ClientUIDepthTypeID!");
                     return GameInstance.GetCanvasTransform(CanvasTransformType.Bottom);
                 }
         }*/
    }


    ///// <summary>
    ///// 销毁缓存中的资源
    ///// </summary>
    ///// <param name="__panelID"></param>
    ///// <param name="__panelSkinID"></param>
    //public void DestoryUIRes(PanelID __panelID, PanelSkinID __panelSkinID)
    //{
    //    var url = GetPanelPath(__panelID, __panelSkinID);

    //    var share = ObjectShare<UIShareObj>.GetShareData(url);

    //    if (share != null)
    //    {
    //        share.Destroy();
    //    }
    //}

    /// <summary>
    /// 允许外面借一个回来
    /// 但是一定要还回来
    /// </summary>
    /// <param name="_panelID"></param>
    /// <param name="__panelSkinID"></param>
    /// <param name="_borrowComplete"></param>
    public void BorrowUIRes(string _panelID, PanelSkinID __panelSkinID, Action<string, PanelSkinID, GameObject> _borrowComplete)
    {
        var url = GetPanelPath(_panelID, __panelSkinID);

        var share = ObjectShare<UIShareObj>.GetShareData(url);

        if (share != null && share.shareGameObject != null)
        {
            if (_borrowComplete != null)
            {
                //添加引用计数
                ObjectShare<UIShareObj>.InstallShareData(url);

                _borrowComplete(_panelID, __panelSkinID, share.shareGameObject);
            }
            else
                throw new Exception("CallBack can't null!");
        }
        else
        {
            LoadUICache cache;

            if (currentLoadDic.TryGetValue(url, out cache))
            {
                //添加一次引用
                cache.ShareCount++;
                //如果正在加载，就存起来回调
                cache.CallBackList.Add(_borrowComplete);
            }
            else
            {
                //真正开启加载
                cache = new LoadUICache();

                cache.Url = url;

                cache.CallBackList.Add(_borrowComplete);
                //初始化一次引用
                cache.ShareCount = 1;

                currentLoadDic[cache.Url] = cache;

                //加载
                OnResourcesLoad(_panelID, __panelSkinID, cache);
            }
        }
    }

    /// <summary>
    /// 允许外面借一个回来（支持PanelID枚举，向后兼容）
    /// </summary>
    /// <param name="_panelID"></param>
    /// <param name="__panelSkinID"></param>
    /// <param name="_borrowComplete"></param>
    public void BorrowUIRes(PanelID _panelID, PanelSkinID __panelSkinID, Action<PanelID, PanelSkinID, GameObject> _borrowComplete)
    {
        BorrowUIRes(_panelID.ToString(), __panelSkinID, (panelID, skinID, go) =>
        {
            if (Enum.TryParse<PanelID>(panelID, out PanelID enumPanelID))
            {
                _borrowComplete?.Invoke(enumPanelID, skinID, go);
            }
            else
            {
                _borrowComplete?.Invoke(PanelID.PREFABE_ID_INVAILD, skinID, go);
            }
        });
    }

    /// <summary>
    /// 预热一个面板
    /// </summary>
    /// <param name="_panelID"></param>
    /// <param name="__panelSkinID"></param>
    /// <param name="_borrowComplete"></param>
    public void WarmUp(string _panelID, PanelSkinID __panelSkinID, Action<string, PanelSkinID, GameObject> _borrowComplete)
    {
        var url = GetPanelPath(_panelID, __panelSkinID);

        var share = ObjectShare<UIShareObj>.GetShareData(url);

        if (share != null && share.shareGameObject != null)
        {
            if (_borrowComplete != null)
            {
                //添加引用计数
                _borrowComplete(_panelID, __panelSkinID, share.shareGameObject);
            }
            else
                throw new Exception("CallBack can't null!");
        }
        else
        {
            LoadUICache cache;

            if (currentLoadDic.TryGetValue(url, out cache))
            {
                //添加一次引用
                //如果正在加载，就存起来回调
                cache.CallBackList.Add(_borrowComplete);
            }
            else
            {
                //真正开启加载
                cache = new LoadUICache();

                cache.Url = url;

                cache.CallBackList.Add(_borrowComplete);
                //初始化一次引用
                cache.ShareCount = 0;

                currentLoadDic[cache.Url] = cache;

                //加载
                OnResourcesLoad(_panelID, __panelSkinID, cache);
            }
        }
    }

    /// <summary>
    /// 预热一个面板（支持PanelID枚举，向后兼容）
    /// </summary>
    /// <param name="_panelID"></param>
    /// <param name="__panelSkinID"></param>
    /// <param name="_borrowComplete"></param>
    public void WarmUp(PanelID _panelID, PanelSkinID __panelSkinID, Action<PanelID, PanelSkinID, GameObject> _borrowComplete)
    {
        WarmUp(_panelID.ToString(), __panelSkinID, (panelID, skinID, go) =>
        {
            if (Enum.TryParse<PanelID>(panelID, out PanelID enumPanelID))
            {
                _borrowComplete?.Invoke(enumPanelID, skinID, go);
            }
            else
            {
                _borrowComplete?.Invoke(PanelID.PREFABE_ID_INVAILD, skinID, go);
            }
        });
    }


    /// <summary>
    /// 归还计数
    /// </summary>
    /// <param name="_paneID"></param>
    /// <param name="__panelSkinID"></param>
    /// <param name="_panelGo"></param>
    /// <param name="__isSynchronousDestory">是否同步 销毁 </param>
    public void GiveBackUIRes(string _paneID, PanelSkinID __panelSkinID, GameObject _panelGo, bool __isSynchronousDestory = false)
    {
        var url = GetPanelPath(_paneID, __panelSkinID);
        //减少引用计数
        ObjectShare<UIShareObj>.UnInstallShareData(url);

        if (_panelGo)
        {
            _panelGo.transform.SetParent(HideRoot, false);

            this.DestoryPanelGameObject(__isSynchronousDestory, _panelGo);
        }
    }

    /// <summary>
    /// 归还计数（支持PanelID枚举，向后兼容）
    /// </summary>
    /// <param name="_paneID"></param>
    /// <param name="__panelSkinID"></param>
    /// <param name="_panelGo"></param>
    /// <param name="__isSynchronousDestory">是否同步 销毁 </param>
    public void GiveBackUIRes(PanelID _paneID, PanelSkinID __panelSkinID, GameObject _panelGo, bool __isSynchronousDestory = false)
    {
        GiveBackUIRes(_paneID.ToString(), __panelSkinID, _panelGo, __isSynchronousDestory);
    }

    /// <summary>
    /// 销毁界面Gameobj
    /// </summary>
    /// <param name="__isSynchronousDestory"></param>
    /// <param name="__panelObj"></param>
    public void DestoryPanelGameObject(bool __isSynchronousDestory = false, GameObject __panelObj = null)
    {
        if (__isSynchronousDestory)
        {
            if (__panelObj != null)
            {
                ///同步销毁
                GameObject.DestroyImmediate(__panelObj);
            }
        }
    }
    #endregion

    #region 内部公共方法

    /// <summary>
    /// 获取 Panel 的路径
    /// </summary>
    /// <param name="_panelID"></param>
    /// <param name="__panelSkinID"></param>
    /// <returns></returns>
    private string GetPanelPath(string _panelID, PanelSkinID __panelSkinID)
    {
        var panelPath = GetPanelPath();


        var url = $"{UI_RES_PATH}{panelPath}";

        return url;

        //获得 PanelPath
        string GetPanelPath()
        {
            var modelData = UIModelData.GetCacheModelData(_panelID);

            ///如果传入了皮肤ID，优先使用皮肤ID 
            if (__panelSkinID != PanelSkinID.None)
            {
                var path = modelData.GetCacheSkinUIResPath(__panelSkinID);

                if (!string.IsNullOrEmpty(path))
                {
                    return path;
                }
            }

            return modelData.PanelPath;
        }
    }

    /// <summary>
    /// 加载 UI 资源
    /// </summary>
    /// <param name="_panelID"></param>
    /// <param name="__panelSkinID"></param>
    /// <param name="_cache"></param>
    private void OnResourcesLoad(string _panelID, PanelSkinID __panelSkinID, LoadUICache _cache)
    {
        //this.Log("LoadUIObject : " + _cache.Url);

        GameObject asset = Resources.Load<GameObject>(_cache.Url);
        GameObject go = GameObject.Instantiate<GameObject>(asset);
        if (go)
        {
            if (_cache.ShareCount == 0)
                go.transform.SetParent(HideRoot, false);

            var url = GetPanelPath(_panelID, __panelSkinID);

            if (!ObjectShare<UIShareObj>.HasShareData(url))
                ObjectShare<UIShareObj>.AddShareData(url, new UIShareObj { shareGameObject = go });

            for (int i = 0; i < _cache.ShareCount; i++)
            {
                ObjectShare<UIShareObj>.InstallShareData(url);
            }

            currentLoadDic.Remove(_cache.Url);

            _cache.ExcuteCallBack(_panelID, __panelSkinID, go);
        }
        else
        {
            currentLoadDic.Remove(_cache.Url);
            this.LogError($"Current loading resource is not exist, PanelID: {_panelID}");
            _cache.ExcuteCallBack(_panelID, __panelSkinID, null);
        }
    }

    /// <summary>
    /// 处理图集加载请求
    /// </summary>
    private async void OnAtlasRequest(string tag, Action<SpriteAtlas> callback)
    {
        this.Log("OnAtlasRequest : " + tag);
        string atlasPath = System.IO.Path.Combine(atlasConfigPath, tag);
        var asset = await Resources.LoadAsync<SpriteAtlas>(atlasPath) as SpriteAtlas;
        callback(asset);

        //AssetsManager.Instance.LoadObject(atlasPath).AddCallback(OnLoadComplete, OnError);

        ////加载完毕
        //void OnLoadComplete(AssetsAsyncOperation _operation)
        //{
        //    SpriteAtlas atlas = _operation.GetAsset<SpriteAtlas>();

        //    if (atlas)
        //        callback(atlas);
        //    else
        //        this.LogError($"Current loading Atlas is not exist, tag: {tag}");
        //}

        ////加载错误
        //void OnError(AssetsAsyncOperation _operation)
        //    => this.LogError(_operation.error);
    }

    #endregion

    #region LoadUICache

    /// <summary>
    /// 上层缓存
    /// </summary>
    private class LoadUICache
    {
        /// <summary>
        /// 正在加载的 URL
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// 回调 list
        /// </summary>
        public List<Action<string, PanelSkinID, GameObject>> CallBackList { get; } = new List<Action<string, PanelSkinID, GameObject>>();
        /// <summary>
        /// 添加几次引用计数
        /// </summary>
        public int ShareCount { get; set; } = 0;

        /// <summary>
        /// 执行回调
        /// </summary>
        /// <param name="_panelID"></param>
        /// <param name="__panelSkinID"></param>
        /// <param name="_go"></param>
        public void ExcuteCallBack(string _panelID, PanelSkinID __panelSkinID, GameObject _go)
        {
            for (int i = 0; i < CallBackList.Count; i++)
            {
                CallBackList[i]?.Invoke(_panelID, __panelSkinID, _go);
            }
        }
    }

    #endregion
}
