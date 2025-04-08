using MCGalaxy.DB;

namespace MCGalaxy.Commands.CPE;

public sealed class CmdMcDebug : Command2
{
    public override string name => "McDebug";

    public override string shortcut => "mcd";

    public override string type => "other";

    public override CommandPerm[] ExtraPerms => new CommandPerm[1]
    {
        new CommandPerm(LevelPermission.Guest, "can display their Extras MyCTF data.")
    };

    public override void Use(Player p, string message, CommandData data)
    {   
        p.Message($"The key {(p.Extras.Contains("MCG_MYCTF_DATA") ? "exists" : "does not exist")}");
    }

    public override void Help(Player p)
    {
        p.Message("&T/mcdebug&H- Displays whether or not the key MCG_MYCTF_DATA exists in your p.Extras.");
    }


}