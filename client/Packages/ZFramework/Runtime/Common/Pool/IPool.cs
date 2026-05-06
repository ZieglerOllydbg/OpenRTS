using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ZLib
{
    /// <summary>
    /// 对象池对象接口
    /// </summary>
    public interface IPool
    {
        /// <summary>
        /// 释放
        /// </summary>
        void Dispose();

        /// <summary>
        /// 重置
        /// </summary>
        /// <param name="__objs"></param>
        void ReSet(params object[] __objs);
    }
}