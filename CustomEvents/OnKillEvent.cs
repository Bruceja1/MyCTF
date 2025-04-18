using System;
using MCGalaxy;
using MCGalaxy.Events;

namespace MCGalaxy;

public sealed class OnKillEvent : IEvent<OnKill>
{
    public static void Call(Player p, int totalKills, int roundKills)
    {
        IEvent<OnKill>[] items = IEvent<OnKill>.handlers.Items;
        for (int i = 0; i < items.Length; i++)
        {
            try
            {
                items[i].method(p, totalKills, roundKills);
            }
            catch (Exception ex)
            {
                IEvent<OnKill>.LogHandlerException(ex, items[i]);
            }
        }
    }
}
