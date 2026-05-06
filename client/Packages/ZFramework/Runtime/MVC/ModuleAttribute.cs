using System;

namespace ZFrame
{
    /// <summary>
    /// 辅助实现模块的发现机制
    /// @author Ollydbg
    /// @date 2018-12-31
    /// </summary>
    public class ModuleAttribute : Attribute
    {
        /// <summary>
        /// 模块名字
        /// </summary>
        public string moduleName;

        /// <summary>
        /// 模块注册顺序
        /// </summary>
        public int order = 255;

        public ModuleAttribute(string _moduleName)
        {
            this.moduleName = _moduleName;
        }
    }
}
