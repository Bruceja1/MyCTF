using System;
using MCGalaxy.Events;

namespace MCGalaxy;

public sealed class OnWinEvent : IEvent<OnWin>
{
    public static void Call(Player p, int wins, int winstreak)
    {
        IEvent<OnWin>[] items = IEvent<OnWin>.handlers.Items;
        for (int i = 0; i < items.Length; i++)
        {
            try
            {
                items[i].method(p, wins, winstreak);
            }
            catch (Exception ex)
            {
                IEvent<OnWin>.LogHandlerException(ex, items[i]);
            }
        }
    }
}
