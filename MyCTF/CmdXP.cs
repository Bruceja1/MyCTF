using MCGalaxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCGalaxy.Modules.Games.MyCTF;

internal sealed class CmdXP : Command2
{
    public override string name => "XP";
    public override string type => "Info";

    public override void Use(Player p, string message)
    {
        MyCTFGame instance = MyCTFGame.Instance;
        if (instance == null )
        {
            p.Message("&cError while retrieving your XP: no CTF game instance found.");
        }
        instance.GetXP(p);
    }

    public override void Help(Player p)
    {
        p.Message("&T/XP &H-Shows how much XP you have and how much XP you need to reach the next rank.");  
    }
}
