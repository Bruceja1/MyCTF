using MCGalaxy.Commands.Misc;
using MCGalaxy.DB;
using MCGalaxy.Events;
using MCGalaxy.Events.ServerEvents;
using System.Collections.Generic;
using MCGalaxy;
using System.Dynamic;
using System;
using MCGalaxy.Network;
using BlockID = System.UInt16;

namespace MyCTF;

public sealed class MyCTFPlugin : Plugin
{
    private static Command cmdMyCTF = new CmdMyCTF();
    private static Command cmdXP = new CmdXP();
    private static Command cmdMcDebug = new CmdMcDebug();
    private static Command cmdBlockSetAll = new CmdBlockSetAll();
    private static Command cmdStatus = new CmdStatus();

    public override string name => "MyCTF";
    DBTopStat xpStat = new DBTopStat("XP", "Most XP", "MyCTF", "XP", TopStat.FormatInteger);
    DBTopStat captureStat = new DBTopStat("Captures", "Most captures", "MyCTF", "Captures", TopStat.FormatInteger);
    DBTopStat killStat = new DBTopStat("Kills", "Most kills", "MyCTF", "Kills", TopStat.FormatInteger);
    DBTopStat killstreakStat = new DBTopStat("Killstreak", "Highest killstreaks", "MyCTF", "Killstreak", TopStat.FormatInteger);
    DBTopStat winStat = new DBTopStat("Wins", "Most wins", "MyCTF", "Wins", TopStat.FormatInteger);
    DBTopStat winstreakStat = new DBTopStat("Winstreak", "Highest winstreaks", "MyCTF", "Winstreak", TopStat.FormatInteger);
    public override void Load(bool startup)
    {
        Command.Register(cmdMyCTF);
        Command.Register(cmdXP);
        Command.Register(cmdMcDebug);
        Command.Register(cmdBlockSetAll);
        Command.Register(cmdStatus);

        TopStat.Register(xpStat);
        TopStat.Register(captureStat);
        TopStat.Register(killStat);       
        TopStat.Register(killstreakStat);
        TopStat.Register(winStat);
        TopStat.Register(winstreakStat);

        MyCTFGame instance = MyCTFGame.Instance;
        instance.Config.Path = "properties/myctf.properties";
        instance.ReloadConfig();
        instance.AutoStart();
        IEvent<OnConfigUpdated>.Register(instance.ReloadConfig, Priority.Low);
        OnlineStat.Stats = MyCTFOnlineProfileStat.Stats;
        OfflineStat.Stats = MyCTFOfflineProfileStat.Stats;
    }

    public override void Unload(bool shutdown)
    {
        MyCTFGame instance = MyCTFGame.Instance;
        if (instance.Running)
        {
            instance.End();
            instance.Running = false;
        }

        IEvent<OnConfigUpdated>.Unregister(instance.ReloadConfig);
        Command.Unregister(cmdMyCTF);
        Command.Unregister(cmdXP);
        Command.Register(cmdMcDebug);
        Command.Unregister(cmdBlockSetAll);
        Command.Unregister(cmdStatus);

        TopStat.Unregister(xpStat);
        TopStat.Unregister(captureStat);
        TopStat.Unregister(killStat);
        TopStat.Unregister(killstreakStat);
        TopStat.Unregister(winStat);
        TopStat.Unregister(winstreakStat);

        OnlineStat.Stats.Clear();
        OfflineStat.Stats.Clear();
    }
}