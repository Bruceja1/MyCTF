using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Security.Policy;
using MCGalaxy;
using MCGalaxy.DB;
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
        OnPlayerConnectEvent.Register(HandlePlayerConnect, Priority.High);
        OnWinEvent.Register(HandleWin, Priority.High);
        OnPlayerDiedEvent.Register(HandleDeath, Priority.High);
    }

    public override void Unload(bool shutdown)
    {
        OnKillEvent.Unregister(HandleKill);
        OnCaptureEvent.Unregister(HandleCapture);
        OnPlayerConnectEvent.Unregister(HandlePlayerConnect);
        OnWinEvent.Unregister(HandleWin);
        OnPlayerDiedEvent.Unregister(HandleDeath);
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

    void HandleKill(Player p, int totalKills, int roundKills, int totalKillstreak, double timeSinceLastKill)
    {
        if (roundKills >= 30 && !HasAward(p, "Exterminator"))
        {
            Command.Find("award").Use(Player.Console, "give " + p.truename + " Exterminator");
            OnAchievementGetEvent.Call(p, "Exterminator");
        }
        if (totalKillstreak >= 10 && !HasAward(p, "Menace"))
        {
            Command.Find("award").Use(Player.Console, "give " + p.truename + " Menace");
            OnAchievementGetEvent.Call(p, "Menace");
        }
        if (timeSinceLastKill <= 0.5 && !HasAward(p, "Two Birds One Laser"))
        {
            Command.Find("award").Use(Player.Console, "give " + p.truename + " Two Birds One Laser");
            OnAchievementGetEvent.Call(p, "Two Birds One Laser");
        }
    }

    void HandleCapture(Player p, int totalCaptures, int roundCaptures)
    {
        if (roundCaptures >= 3 && !HasAward(p, "Bringing It Home"))
        {
            Command.Find("award").Use(Player.Console, "give " + p.truename + " Bringing It Home");
            OnAchievementGetEvent.Call(p, "Bringing It Home");
        }
    }

    void HandlePlayerConnect(Player p)
    {
        if (p.TotalTime.TotalDays >= 10 && !HasAward(p, "No-Lifer"))
        {
            Command.Find("award").Use(Player.Console, "give " + p.truename + " No-Lifer");
            OnAchievementGetEvent.Call(p, "No-Lifer");
        }
    }

    void HandleWin(Player p, int wins, int winstreak)
    {
        if (wins >= 1 && !HasAward(p, "Victory Achieved"))
        {
            Command.Find("award").Use(Player.Console, "give " + p.truename + " Victory Achieved");
            OnAchievementGetEvent.Call(p, "Victory Achieved");
        }
        if (wins >= 100 && !HasAward(p, "Superstar"))
        {
            Command.Find("award").Use(Player.Console, "give " + p.truename + " Superstar");
            OnAchievementGetEvent.Call(p, "Superstar");
        }
        if (winstreak >= 5 && !HasAward(p, "I Am Malenia Blade Of Miquella..."))
        {
            Command.Find("award").Use(Player.Console, "give " + p.truename + " I Am Malenia Blade Of Miquella...");
            OnAchievementGetEvent.Call(p, "I Am Malenia Blade Of Miquella...");
        }
        if (winstreak >= 10 && !HasAward(p, "...And I Have Never Known Defeat"))
        {
            Command.Find("award").Use(Player.Console, "give " + p.truename + " ...And I Have Never Known Defeat");
            OnAchievementGetEvent.Call(p, "...And I Have Never Known Defeat");
        }
    }

    void HandleDeath(Player p, ushort cause, ref TimeSpan cooldown)
    {
        if (p.TimesDied >= 1000 && !HasAward(p, "The True Geometry Dash Experience"))
        {
            Command.Find("award").Use(Player.Console, "give " + p.truename + " The True Geometry Dash Experience");
            OnAchievementGetEvent.Call(p, "The True Geometry Dash Experience");
        }
    }
}

