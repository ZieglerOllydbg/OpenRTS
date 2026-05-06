using UnityEngine;
using UnityEngine.UI;
using ZFrame;
using TMPro;
using zUnity;

/// <summary>
/// 登录面板 - 简化的登录界面
/// </summary>
[UIModel(
    panelID = "LoginPanel",
    panelPath = "LoginPanel",
    panelName = "登录面板",
    panelUIDepthType = ClientUIDepthTypeID.GameMid
)]
public class LoginPanel : BasePanel
{
    // 1. 声明 UI 组件引用
    private Button _loginButton;
    
    public LoginPanel(IDispathMessage _processor, UIModelData _modelData, DisableNew _disableNew) 
        : base(_processor, _modelData, _disableNew)
    {
    }

    // 2. 在面板显示前获取组件引用
    protected override void OnBecameVisible()
    {
        base.OnBecameVisible();
        
        // 获取 Login 按钮组件
        _loginButton = PanelObject.transform.Find("Login/LoginBtn")?.GetComponent<Button>();
        
        zUDebug.Log("[LoginPanel] 面板已显示");
    }

    // 3. 在 AddEvent 中添加按钮事件（面板显示时自动调用）
    protected override void AddEvent()
    {
        base.AddEvent();
        
        // 为 Login 按钮添加点击事件监听
        if (_loginButton != null)
        {
            _loginButton.onClick.AddListener(OnLoginButtonClick);
            zUDebug.Log("[LoginPanel] 登录按钮事件已绑定");
        }
        else
        {
            zUDebug.LogWarning("[LoginPanel] 未找到登录按钮组件");
        }
    }

    // 4. 在 RemoveEvent 中移除按钮事件（面板关闭时自动调用）
    protected override void RemoveEvent()
    {
        base.RemoveEvent();
        
        // 移除 Login 按钮的点击事件监听
        if (_loginButton != null)
        {
            _loginButton.onClick.RemoveListener(OnLoginButtonClick);
            zUDebug.Log("[LoginPanel] 登录按钮事件已移除");
        }
    }

    // 5. 在面板隐藏时清理资源
    protected override void OnBecameInvisible()
    {
        base.OnBecameInvisible();
        zUDebug.Log("[LoginPanel] 面板已隐藏");
    }
    
    private const string NICKNAME_PREF_KEY = "GuestNickname";

    // Login 按钮点击处理方法
    private void OnLoginButtonClick()
    {
        zUDebug.Log("[LoginPanel] 登录按钮被点击");

        // TODO: 实现登录逻辑
        // 示例：触发登录事件或调用登录接口
#if UNITY_WEBGL || WEIXINMINIGAME
        // 微信环境登录（非安卓平台）
        LoginWX loginWX = Object.FindObjectOfType<LoginWX>();
        if (loginWX != null)
        {
            loginWX.LoaderWXMess();
        }
        else
        {
            zUDebug.LogError("[MatchPanel] 未找到 LoginWX 组件");
        }
#else
        string nickName = GetOrCreateGuestNickname();
        string avatarUrl = "https://thirdwx.qlogo.cn/mmopen/vi_32/Q0j4TwGTfTLXlC8Ynp4rPicm4icSMDic9cXJzfS4abSzRdWMraKrO8o6kiap7EsjPEXL8jiaPphXoOmLKr5QPWDMNBQ/132";

        Frame.DispatchEvent(new LoginEvent(nickName, avatarUrl));
#endif

    }

    /// <summary>
    /// 获取或创建游客昵称（持久化保存）
    /// </summary>
    private string GetOrCreateGuestNickname()
    {
        string savedNickname = PlayerPrefs.GetString(NICKNAME_PREF_KEY, "");
        if (!string.IsNullOrEmpty(savedNickname))
        {
            return savedNickname;
        }

        // 生成"游客"+4位随机数字
        int randomNumber = Random.Range(1000, 10000);
        string newNickname = $"游客{randomNumber}";
        PlayerPrefs.SetString(NICKNAME_PREF_KEY, newNickname);
        PlayerPrefs.Save();
        zUDebug.LogInfo($"[LoginPanel] 生成新游客昵称: {newNickname}");
        return newNickname;
    }
    
    /// <summary>
    /// 判断是否为安卓平台
    /// </summary>
    /// <returns></returns>
    private bool IsAndroidPlatform()
    {
#if UNITY_ANDROID
        return true;
#else
        return false;
#endif
    }
}
