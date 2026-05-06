using System.Collections;
using System.Collections.Generic;
using PostHogUnity;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using ZFrame;

#if UNITY_WEBGL || WEIXINMINIGAME
using WeChatWASM;

public class LoginWX : MonoBehaviour
{
    /// <summary>
    /// 用户信息
    /// 获取到用户信息后可根据需要存云端或本地持久存储
    /// </summary>
    public WXUserInfo userInfo;

    /// <summary>
    /// 是否获取用户信息
    /// </summary>
    private bool infoFlag = false;

    /// <summary>
    /// 头像材质
    /// </summary>
    public Texture2D avatarTexture;

    // Start is called before the first frame update
    void Start()
    {
        if (!Application.isEditor)
        {
            // 初始化微信 SDK
            WX.InitSDK((code) =>
            {
                zUDebug.Log("init WXSDK code: " + code);
                // 加载用户信息
                this.LoaderWXMess();
            });
        }
    }

    /// <summary>
    /// 加载微信授权相关信息
    /// </summary>
    public void LoaderWXMess()
    {
        // 1. 询问隐私协议授权情况
        WX.GetPrivacySetting(new GetPrivacySettingOption()
        {
            success = (res) =>
            {
                /**
                 * needAuthorization - 含义
                 * 是否需要用户授权隐私协议（如果开发者没有在[mp后台-设置-服务内容声明-用户隐私保护指引]中声明隐私收集类型则会返回false；
                 * 如果开发者声明了隐私收集，且用户之前同意过隐私协议则会返回false；
                 * 如果开发者声明了隐私收集，且用户还没同意过则返回true；
                 * 如果用户之前同意过、但后来小程序又新增了隐私收集类型也会返回true）
                 */
                // 询问成功
                if (res.needAuthorization)
                {
                    // 有隐私协议，且未授权
                    // 2. 发起隐私协议授权
                    // 弹出隐私协议询问弹窗
                    WX.RequirePrivacyAuthorize(new RequirePrivacyAuthorizeOption()
                    {
                        success = (res) =>
                        {
                            zUDebug.Log("同意隐私协议：" + JsonUtility.ToJson(res, true));
                            // 用户同意隐私协议
                            // 3. 获取用户信息
                            this.GetScopeInfoSetting();
                            // 将 信息获取标志 标记为true
                            this.infoFlag = true;
                        },
                        fail = (err) =>
                        {
                            zUDebug.Log("拒绝隐私协议：" + JsonUtility.ToJson(res, true));
                        },
                        complete = (res) =>
                        {
                            zUDebug.Log("询问隐私协议结束");
                        }
                    });
                }
            },
            fail = (err) => { },
            complete = (res) =>
            {
                // 处理询问隐私协议失败或之前已经同意但未授权用户信息的情况
                if (!this.infoFlag)
                {
                    this.GetScopeInfoSetting();
                }
            }
        });
    }


    /// <summary>
    /// 点击开始游戏
    /// </summary>
    public void StartGame()
    {
        zUDebug.Log("start game");
        zUDebug.Log("用户信息：" + JsonUtility.ToJson(this.userInfo, true));
    }

    /// <summary>
    /// 获取授权
    /// </summary>
    private void GetScopeInfoSetting()
    {
        // 询问用户信息授权情况
        WX.GetSetting(new GetSettingOption()
        {
            success = (res) =>
            {
                zUDebug.Log("获取用户信息授权情况成功: " + JsonUtility.ToJson(res.authSetting, true));
                // 判断用户信息的授权情况
                if (!res.authSetting.ContainsKey("scope.userInfo") || !res.authSetting["scope.userInfo"])
                {
                    // 3.1 未授权，创建授权按钮区
                    // 需引导用户点击所创建的区域，这里的做法是将开始游戏的按钮放在该区域
                    this.CreateUserInfoButton();
                }
                else
                {
                    // 3.2 已授权，直接获取用户信息
                    this.GetUserInfo();
                    // 这里也可以先不获取，留到点击开始游戏按钮再获取，但没必要，先获取后存起来即可
                }
            },
            fail = (err) =>
            {
                zUDebug.Log("获取用户信息授权情况失败：" + JsonUtility.ToJson(err, true));
            }
        });
    }

    /// <summary>
    /// 创建用户信息授权点击区域
    /// </summary>
    private void CreateUserInfoButton()
    {
        zUDebug.Log("create userinfo button area");

        /**
         * 方法一：创建用户信息获取按钮，在底部区域创建一个300高度的 透明！！！ 区域
         * 首次获取会弹出用户授权窗口, 可通过右上角-设置-权限管理用户的授权记录
         * 可根据需要设置不同高度，使用屏幕还有其它点击热区的情况
         */
        // 获取屏幕信息
        // var systemInfo = WX.GetSystemInfoSync();
        // var canvasWith = (int)(systemInfo.screenWidth * systemInfo.pixelRatio);
        // var canvasHeight = (int)(systemInfo.screenHeight * systemInfo.pixelRatio);
        // var buttonHeight = (int)(canvasWith / 1080f * 300f);
        // 很容易被误导，与其叫按钮，不如叫热区
        // WXUserInfoButton btn = WX.CreateUserInfoButton(0, canvasHeight - buttonHeight, canvasWith, buttonHeight, "zh_CN", false);
        /**
         * 方法二：创建布满整个屏幕的授权按钮区
         */
        WXUserInfoButton btn = WX.CreateUserInfoButton(0, 0, Screen.width, Screen.height, "zh_CN", false);
        // 监听授权区域的点击
        btn.OnTap((res) =>
        {
            zUDebug.Log("click userinfo btn: " + JsonUtility.ToJson(res, true));
            if (res.errCode == 0)
            {
                // 用户已允许获取个人信息，返回的 res.userInfo 即为用户信息
                zUDebug.Log("userinfo: " + JsonUtility.ToJson(res.userInfo, true));
                // 将用户信息存入成员变量，以待后用
                this.userInfo = res.userInfo;
                // 展示，只是为了测试看到
                this.ShowUserInfo(res.userInfo.avatarUrl, res.userInfo.nickName);
            }
            else
            {
                zUDebug.Log("用户拒绝获取个人信息");
            }
            // 最后隐藏授权区域，防止阻塞游戏继续
            btn.Hide();
            zUDebug.Log("已隐藏热区");
        });
    }

    /// <summary>
    /// 调用Api获取用户信息
    /// </summary>
    private void GetUserInfo()
    {
        WX.GetUserInfo(new GetUserInfoOption()
        {
            lang = "zh_CN",
            success = (res) =>
            {
                zUDebug.Log("获取用户信息成功(API): " + JsonUtility.ToJson(res.userInfo, true));
                // 将用户信息存入成员变量，或存入云端，方便后续使用
                this.userInfo = this.ConvertUserInfo(res.userInfo);

                this.ShowUserInfo(res.userInfo.avatarUrl, res.userInfo.nickName);
            },
            fail = (err) =>
            {
                zUDebug.Log("获取用户信息失败(API): " + JsonUtility.ToJson(err, true));
            }
        });
    }

    /// <summary>
    /// 展示用户信息，对头像、昵称展示的整合
    /// ps: 测试用
    /// </summary>
    /// <param name="avatarUrl"></param>
    /// <param name="nickName"></param>
    private void ShowUserInfo(string avatarUrl, string nickName)
    {
        Frame.DispatchEvent(new LoginEvent(nickName, avatarUrl));
    }



    /// <summary>
    /// 将UserInfo对象转为WXUserInfo
    /// ps: 不知为何，相同结构要搞两个对象
    /// </summary>
    /// <param name="userInfo"></param>
    /// <returns></returns>
    WXUserInfo ConvertUserInfo(UserInfo userInfo)
    {
        return new WXUserInfo()
        {
            nickName = userInfo.nickName,
            avatarUrl = userInfo.avatarUrl,
            country = userInfo.country,
            province = userInfo.province,
            city = userInfo.city,
            language = userInfo.language,
            gender = (int)userInfo.gender
        };
    }
}
#endif
