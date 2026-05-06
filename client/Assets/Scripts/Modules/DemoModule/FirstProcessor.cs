using System;
using System.Collections.Generic;
using ZFrame;

public class FirstProcessor : BaseProcessor
{
    private DemoPanel _demoPanel;
    public DemoPanel DemoPanel
    {
        get
        {
            if (_demoPanel == null)
            {
                // 使用字符串ID，业务层可以定义自己的枚举并通过ToString()转换
                _demoPanel = _demoPanel.New<DemoPanel>(this, "DemoPanel");
            }
            return _demoPanel;
        }
    }

    public FirstProcessor(Module _module) : base(_module)
    {
    }

    protected override List<Type> ListenModuleEvents()
    {
        return new List<Type>()
        {
            typeof(StartUpEvent)
        };
    }

    protected override void ReceivedMessage(object __msg)
    {
    }

    protected override void ReceiveModuleEvent(ModuleEvent __me)
    {
        switch (__me)
        {
            case StartUpEvent e:
                {
                    DemoPanel.Open();
                }
                break;
        }
    }
}
