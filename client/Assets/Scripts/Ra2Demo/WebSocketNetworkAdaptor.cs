using Game.RA2.Client;
using ZLockstep.Sync;
using ZLockstep.Sync.Command;
using ZLockstep.View;
using System.Collections.Generic;
using zUnity;
using System;

public class WebSocketNetworkAdaptor : INetworkAdapter
{
    private WebSocketClient _client;

    private Ra2Demo _ra2Demo;

    public WebSocketNetworkAdaptor(Ra2Demo ra2Demo, WebSocketClient client)
    {
        _ra2Demo = ra2Demo; // 保存引用
        _client = client;

        // 绑定网络适配器
        _ra2Demo.GetBattleGame().FrameSyncManager.NetworkAdapter = this;
        _client.OnFrameSync += OnFrameSync(_ra2Demo.GetBattleGame());
    }

    public void SendCommandToServer(ICommand command)
    {
        _client.SendFrameInput(command.ExecuteFrame, command);
    }

    private static Action<FrameSyncData> OnFrameSync(ZLockstep.Sync.Game game)
    {
        return (data) =>
        {
            List<ICommand> commandList = FrameSyncMessageParser.ParseCommands(data.Data, "[Ra2Demo]");
            if (commandList.Count > 0)
            {
                zUDebug.Log($"[Ra2Demo] 接收到的命令帧: {data}");
            }

            game.FrameSyncManager.ConfirmFrame(data.Frame, commandList);
            int pendingFrameCount = game.FrameSyncManager.GetPendingFrameCount();
            if (pendingFrameCount > 1 && !game.IsCatchingUp)
            {
                game.TryStartCatchUp();
            }
        };
    }
}
