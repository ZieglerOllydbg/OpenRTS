using UnityEngine;
using TMPro; // 添加TextMeshPro命名空间
using ZFrame;
using UnityEngine.UI;

/// <summary>
/// 加载面板 - 显示加载进度和玩家数量
/// </summary>
[UIModel(
    panelID = "LoadingPanel",
    panelPath = "LoadingPanel",
    panelName = "加载面板",
    panelUIDepthType = ClientUIDepthTypeID.GameTop
)]
public class LoadingPanel : BasePanel
{
    // 声明UI组件引用
    private Image _image;
    private Transform _numPlayer;
    private TMP_Text _value; // 改为TextMeshPro
    
    public LoadingPanel(IDispathMessage _processor, UIModelData _modelData, DisableNew _disableNew) 
        : base(_processor, _modelData, _disableNew)
    {
    }

    protected override void OnBecameVisible()
    {
        base.OnBecameVisible();
        
        // 获取Image组件
        _image = PanelObject.transform.Find("Loading/Image")?.GetComponent<Image>();
        
        // 获取NumPlayer容器和其子组件
        _numPlayer = PanelObject.transform.Find("Loading/NumPlayer");
        zUDebug.Log("[LoadingPanel] 获取NumPlayer容器 " + _numPlayer);            
        _value = _numPlayer?.Find("Value")?.GetComponentInChildren<TMP_Text>(); // 改为TextMeshPro
    }

    protected override void AddEvent()
    {
        base.AddEvent();
        // 可以在这里添加事件监听
    }

    protected override void RemoveEvent()
    {
        base.RemoveEvent();
        // 可以在这里移除事件监听
    }

    /// <summary>
    /// 设置加载进度
    /// </summary>
    /// <param name="progress">进度值 (0-1)</param>
    public void SetProgress(float progress)
    {
        if (_image != null)
        {
            _image.fillAmount = progress;
        }
    }

    /// <summary>
    /// 设置玩家数量
    /// </summary>
    /// <param name="playerCount">玩家数量</param>
    public void SetPlayerCount(int playerCount)
    {
        if (_value != null)
        {
            _value.text = playerCount.ToString();
        }
    }
}