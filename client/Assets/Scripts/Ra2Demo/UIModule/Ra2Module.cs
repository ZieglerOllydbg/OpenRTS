using System.Collections.Generic;
using ZFrame;

[Module("Ra2Module")]
public class Ra2Module : Module
{
    protected override List<Processor> ListProcessors()
    {
        return new List<Processor>()
        {
            new Ra2Processor(this)
        };
    }
}
