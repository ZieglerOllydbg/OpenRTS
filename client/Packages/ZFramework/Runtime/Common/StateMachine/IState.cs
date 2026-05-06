namespace ZLib
{
    /// <summary>
    /// 状态机接口
    /// </summary>
    public interface IState<CONTEXT>
    {
        /// <summary>
        /// 上下文
        /// </summary>
        public CONTEXT context { get; set; }
        /// <summary>
        /// 当进入此状态
        /// </summary>
        void OnEnter();
        /// <summary>
        /// 执行状态
        /// </summary>
        void OnExecute();
        /// <summary>
        /// 当退出此状态
        /// </summary>
        void OnExit();
        /// <summary>
        /// 当销毁时
        /// </summary>
        void OnDestroy();

        /// <summary>
        /// 每帧执行更新
        /// </summary>
        void OnUpdate();
    }
}