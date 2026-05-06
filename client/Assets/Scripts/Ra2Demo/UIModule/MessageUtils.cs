
using ZFrame;

public static class MessageUtils
{
    public static void ShowTips(string tips)
    {
        Frame.DispatchEvent(new ShowMessageEvent(tips));
    }
}