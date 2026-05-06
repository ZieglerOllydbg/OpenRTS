using Game.RA2.Client;
using ZFrame;

public class MatchedEvent : ModuleEvent
{
    public MatchSuccessData data;

    public MatchedEvent(MatchSuccessData _data) : base()
    {
        data = _data;
    }
}