namespace ZLib
{
    /// <summary>
    /// 支持消息的冒泡机制
    /// 不同于一般的js,as里面的冒泡机制 我们的冒泡机制是可以恢复的，也就是说消息保存以后可以通过ResumePropagation机制再次发出去继续处理
    /// @author Ollydbg
    /// @date 2019-6-18
    /// </summary>
    internal interface IPropagation
    {
        /// <summary>
        /// 是否已经停止冒泡
        /// </summary>
        bool stopPropagation { get; }
        /// <summary>
        /// 停止冒泡
        /// </summary>
        void StopPropagation();

        /// <summary>
        /// 重新开始冒泡
        /// </summary>
        void ResumePropagation();
    }
}
