using UnityEngine;
using ZLockstep.Simulation.ECS;

namespace ZLockstep.View
{
    /// <summary>
    /// 表现层绑定组件（引用Unity GameObject）
    /// 注意：这是唯一引用Unity的组件，只在表现层使用
    /// 不参与逻辑计算，仅用于显示
    /// </summary>
    public class ViewComponent : IComponent
    {
        /// <summary>
        /// 关联的Unity GameObject
        /// </summary>
        public GameObject GameObject;

        public bool BuildingOK;

        /// <summary>
        /// Transform缓存（性能优化）
        /// </summary>
        public Transform Transform;

        public Transform ForwardTransform;
        public int LastAppliedAttackTick;
        public float LastAttackTime;

        /// <summary>
        /// 动画控制器（如果有）
        /// </summary>
        public Animator Animator;

        /// <summary>
        /// 渲染器（如果有）
        /// </summary>
        public Renderer Renderer;

        // --- 插值相关（用于平滑表现） ---

        /// <summary>
        /// 上一帧的逻辑位置
        /// </summary>
        public Vector3 LastLogicPosition;

        /// <summary>
        /// 上一帧的逻辑旋转
        /// </summary>
        public Quaternion LastLogicRotation;

        /// <summary>
        /// 是否启用插值
        /// </summary>
        public bool EnableInterpolation;

        /// <summary>
        /// 插值总时间（秒），默认 0.25 秒
        /// </summary>
        public float InterpolationDuration = 0.05f;

        /// <summary>
        /// 当前插值已用时间
        /// </summary>
        public float CurrentInterpolationTime;

        /// <summary>
        /// 创建 ViewComponent
        /// </summary>
        public static ViewComponent Create(GameObject gameObject, bool enableInterpolation = false)
        {
            var view = new ViewComponent
            {
                GameObject = gameObject,
                BuildingOK = false,
                Transform = gameObject.transform,
                ForwardTransform = FindChildRecursive(gameObject.transform, "Forward"),
                LastAppliedAttackTick = -1,
                LastAttackTime = float.NegativeInfinity,
                Animator = gameObject.GetComponent<Animator>(),
                Renderer = gameObject.GetComponent<Renderer>(),
                EnableInterpolation = enableInterpolation,
                LastLogicPosition = gameObject.transform.position,
                LastLogicRotation = gameObject.transform.rotation,
                InterpolationDuration = 0.05f,
                CurrentInterpolationTime = 0f
            };

            return view;
        }

        public void RefreshTransformCache()
        {
            if (GameObject == null)
            {
                Transform = null;
                ForwardTransform = null;
                Animator = null;
                Renderer = null;
                return;
            }

            Transform = GameObject.transform;
            ForwardTransform = FindChildRecursive(Transform, "Forward");
            Animator = GameObject.GetComponent<Animator>();
            Renderer = GameObject.GetComponent<Renderer>();
        }

        private static Transform FindChildRecursive(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name))
            {
                return null;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.name == name)
                {
                    return child;
                }

                var found = FindChildRecursive(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        /// <summary>
        /// 销毁GameObject
        /// </summary>
        public void Destroy()
        {
            if (GameObject != null)
            {
                Object.Destroy(GameObject);
                GameObject = null;
                Transform = null;
                ForwardTransform = null;
                Animator = null;
                Renderer = null;
            }
        }
    }
}

