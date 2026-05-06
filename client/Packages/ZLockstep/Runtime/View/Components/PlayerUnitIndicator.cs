using UnityEngine;
using ZLockstep.View.Systems;


namespace ZLockstep.View.Components
{
    /// <summary>
    /// 玩家单位指示器组件
    /// 用于区分本地玩家单位和敌方单位的视觉表现
    /// </summary>
    public class PlayerUnitIndicator : MonoBehaviour
    {
        private Renderer[] _renderers;
        private int _unitPlayerId = -1;

        private void Awake()
        {
            // 获取Camp子对象的所有渲染器组件（可能在多层嵌套中）
            Transform campTransform = FindDeepChild(gameObject.transform, "Camp");
            if (campTransform != null)
            {
                _renderers = campTransform.gameObject.GetComponentsInChildren<Renderer>(true);
            }
            else
            {
                // 获取所有渲染器组件
                _renderers = GetComponentsInChildren<Renderer>(true);
                Debug.LogWarning("未能找到名为'Camp'的子对象", this);
            }
        }

        /// <summary>
        /// 深度查找子对象（递归查找所有层级）
        /// </summary>
        /// <param name="parent">父级变换组件</param>
        /// <param name="name">要查找的对象名称</param>
        /// <returns>找到的变换组件或null</returns>
        private Transform FindDeepChild(Transform parent, string name)
        {
            // 先在直接子对象中查找
            Transform result = parent.Find(name);
            if (result != null)
                return result;

            // 如果没找到，则递归查找每个子对象的子对象
            foreach (Transform child in parent)
            {
                result = FindDeepChild(child, name);
                if (result != null)
                    return result;
            }

            return null;
        }

        /// <summary>
        /// 初始化指示器
        /// </summary>
        /// <param name="localPlayerId">本地玩家ID</param>
        /// <param name="unitPlayerId">单位所属玩家ID</param>
        public void Initialize(int unitPlayerId)
        {
            _unitPlayerId = unitPlayerId;
            
            UpdateVisualIndication();
        }

        /// <summary>
        /// 更新视觉指示
        /// </summary>
        public void UpdateVisualIndication()
        {
            if (_unitPlayerId == -1)
                return;

            Color targetColor = GetCampColor(_unitPlayerId);
            ApplyColorToRenderers(targetColor);
        }

        /// <summary>
        /// 应用颜色到所有渲染器
        /// </summary>
        /// <param name="color">目标颜色</param>
        private void ApplyColorToRenderers(Color color)
        {
            if (_renderers == null)
                return;

            foreach (var renderer in _renderers)
            {
                if (renderer != null && renderer.material != null)
                {
                    renderer.material.color = color;
                }
            }
        }

        /// <summary>
        /// 根据阵营ID获取对应颜色
        /// </summary>
        /// <param name="campId">阵营ID</param>
        /// <returns>阵营对应的颜色</returns>
        private Color GetCampColor(int campId)
        {
            switch (campId)
            {
                case (int)CampColor.Red:
                    return Color.red;
                case (int)CampColor.Blue:
                    return Color.blue;
                case (int)CampColor.Green:
                    return Color.green;
                case (int)CampColor.Yellow:
                    return Color.yellow;
                case (int)CampColor.Orange:
                    return new Color(1.0f, 0.5f, 0.0f); // 橙色
                case (int)CampColor.Purple:
                    return new Color(0.5f, 0.0f, 1.0f); // 紫色
                case (int)CampColor.Pink:
                    return new Color(1.0f, 0.4f, 0.7f); // 粉色
                case (int)CampColor.Brown:
                    return new Color(0.6f, 0.3f, 0.0f); // 棕色
                default: // 默认使用白色
                    return Color.white;
            }
        }
    }
}