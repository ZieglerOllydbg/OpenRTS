namespace ZLib
{
    /// <summary>
    /// 缓存池对象接口
    /// </summary>
    public interface ICache
    {
        /// <summary>
        /// 是否强壮的,  是的话, 当时间延迟达到时, 不会被销毁
        /// </summary>
        bool isStrong { get; }

        /// <summary>
        /// 标识key(可以不唯一)
        /// </summary>
        string key { get; }
        /// <summary>
        /// 销毁
        /// </summary>
        void Destroy();
    }
}