using System;
using MCGalaxy.Events;

namespace MCGalaxy;

public sealed class OnAchievementGetEvent : IEvent<OnAchievementGet>
{
    public static void Call(Player p, string achievement)
    {
        IEvent<OnAchievementGet>[] items = IEvent<OnAchievementGet>.handlers.Items;
        for (int i = 0; i < items.Length; i++)
        {
            try
            {
                items[i].method(p, achievement);
            }
            catch (Exception ex)
            {
                IEvent<OnAchievementGet>.LogHandlerException(ex, items[i]);
            }
        }
    }
}
