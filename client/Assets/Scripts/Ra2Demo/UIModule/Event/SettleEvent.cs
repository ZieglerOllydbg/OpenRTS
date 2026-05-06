using ZFrame;

public class SettleEvent : ModuleEvent
{
    public bool IsVictory { get; private set; }
    
    public SettleEvent(bool isVictory) : base()
    {
        IsVictory = isVictory;
    }
}