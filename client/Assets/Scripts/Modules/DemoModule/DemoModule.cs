using System.Collections.Generic;
using ZFrame;

[Module("FirstModule")]
public class FirstModule : Module
{
    protected override List<Processor> ListProcessors()
    {
        return new List<Processor>()
        {
            new FirstProcessor(this)
        };
    }
}
