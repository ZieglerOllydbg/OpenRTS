using ZFrame;

public class Ra2StartUpEvent : ModuleEvent
{
    public Ra2Demo Ra2Demo;

    public Ra2StartUpEvent(Ra2Demo __ra2Demo) : base()
    {
        Ra2Demo = __ra2Demo;
    }
}
