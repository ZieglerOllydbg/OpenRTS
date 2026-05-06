using ZFrame;

/// <summary>
/// 网络对局开始事件，携带服务器为本局历史帧持久化生成的文件名，供界面流程在本地创建占位文件。
/// </summary>
public class GameStartEvent : ModuleEvent
{
    /// <summary>
    /// 获取服务器下发的历史帧文件名；为空时表示服务器未下发或仍使用旧协议。
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// 创建网络对局开始事件，并保存服务器下发的历史帧文件名。
    /// </summary>
    /// <param name="fileName">服务器在 gameStart 消息中下发的历史帧持久化文件名。</param>
    public GameStartEvent(string fileName) : base()
    {
        FileName = fileName;
    }
}
