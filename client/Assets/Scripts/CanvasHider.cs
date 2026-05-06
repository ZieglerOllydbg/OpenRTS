using UnityEngine;

/// <summary>
/// 隐藏当前Canvas的所有子Canvas
/// 将此脚本挂载到Canvas上，每帧检测并隐藏所有子Canvas
/// </summary>
public class CanvasHider : MonoBehaviour
{
    private Canvas parentCanvas;

    private void Awake()
    {
        // 获取当前GameObject上的Canvas组件
        parentCanvas = GetComponent<Canvas>();
        
        if (parentCanvas == null)
        {
            Debug.LogWarning("CanvasHider: 未找到Canvas组件！请将此脚本挂载到Canvas上");
        }
    }

    private void Update()
    {
        if (parentCanvas != null)
        {
            // 获取当前Canvas的所有子Canvas组件
            Canvas[] childCanvases = parentCanvas.GetComponentsInChildren<Canvas>(true);
            
            // 隐藏所有子Canvas（排除当前Canvas本身）
            foreach (Canvas childCanvas in childCanvases)
            {
                if (childCanvas != parentCanvas && childCanvas.gameObject.activeSelf)
                {
                    childCanvas.gameObject.SetActive(false);
                }
            }
        }
    }
}
