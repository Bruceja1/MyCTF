using System;
using MCGalaxy;
using MCGalaxy.Events;

namespace MCGalaxy;

public sealed class OnCaptureEvent : IEvent<OnCapture>
{
    public static void Call(Player p, int totalCaptures, int roundCaptures)
    {
        IEvent<OnCapture>[] items = IEvent<OnCapture>.handlers.Items;
        for (int i = 0; i < items.Length; i++)
        {
            try
            {
                items[i].method(p, totalCaptures, roundCaptures);
            }
            catch (Exception ex)
            {
                IEvent<OnCapture>.LogHandlerException(ex, items[i]);
            }
        }
    }
}
