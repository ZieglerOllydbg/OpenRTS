using UnityEngine;
using zUnity;

namespace ZLockstep.View
{
    /// <summary>
    /// 确定性数学类型与Unity类型的转换扩展
    /// </summary>
    public static class MathConversionExtensions
    {
        // ========== zVector3 <-> Vector3 ==========

        public static Vector3 ToVector3(this zVector3 v)
        {
            return new Vector3((float)v.x, (float)v.y, (float)v.z);
        }

        public static zVector3 ToZVector3(this Vector3 v)
        {
            return new zVector3((zfloat)v.x, (zfloat)v.y, (zfloat)v.z);
        }

        // ========== zVector2 <-> Vector2 ==========

        public static Vector2 ToVector2(this zVector2 v)
        {
            return new Vector2((float)v.x, (float)v.y);
        }

        public static zVector2 ToZVector2(this Vector2 v)
        {
            return new zVector2((zfloat)v.x, (zfloat)v.y);
        }

        // ========== zQuaternion <-> Quaternion ==========

        public static Quaternion ToQuaternion(this zQuaternion q)
        {
            return new Quaternion((float)q.x, (float)q.y, (float)q.z, (float)q.w);
        }

        public static zQuaternion ToZQuaternion(this Quaternion q)
        {
            return new zQuaternion((zfloat)q.x, (zfloat)q.y, (zfloat)q.z, (zfloat)q.w);
        }

        // ========== zfloat <-> float ==========

        public static float ToFloat(this zfloat z)
        {
            return (float)z;
        }

        public static zfloat ToZFloat(this float f)
        {
            return (zfloat)f;
        }

        // ========== 辅助方法 ==========

    }
}

