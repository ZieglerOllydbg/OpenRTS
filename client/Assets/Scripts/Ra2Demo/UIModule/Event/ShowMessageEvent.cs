using System;
using Game.RA2.Client;
using ZFrame;
using ZLockstep.Simulation.ECS;

public class ShowMessageEvent : ModuleEvent
{
    public string Message { get; private set; }

    public ShowMessageEvent(string message) : base()
    {
        Message = message;
    }
}