using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ZFrame;

/// <summary>
/// UI 音效管理器 - 负责播放 UI 点击等音效
/// </summary>
public class UISound
{
    private static UISound instance;
    private AudioSource uiAudioSource;
    private AudioClip clickClip;
    
    /// <summary>
    /// 单例实例
    /// </summary>
    public static UISound Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new UISound();
                instance.Initialize();
            }
            return instance;
        }
    }
    
    /// <summary>
    /// 初始化音效系统
    /// </summary>
    private async Task Initialize()
    {
        // 查找 UIRoot 对象
        GameObject uiRoot = GameObject.Find("UIRoot");
        if (uiRoot != null)
        {
            // 获取或添加 AudioSource 组件
            uiAudioSource = uiRoot.GetComponent<AudioSource>();
            if (uiAudioSource == null)
            {
                uiAudioSource = uiRoot.AddComponent<AudioSource>();
            }
            
            // 配置 AudioSource 属性
            uiAudioSource.playOnAwake = false;
            uiAudioSource.loop = false;
            uiAudioSource.spatialBlend = 0f; // 2D 声音
            
            zUDebug.Log("[UISound] 已找到 UIRoot 并配置 AudioSource");
        }
        else
        {
            zUDebug.LogWarning("[UISound] 未找到 UIRoot 对象");
        }
        
        // 加载点击音效
        clickClip = await AssetManager.GetAudioClipAsync("Audio/click-clear");
        if (clickClip != null)
        {
            zUDebug.Log("[UISound] 点击音效加载成功");
        }
        else
        {
            zUDebug.LogWarning("[UISound] 点击音效加载失败：Audio/ui-click");
        }
    }
    
    /// <summary>
    /// 播放点击音效
    /// </summary>
    public void PlayClick()
    {
        if (clickClip == null)
        {
            zUDebug.LogWarning("[UISound] 点击音效未加载");
            return;
        }
        
        if (uiAudioSource == null)
        {
            zUDebug.LogWarning("[UISound] AudioSource 未初始化");
            return;
        }
        
        // 使用 PlayOneShot 播放音效，避免中断其他声音
        uiAudioSource.PlayOneShot(clickClip);
    }
    
    /// <summary>
    /// 播放指定音效
    /// </summary>
    /// <param name="clipName">音效资源名称（路径相对于 Audio 文件夹）</param>
    public async void PlaySound(string clipName)
    {
        AudioClip clip = await AssetManager.GetAudioClipAsync("Audio/" + clipName);
        if (clip != null && uiAudioSource != null)
        {
            uiAudioSource.PlayOneShot(clip);
        }
        else
        {
            zUDebug.LogWarning($"[UISound] 音效加载失败：{clipName}");
        }
    }
    
    /// <summary>
    /// 设置 UI 音效音量
    /// </summary>
    /// <param name="volume">音量值 (0-1)</param>
    public void SetVolume(float volume)
    {
        if (uiAudioSource != null)
        {
            uiAudioSource.volume = Mathf.Clamp01(volume);
        }
    }
    
    /// <summary>
    /// 获取当前音量
    /// </summary>
    /// <returns>当前音量值</returns>
    public float GetVolume()
    {
        return uiAudioSource?.volume ?? 1f;
    }
}
