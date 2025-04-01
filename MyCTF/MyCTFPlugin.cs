using MCGalaxy.Commands.Misc;
using MCGalaxy.Events;
using MCGalaxy.Events.ServerEvents;
using System.Collections.Generic;

namespace MCGalaxy.Modules.Games.MyCTF;

public sealed class MyCTFPlugin : Plugin
{
    private static Command cmdMyCTF = new CmdMyCTF();

    public override string name => "MyCTF";

    public override void Load(bool startup)
    {
        Command.Register(cmdMyCTF);
        MyCTFGame instance = MyCTFGame.Instance;
        instance.Config.Path = "properties/myctf.properties";
        instance.ReloadConfig();
        instance.AutoStart();
        IEvent<OnConfigUpdated>.Register(instance.ReloadConfig, Priority.Low);
    }

    public override void Unload(bool shutdown)
    {
        foreach (Player player in PlayerInfo.Online.Items)
        {
            MyCTFGame.ClearData(player); // Debugging
        }

        MyCTFGame instance = MyCTFGame.Instance;
        if (instance.Running)
        {
            instance.Running = false;
        }
        IEvent<OnConfigUpdated>.Unregister(instance.ReloadConfig);
        Command.Unregister(cmdMyCTF);
    }
}