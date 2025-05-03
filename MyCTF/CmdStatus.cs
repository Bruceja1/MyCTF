using MCGalaxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyCTF;

internal sealed class CmdStatus : Command2
{
    public override string name => "Status";
    public override string type => "Info";

    public override void Use(Player p, string message)
    {
        MyCTFGame instance = MyCTFGame.Instance;
        if (instance == null)
        {
            p.Message("&cError while retrieving your Status: no CTF game instance found.");
        }
        instance.OutputStatus(p);
    }

    public override void Help(Player p)
    {
        p.Message("&T/status &H-Shows round stats such as captures, kills, killstreak and winstreak.");
    }
}
