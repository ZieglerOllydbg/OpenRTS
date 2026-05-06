using System;
using UnityEngine;
using UnityEngine.UI;
using ZFrame;
using ZLib;

/// <summary>
/// 统一管理界面 管理生存周期
/// @author Ollydbg
/// @date 2018-2-23
/// </summary>
public abstract class BasePanel
{

    #region 加载完成通知

    /// <summary>
    /// 加载完成通知
    /// </summary>
    public Action<bool> OnLoadComplete;

    /// <summary>
    /// 界面打开回调
    /// </summary>
    public Action onDisplayCallback;

    #endregion

    #region 面板的一些通用信息

    /// <summary>
    /// 面板消息监听
    /// </summary>
    protected IDispathMessage processor;
    /// <summary>
    /// 定义了Panel的元数据
    /// </summary>
    public UIModelData ModelData { get; }
    /// <summary>
    /// 当前面板上绑定的Unity GameObject
    /// </summary>
    public GameObject PanelObject { get; protected set; }
    /// <summary>
    /// UIObject
    /// </summary>
    //protected UIObject uiObject;

    /// <summary>
    /// 是否已经销毁掉了
    /// </summary>
    protected bool isDisposed = false;

    #region 状态管理（使用状态机替代多个bool标志）

    /// <summary>
    /// 当前面板状态
    /// </summary>
    private PanelState state = PanelState.Uninitialized;

    /// <summary>
    /// 获取当前状态
    /// </summary>
    public PanelState State => state;

    /// <summary>
    /// 兼容属性：是否在打开状态（包括加载中、已加载、正在打开、已打开）
    /// </summary>
    public bool IsOpen => state == PanelState.Loading ||
                          state == PanelState.Loaded ||
                          state == PanelState.Opening ||
                          state == PanelState.Opened;

    /// <summary>
    /// 兼容属性：是否可见（已打开并显示）
    /// </summary>
    public bool IsVisiable => state == PanelState.Opened;

    /// <summary>
    /// 兼容属性：是否正在加载中
    /// </summary>
    public bool IsLoading => state == PanelState.Loading;

    /// <summary>
    /// 是否有GameObject
    /// </summary>
    public bool IsHasGameObject { get => PanelObject != null; }

    /// <summary>
    /// 状态转换（集中管理，便于调试和追踪）
    /// </summary>
    private void TransitionTo(PanelState newState)
    {
        // 状态转换合法性检查
        if (!IsValidTransition(state, newState))
        {
            this.LogError($"Invalid state transition: {state} -> {newState} for Panel: {ModelData.currentPanelID}");
            return;
        }

        var oldState = state;
        state = newState;

        this.Log($"[State] {oldState} -> {newState} (Panel: {ModelData.currentPanelID})");

        // 可选：发布状态变化事件
        OnStateChanged(oldState, newState);
    }

    /// <summary>
    /// 检查状态转换是否合法
    /// </summary>
    private bool IsValidTransition(PanelState from, PanelState to)
    {
        // 允许相同状态转换（幂等）
        if (from == to) return true;

        // 定义合法的状态转换
        switch (from)
        {
            case PanelState.Uninitialized:
                return to == PanelState.Closed;

            case PanelState.Closed:
                return to == PanelState.Loading || to == PanelState.Loaded;

            case PanelState.Loading:
                return to == PanelState.Loaded || to == PanelState.Closed;

            case PanelState.Loaded:
                return to == PanelState.Opening || to == PanelState.Closed;

            case PanelState.Opening:
                return to == PanelState.Opened || to == PanelState.Closing;

            case PanelState.Opened:
                return to == PanelState.Closing;

            case PanelState.Closing:
                return to == PanelState.Closed;

            default:
                return false;
        }
    }

    /// <summary>
    /// 状态变化事件（子类可重写）
    /// </summary>
    protected virtual void OnStateChanged(PanelState oldState, PanelState newState)
    {
        // 子类可以在这里监听状态变化
    }

    #endregion

    /// <summary>
    /// 界面皮肤SkinID
    /// </summary>
    public PanelSkinID panelSkinID
    {
        private set;
        get;
    } = PanelSkinID.None;
    /// <summary>
    /// 面板深度
    /// </summary>
    protected int depth = 0;

    //[JsonIgnore]
    //[NonSerialized]
    //protected HLData hlData;
    /// <summary>
    /// 抽象一个管理层次或者深度的逻辑
    /// </summary>
    public virtual int UIDepth
    {
        get { return depth; }
        set
        {
            depth = value;

            if (PanelObject)
            {
                var canvas = PanelObject.GetOrAddComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingOrder = depth;

                PanelObject.GetOrAddComponent<GraphicRaycaster>();
            }
        }
    }

    #endregion

    #region 构造方法

    /// <summary>
    /// 构造 BasePanel
    /// </summary>
    /// <param name="_processor"></param>
    /// <param name="_modelData"></param>
    /// <param name="_disableNew"></param>
    public BasePanel(IDispathMessage _processor, UIModelData _modelData, DisableNew _disableNew)
    {
        if (_disableNew == null)
            throw new Exception("Panel need Create By New extend method!");

        this.processor = _processor ?? throw new Exception("Panel need a Processor!");
        this.ModelData = _modelData ?? throw new Exception("Panel need have a modelData create by ModelDataAttribute!");

        OnInit();

        RegisterPanel();

        // 初始化完成后转换到Closed状态
        TransitionTo(PanelState.Closed);
    }

    #endregion

    #region 生命周期方法
    /// <summary>
    /// 指令点击打点数据
    /// </summary>
    //protected CommandClickDot dotMessage;
    /// <summary>
    /// Panel 初始化时调用
    /// </summary>
    protected virtual void OnInit()
    {

    }


    /// <summary>
    /// 在 PanelManager 中注册自己
    /// </summary>
    private void RegisterPanel()
        => PanelManager.instance.AddPanel(this);

    public virtual void Open()
    {
        this.Open(PanelSkinID.None);
    }

    /// <summary>
    /// 打开面板（使用状态机重构）
    /// </summary>
    public virtual void Open(PanelSkinID __panelSkinID = PanelSkinID.None)
    {
        switch (state)
        {
            case PanelState.Closed:
            case PanelState.Uninitialized:
                // 通知PanelManager
                PanelManager.instance.OpenPanel(this);

                // 检查是否需要加载/重新加载资源
                bool needReload = PanelObject == null ||
                                  (this.panelSkinID != PanelSkinID.None && __panelSkinID != PanelSkinID.None && this.panelSkinID != __panelSkinID);

                if (needReload)
                {
                    // 如果需要更换皮肤，先释放旧资源
                    if (PanelObject != null && this.panelSkinID != __panelSkinID)
                    {
                        CloseByActionType(true);
                    }

                    // 开始加载资源
                    this.panelSkinID = __panelSkinID;
                    TransitionTo(PanelState.Loading);
                    LoadUIPrefab(__panelSkinID);
                }
                else
                {
                    // 资源已存在，直接打开
                    this.panelSkinID = __panelSkinID;
                    TransitionTo(PanelState.Loaded);
                    BeginOpen();
                }
                break;

            case PanelState.Loading:
                // 正在加载中，什么都不做（等加载完成后自动打开）
                this.LogWarning($"Panel is loading, please wait... PanelID: {ModelData.currentPanelID}");
                break;

            case PanelState.Opened:
                // 已经打开，触发OnOpen回调
                OnOpen();
                break;

            case PanelState.Opening:
            case PanelState.Loaded:
                // 这些状态下重复调用Open，不做处理
                this.LogWarning($"Panel is already opening or loaded. State: {state}, PanelID: {ModelData.currentPanelID}");
                break;

            default:
                this.LogWarning($"Cannot open panel in state: {state}, PanelID: {ModelData.currentPanelID}");
                break;
        }
    }

    protected virtual void OnOpen() { }
    protected virtual void OnClose() { }

    /// <summary>
    /// 加载 Prefab
    /// </summary>
    protected virtual void LoadUIPrefab(PanelSkinID __panelSkinID = PanelSkinID.None)
    {
        // 状态已在Open()中设置为Loading，这里直接加载资源
        PanelManager.instance.BorrowUIRes(this.ModelData.currentPanelID, __panelSkinID, BorrowComplete);
    }

    /// <summary>
    /// 预加载UI资源
    /// </summary>
    public void PrestrainUIRes(PanelSkinID _panelSkinID, Action<string, PanelSkinID, GameObject> _prestrainComplete)
    {
        this.Log($"【BasePanel】 PrestrainUIRes 预加载资源 panelID:{this.ModelData?.currentPanelID}  _panelSkinID:{_panelSkinID}  _prestrainComplete:{_prestrainComplete}");

        if ((this.panelSkinID != PanelSkinID.None || _panelSkinID != PanelSkinID.None) && this.panelSkinID != _panelSkinID)
        {
            //如果与上一个类型不相同，需要卸载 然后重新加载
            //新传入的皮肤不是空
            //上一个类型
            if (PanelObject != null)
            {
                //这种情况下可以尝试直接打开
                //先根据配置，归还掉当前的界面,同步销毁UI 
                this.CloseByActionType(true);
            }

            //这里需要假设在Loading状态 走相同的逻辑
            //IsLoading = true;

            PanelManager.instance.BorrowUIRes(this.ModelData.currentPanelID, _panelSkinID, PrestrainBorrowComplete);
        }
        else
        {
            //这里需要假设在Loading状态 走相同的逻辑
            //IsLoading = true;
            // 如果仅仅是隐藏模式，直接尝试打开，否则重新加载
            if (PanelObject != null)
            {
                //这种情况下可以尝试直接打开
                PrestrainBorrowComplete(this.ModelData.currentPanelID, _panelSkinID, this.PanelObject);
            }
            else
            {
                //然后重新加载
                PanelManager.instance.BorrowUIRes(this.ModelData.currentPanelID, _panelSkinID, PrestrainBorrowComplete);
            }
        }

        this.panelSkinID = _panelSkinID;
        ///
        void PrestrainBorrowComplete(string __panelID, PanelSkinID __panelSkinID, GameObject __go)
        {
            this.Log($"【BasePanel】 PrestrainBorrowComplete 预加载资源，给回调");

            this.PanelObject = __go;

            this.PanelObject.SetActive(false);

            this.SetParent(PanelManager.instance.GetParentByPanelDepthType(this.ModelData.PanelUIDepthType));

            _prestrainComplete?.Invoke(__panelID, __panelSkinID, __go);
        }
    }

    /// <summary>
    /// 父类提供warmUp接口
    /// 核心实现思路是不改变打开关闭状态的情况下 准备好资源
    /// </summary>
    public void WarmUp()
    {
        ///warmup的UI自动切换为隐藏模式
        this.ModelData.RemoveActionType = PanelRemoveActionType.JustHide;

        if (!this.IsVisiable)
        {
            Open();
        }
    }


    /// <summary>
    /// 资源加载完成回调（异步）
    /// </summary>
    protected virtual void BorrowComplete(string _panelID, PanelSkinID __panelSkinID, GameObject _go)
    {
        if (_go == null)
        {
            // 加载失败
            this.LogError($"Load UI failed! PanelID: {ModelData.currentPanelID}");
            TransitionTo(PanelState.Closed);
            OnLoadComplete?.Invoke(false);
            Destroy();
            return;
        }

        // 设置PanelObject和基础属性
        this.PanelObject = _go;

        var rectTransform = this.PanelObject?.GetComponent<RectTransform>();
        // if (rectTransform) //发现有部分 UI 会自动修改这里的数据，强制纠正过来了。
        // {
        //     rectTransform.offsetMax = Vector2.zero;
        //     rectTransform.offsetMin = Vector2.zero;
        // }
        // 纠正会出问题，所以这里取消

        this.SetParent(PanelManager.instance.GetParentByPanelDepthType(this.ModelData.PanelUIDepthType));
        InitializePanelData();

        // 转换到Loaded状态
        TransitionTo(PanelState.Loaded);

        // 检查加载完成时面板是否仍需要打开
        if (state == PanelState.Closed)
        {
            // 加载期间被关闭了，直接释放资源
            this.LogWarning($"Panel was closed during loading. PanelID: {ModelData.currentPanelID}");
            CloseByActionType();
        }
        else
        {
            // 正常打开流程
            BeginOpen();
        }

        // 修复加载 UIPrefab 适配问题：调整尺寸和描点位置
        Transform parent = this.PanelObject.transform.parent;
        if (parent != null)
        {
            RectTransform parentRect = parent.GetComponent<RectTransform>();
            if (parentRect != null)
            {
                // 设置与父对象相同的尺寸
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, parentRect.rect.width);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, parentRect.rect.height);
                // 设置中心点为锚点位置（anchoredPosition 为 0 表示与锚点对齐）
                rectTransform.anchoredPosition = new Vector2(parentRect.rect.width / 2, parentRect.rect.height / 2);
            }
        }
    }

    /// <summary>
    /// 开始打开面板（资源已加载）
    /// </summary>
    private void BeginOpen()
    {
        TransitionTo(PanelState.Opening);

        SetUIReadyThenGetPanelComponent();

        // TODO: 播放打开动画
        // 动画完成后调用 OnOpenAnimationComplete()
        // 目前没有动画，直接完成

        OnOpenAnimationComplete();
    }

    /// <summary>
    /// 打开动画完成
    /// </summary>
    private void OnOpenAnimationComplete()
    {
        TransitionTo(PanelState.Opened);

        this.UIDepth = this.UIDepth;

        UpdateData();
        BecameVisible();

        OnLoadComplete?.Invoke(true);
    }

    /// <summary>
    /// 让UI准备好 并且获取GameObject上的组件
    /// </summary>
    protected void SetUIReadyThenGetPanelComponent()
        => PanelObject.SetActive(true);

    /// <summary>
    /// 初始化一些面板数据
    /// </summary>
    private void InitializePanelData()
    {
        if (this.PanelObject == null) return;

        //TODO 未来这里添加一些面板名字、Icon或者动画相关的数据，如果需要有的话
    }

    /// <summary>
    /// 返回去 UI
    /// </summary>
    /// <returns></returns>
    protected GameObject GiveBackUI()
    {
        var go = this.PanelObject;

        this.PanelObject = null;

        return go;
    }


    /// <summary>
    /// 刷新数据
    /// </summary>
    /// <returns></returns>
    public virtual bool UpdateData() => true;

    /// <summary>
    /// Panel显示触发
    /// </summary>
    protected void BecameVisible()
    {
        OnBecameVisible();

        OnInternational();

        PlayOpenSound();

        AddEvent();
        this.OnOpen();

        onDisplayCallback?.Invoke();
    }

    /// <summary>
    /// 国际化多语言设置
    /// </summary>
    protected virtual void OnInternational()
    {

    }

    /// <summary>
    /// 在Panel真正显示之前触发
    /// </summary>
    protected virtual void OnBecameVisible()
    {
        this.Log("OnBecameVisible >> " + ModelData.currentPanelID);
    }

    /// <summary>
    /// 添加事件
    /// </summary>
    protected virtual void AddEvent()
    { }

    /// <summary>
    /// 移除事件
    /// </summary>
    protected virtual void RemoveEvent()
    { }

    /// <summary>
    /// 窗口开启声音
    /// </summary>
    protected virtual void PlayOpenSound()
    { }

    /// <summary>
    /// 窗口关闭声音
    /// </summary>
    protected virtual void PlayCloseSound()
    { }

    /// <summary>
    /// Panel 关闭时触发
    /// </summary>
    protected void BecameInvisible()
    {
        this.OnClose();
        OnBecameInvisible();

        PlayCloseSound();

        RemoveEvent();
    }

    /// <summary>
    /// 在Panel真正关闭前触发
    /// </summary>
    protected virtual void OnBecameInvisible()
    {
        this.Log("OnBecameInvisible << " + ModelData.currentPanelID);
    }

    public virtual void Close()
    {
        this.Close(false);
    }

    /// <summary>
    /// 关闭面板（使用状态机重构）
    /// </summary>
    /// <param name="__isSynchronousDestory">是否立即销毁当前UI资源</param>
    public virtual void Close(bool __isSynchronousDestory = false)
    {
        this.Log($"BasePanel Close << PanelID: {ModelData.currentPanelID}, CurrentState: {state}");

        switch (state)
        {
            case PanelState.Opened:
                // 正常关闭流程
                TransitionTo(PanelState.Closing);
                BeginClose(__isSynchronousDestory);
                break;

            case PanelState.Loading:
                // 加载中被关闭，标记为需要关闭
                // 等加载完成后在BorrowComplete中会检查状态并关闭
                this.LogWarning($"Close during loading, will close after loaded. PanelID: {ModelData.currentPanelID}");
                TransitionTo(PanelState.Closed);
                break;

            case PanelState.Opening:
            case PanelState.Loaded:
                // 正在打开或已加载但未显示，直接关闭
                TransitionTo(PanelState.Closing);
                BeginClose(__isSynchronousDestory);
                break;

            case PanelState.Closed:
            case PanelState.Closing:
                // 已经关闭或正在关闭，不需要操作
#if UNITY_EDITOR
                this.LogWarning($"Panel is already closed or closing. State: {state}, PanelID: {ModelData.currentPanelID}");
#endif
                break;

            default:
                this.LogWarning($"Cannot close panel in state: {state}, PanelID: {ModelData.currentPanelID}");
                break;
        }
    }

    /// <summary>
    /// 开始关闭面板
    /// </summary>
    private void BeginClose(bool isSynchronousDestory = false)
    {
        this.OnLoadComplete = null;

        // 如果面板可见，触发不可见事件
        if (state == PanelState.Opened || state == PanelState.Closing)
        {
            BecameInvisible();
        }

        // TODO: 播放关闭动画
        // 动画完成后调用 OnCloseAnimationComplete()
        // 目前没有动画，直接完成

        OnCloseAnimationComplete(isSynchronousDestory);
    }

    /// <summary>
    /// 关闭动画完成
    /// </summary>
    private void OnCloseAnimationComplete(bool isSynchronousDestory = false)
    {
        TransitionTo(PanelState.Closed);

        CloseByActionType(isSynchronousDestory);
        PanelManager.instance.ClosePanel(this);
    }

    /// <summary>
    /// 根据配置类型决定操作
    /// </summary>
    private void CloseByActionType(bool __isSynchronousDestory = false)
    {
        switch (ModelData.RemoveActionType)
        {
            case PanelRemoveActionType.Default:

                if (PanelObject != null)
                    PanelManager.instance.GiveBackUIRes(this.ModelData.currentPanelID, this.panelSkinID, this.GiveBackUI(), __isSynchronousDestory);
                else
                    this.LogWarning($"PanelObject was null when the panel closed by removeActionType: { ModelData.RemoveActionType }");

                break;
            case PanelRemoveActionType.JustHide:

                if (PanelObject != null)
                {
                    PanelObject.SetActive(false);
                    //如果需要同步销毁之前的旧皮肤，
                    PanelManager.instance.DestoryPanelGameObject(__isSynchronousDestory, PanelObject);
                }
                else
                    this.LogWarning($"PanelObject was null when the panel closed by removeActionType: { ModelData.RemoveActionType }");

                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 销毁面板，从管理器中删除
    /// </summary>
    public virtual void Destroy()
    {
        PanelManager.instance.RemovePanel(this);

        //TODO 需要考虑哪些要销毁

        if (IsOpen)
            Close();

        if (PanelObject != null)
            PanelManager.instance.GiveBackUIRes(this.ModelData.currentPanelID, this.panelSkinID, this.GiveBackUI());


        isDisposed = true;

        OnDestory();
    }

    /// <summary>
    /// 析构时调用
    /// </summary>
    protected virtual void OnDestory()
    { }

    #endregion

    /// <summary>
    /// 设置 Parent
    /// </summary>
    /// <param name="_parent"></param>
    public void SetParent(Transform _parent)
        => this.PanelObject.transform.SetParent(_parent, false);

    #region 重载比较操作符 允许释放 参考 Unity GameObject 的释放方式

    /// <summary>
    /// 比较两个 Panel 对象是否相同
    /// </summary>
    /// <param name="_panelA"></param>
    /// <param name="_panelB"></param>
    /// <returns></returns>
    private static bool CompareObjects(BasePanel _panelA, BasePanel _panelB)
    {
        bool flag1 = object.Equals(_panelA, null);

        bool flag2 = object.Equals(_panelB, null);

        //两个都为空，返回 true
        if (flag1 && flag2) return true;

        //panelA 不为空 panelB为空 但是 panelA 销毁掉了 就相等
        if (!flag1)
        {
            if (flag2)
            {
                if (_panelA.isDisposed)
                    return true;
            }
        }

        //panelB 一样
        if (!flag2)
        {
            if (flag1)
            {
                if (_panelB.isDisposed)
                    return true;
            }
        }

        //这种情况是panelA和PanelB都没有被销毁 也有可能为空 直接比对即可
        return object.ReferenceEquals(_panelA, _panelB);
    }

    /// <summary>
    /// 重载操作符判定是否相等
    /// </summary>
    /// <param name="_panelA"></param>
    /// <param name="_panelB"></param>
    /// <returns></returns>
    public static bool operator ==(BasePanel _panelA, BasePanel _panelB)
        => CompareObjects(_panelA, _panelB);

    /// <summary>
    /// 重载操作符判定是否相等
    /// </summary>
    /// <param name="_panelA"></param>
    /// <param name="_panelB"></param>
    /// <returns></returns>
    public static bool operator !=(BasePanel _panelA, BasePanel _panelB)
        => !CompareObjects(_panelA, _panelB);

    /// <summary>
    /// 重载强制转化为 bool
    /// </summary>
    /// <param name="_panel"></param>
    public static implicit operator bool(BasePanel _panel)
        => !CompareObjects(_panel, null);

    /// <summary>
    /// 判断是否相同
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object obj)
    {
        if (obj == null) return false;

        if (obj is BasePanel panel)
        {
            return panel == null ? false : CompareObjects(this, panel);
        }

        return false;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    #endregion
}
