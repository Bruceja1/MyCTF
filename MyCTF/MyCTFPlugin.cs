using MCGalaxy.Commands.Misc;
using MCGalaxy.DB;
using MCGalaxy.Events;
using MCGalaxy.Events.ServerEvents;
using System.Collections.Generic;
using MCGalaxy;
using System.Dynamic;
using System;

namespace MyCTF;

public sealed class MyCTFPlugin : Plugin
{
    private static Command cmdMyCTF = new CmdMyCTF();
    private static Command cmdXP = new CmdXP();

    public override string name => "MyCTF";
    DBTopStat killStat = new DBTopStat("Kills", "Most kills", "MyCTF", "Kills", TopStat.FormatInteger);
    DBTopStat captureStat = new DBTopStat("Captures", "Most captures", "MyCTF", "Captures", TopStat.FormatInteger);
    DBTopStat xpStat = new DBTopStat("XP", "Most XP", "MyCTF", "XP", TopStat.FormatInteger);
    DBTopStat killstreakStat = new DBTopStat("Killstreak", "Highest killstreaks", "MyCTF", "Killstreak", TopStat.FormatInteger);

    public override void Load(bool startup)
    {
        Command.Register(cmdMyCTF);
        Command.Register(cmdXP);
        TopStat.Register(killStat);
        TopStat.Register(captureStat);
        TopStat.Register(xpStat);
        TopStat.Register(killstreakStat);
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
        TopStat.Unregister(killStat);
        TopStat.Unregister(captureStat);
        TopStat.Unregister(xpStat);
        TopStat.Unregister(killstreakStat);
        OnlineStat.Stats.Clear();
        OfflineStat.Stats.Clear();
    }
}