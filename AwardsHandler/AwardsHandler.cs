using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using MCGalaxy;
using MCGalaxy.Events;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Modules.Awards;

namespace MCGalaxy;

public class AwardsHandler : Plugin
{
    public override string creator { get { return "Bruceja"; } }
    public override string MCGalaxy_Version { get { return "1.9.5.3"; } }
    public override string name { get { return "AwardsHandler"; } }

    public override void Load(bool startup)
    {
        OnKillEvent.Register(HandleKill, Priority.High);
        OnCaptureEvent.Register(HandleCapture, Priority.High);
    }

    public override void Unload(bool shutdown)
    {
        OnKillEvent.Unregister(HandleKill);
        OnCaptureEvent.Unregister(HandleCapture);
    }

    bool HasAward(Player p, string award)
    {
        List<string> awards = PlayerAwards.Get(p.truename);
        if (awards != null && awards.Contains(award))
        {
            return true;
        }
        return false;
    }
    void HandleKill(Player p, int totalKills, int roundKills)
    {
        p.Message("Round kills: " + roundKills.ToString());
        if (roundKills >= 30 && !HasAward(p, "Exterminator"))
        {
            Command.Find("award").Use(Player.Console, "give " + p.truename + " Exterminator");
        }
    }

    void HandleCapture(Player p, int totalCaptures, int roundCaptures)
    {
        p.Message("Round captures: " + roundCaptures.ToString());
        if (roundCaptures >= 3 && !HasAward(p, "Bringing It Home"))
        {
            Command.Find("award").Use(Player.Console, "give " + p.truename + " Bringing It Home");
        }
    }
}

