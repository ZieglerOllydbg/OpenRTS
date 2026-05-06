namespace ZLockstep.Simulation.Events
{
    /// <summary>
    /// 消息事件结构体，用于在仿真系统中传递字符串消息
    /// 实现IEvent接口，可在事件系统中使用
    /// </summary>
    public struct MessageEvent : IEvent
    {
        /// <summary>
        /// 消息内容
        /// </summary>
        public string Message;

        public MessageEvent(string message)
        {
            Message = message;
        }
    }
}