using System.Linq;
using zUnity;
using ZLockstep.Simulation;
using ZLockstep.Simulation.ECS;
using ZLockstep.Simulation.ECS.Components;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ZLockstep.Sync.Command.Commands
{
    /// <summary>
    /// 移动命令
    /// 让一个或多个单位移动到指定位置
    /// </summary>
    [CommandType(CommandTypes.GMCommand)]
    public class GMCommand : BaseCommand
    {
        public string CommandLine { get; set; }

        public GMCommand(string commandLine)
            : base(0)
        {
            CommandLine = commandLine;
        }

        public override void Execute(zWorld world)
        {
            world.GMManager.ExecuteCommand(CommandLine);
        }
        
    }
    
}