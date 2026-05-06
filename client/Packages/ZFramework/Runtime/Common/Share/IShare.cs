
namespace ZLib
{
    /// <summary>
    /// 共享池对象接口
    /// </summary>
    public interface IShare
    {
        /// <summary>
        /// 计数(仅供内部使用)
        /// </summary>
        int count { get; set; }
        /// <summary>
        /// 计数清0的时间(仅供内部使用)
        /// </summary>
        float time { get; set; }

        void AddRefInfo(object obj);
        void DelRefInfo(object obj);

        /// <summary>
        /// 销毁
        /// </summary>
        void Destroy();


    }
}