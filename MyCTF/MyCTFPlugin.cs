using MCGalaxy.Commands.Misc;
using MCGalaxy.DB;
using MCGalaxy.Events;
using MCGalaxy.Events.ServerEvents;
using System.Collections.Generic;

namespace MCGalaxy.Modules.Games.MyCTF;

public sealed class MyCTFPlugin : Plugin
{
    private static Command cmdMyCTF = new CmdMyCTF();

    public override string name => "MyCTF";
    DBTopStat killStat = new DBTopStat("Kills", "Most kills", "MyCTF", "Kills", TopStat.FormatInteger);
    DBTopStat captureStat = new DBTopStat("Captures", "Most captures", "MyCTF", "Captures", TopStat.FormatInteger);

    public override void Load(bool startup)
    {
        Command.Register(cmdMyCTF);
        TopStat.Register(killStat);
        TopStat.Register(captureStat);
        MyCTFGame instance = MyCTFGame.Instance;
        instance.Config.Path = "properties/myctf.properties";
        instance.ReloadConfig();
        instance.AutoStart();
        IEvent<OnConfigUpdated>.Register(instance.ReloadConfig, Priority.Low);
    }

    public override void Unload(bool shutdown)
    {
        MyCTFGame instance = MyCTFGame.Instance;
        if (instance == null)
        {
            PlayerInfo.FindExact("Bruceja").Message("instance is null when trying to unload!");
        }
        if (instance.Running)
        {
            instance.End();
            instance.Running = false;
        }

        IEvent<OnConfigUpdated>.Unregister(instance.ReloadConfig);
        Command.Unregister(cmdMyCTF);
        TopStat.Unregister(killStat);
        TopStat.Unregister(captureStat);
    }
}