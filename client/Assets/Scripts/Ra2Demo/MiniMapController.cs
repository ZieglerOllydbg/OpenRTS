using UnityEngine;

/// <summary>
/// 小地图控制器，用于管理小地图相机和渲染纹理
/// </summary>
public class MiniMapController : MonoBehaviour
{
    [Header("小地图设置")]
    [SerializeField] private Camera mainCamera; // 主相机
    [SerializeField] private Camera miniMapCamera; // 小地图相机
    [SerializeField] private RenderTexture miniMapTexture; // 小地图渲染纹理
    [SerializeField] private int mapSize = 256; // 小地图纹理大小
    
    [Header("相机设置")]
    [SerializeField] private float cameraHeight = 50f; // 小地图相机高度
    [SerializeField] private float cameraTilt = 90f; // 小地图相机倾斜角度
    
    // 固定的小地图场景范围 (minX, minY, maxX, maxY)
    private readonly Vector4 MAP_BOUNDS = new Vector4(0, 0, 256, 256);
    
    private void Awake()
    {
        InitializeMiniMapSystem();
    }
    
    /// <summary>
    /// 初始化小地图系统
    /// </summary>
    private void InitializeMiniMapSystem()
    {
        // 如果没有分配主相机，尝试获取主相机
        if (mainCamera == null)
            mainCamera = Camera.main;
            
        // 创建小地图相机
        CreateMiniMapCamera();
        
        // 创建渲染纹理
        CreateRenderTexture();
        
        // 配置小地图相机
        SetupMiniMapCamera();
    }
    
    /// <summary>
    /// 创建小地图相机
    /// </summary>
    private void CreateMiniMapCamera()
    {
        if (miniMapCamera == null)
        {
            // 创建小地图相机对象
            GameObject miniMapCameraObject = new GameObject("MiniMapCamera");
            miniMapCamera = miniMapCameraObject.AddComponent<Camera>();
            
            // 设置为子对象，便于管理
            miniMapCameraObject.transform.SetParent(transform);
        }
    }
    
    /// <summary>
    /// 创建渲染纹理
    /// </summary>
    private void CreateRenderTexture()
    {
        if (miniMapTexture == null)
        {
            miniMapTexture = new RenderTexture(mapSize, mapSize, 16, RenderTextureFormat.ARGB32);
            miniMapTexture.Create();
        }
    }
    
    /// <summary>
    /// 配置小地图相机
    /// </summary>
    private void SetupMiniMapCamera()
    {
        if (miniMapCamera != null && miniMapTexture != null)
        {
            // 设置小地图相机参数
            miniMapCamera.targetTexture = miniMapTexture;
            miniMapCamera.clearFlags = CameraClearFlags.SolidColor;
            miniMapCamera.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f); // 深灰色背景
            miniMapCamera.orthographic = true;
            miniMapCamera.orthographicSize = 64f; // 固定场景范围(0-128)的一半
            miniMapCamera.cullingMask = ~(1 << 4); // 不渲染UI层
            
            // 设置相机位置和角度（固定视角俯视整个地图区域）
            miniMapCamera.transform.position = new Vector3(64, cameraHeight, 64); // 地图中心点(128,128)
            miniMapCamera.transform.eulerAngles = new Vector3(cameraTilt, 0, 0);
        }
    }
    
    /// <summary>
    /// 获取小地图渲染纹理
    /// </summary>
    public RenderTexture GetMiniMapTexture()
    {
        return miniMapTexture;
    }
    
    /// <summary>
    /// 获取小地图相机
    /// </summary>
    public Camera GetMiniMapCamera()
    {
        return miniMapCamera;
    }
    
    /// <summary>
    /// 更新小地图相机位置以匹配主相机
    /// </summary>
    public void UpdateMiniMapPosition()
    {
        // 小地图相机是固定视角，不需要更新位置
        // 这个方法保留以避免Ra2Demo.cs中的调用出错
    }
    
    /// <summary>
    /// 获取地图尺寸信息
    /// </summary>
    public Vector4 GetMapBounds()
    {
        return MAP_BOUNDS; // minX, minY, maxX, maxY
    }
}