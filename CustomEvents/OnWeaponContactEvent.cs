using System;
using MCGalaxy;
using MCGalaxy.Events;

namespace MCGalaxy;

public sealed class OnWeaponContactEvent : IEvent<OnWeaponContact>
{
    public static void Call(Player p, Player opponent)
    {
        IEvent<OnWeaponContact>[] items = IEvent<OnWeaponContact>.handlers.Items;
        for (int i = 0; i < items.Length; i++)
        {
            try
            {
                items[i].method(p, opponent);
            }
            catch (Exception ex)
            {
                IEvent<OnWeaponContact>.LogHandlerException(ex, items[i]);
            }
        }
    }
}
