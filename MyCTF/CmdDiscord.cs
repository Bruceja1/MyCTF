using MCGalaxy.DB;
using MCGalaxy;
using MCGalaxy.Commands;

namespace MyCTF;

internal sealed class CmdDiscord : Command2
{
    public override string name => "Discord";

    public override string shortcut => "dc";

    public override string type => "Info";

    public override void Use(Player p, string message, CommandData data)
    {
        p.Message("Join the Discord server to see who's playing at any time: &bhttps://discord.gg/gVUrNgRnx7");
    }

    public override void Help(Player p)
    {
        p.Message("&T/discord&H- Displays the Discord server invitation link.");
    }

}