namespace ZLib
{
    /// <summary>
    /// 通知信息
    /// @author zcp
    /// </summary>
    public class Notification
    {

        /// <summary>
        /// 通知标识
        /// </summary>
        public int key;

        /// <summary>
        /// 通知数据体
        /// </summary>
        public object body;

        /// <summary>
        /// Notification
        /// </summary>
        /// <param name="__name">通知标识</param>
        /// <param name="__body">通知数据体</param>
        public Notification(int __key, object __body)
        {
            key = __key;
            body = __body;
        }
    }
}