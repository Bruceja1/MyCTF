using System;
using MCGalaxy;
using MCGalaxy.Events;

namespace MCGalaxy;

public sealed class OnCTFPlayerInfoUpdatedEvent : IEvent<OnCTFPlayerInfoUpdated>
{
    public static void Call(Player p, string flag, string group)
    {
        IEvent<OnCTFPlayerInfoUpdated>[] items = IEvent<OnCTFPlayerInfoUpdated>.handlers.Items;
        for (int i = 0; i < items.Length; i++)
        {
            try
            {
                items[i].method(p, flag, group);
            }
            catch (Exception ex)
            {
                IEvent<OnCTFPlayerInfoUpdated>.LogHandlerException(ex, items[i]);
            }
        }
    }
}
