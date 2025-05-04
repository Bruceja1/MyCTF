using MCGalaxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyCTF;

internal sealed class CmdPlay : Command2
{
    public override string name => "Play";
    public override string type => "Info";

    public override void Use(Player p, string message)
    {
        p.Message("&b-+- How to play -+-");
        p.Message("&bBreak the opposing team's flag block to grab it.");
        p.Message("&bTo capture it, go back and click your team's flag.");
        p.Message("&bPlace gravel to fire a deadly laser in front of you.");
        p.Message("&bType &a/status &bor &a. &bin chat to view your round stats.");
        p.Message("&bUse &a/whois &bto view your overall stats and profile.");
        p.Message("&bUse &a/xp &bto see when you will rank up.");
        p.Message("&bUse &a/store &bto spend cash on various cosmetics.");
        p.Message("&bAchieve all &a/awards &bto turn your &a/whois &bprofile &6gold&b.");
        p.Message("&bJoin the Discord server using &a/discord&b.");
    }

    public override void Help(Player p)
    {
        p.Message("&T/Play &H-Tells you how the game works.");
    }
}
