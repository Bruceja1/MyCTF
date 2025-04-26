using MCGalaxy;
using MyCTF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyCTF;
internal sealed class CmdBlockSetAll : Command2
{
    public override string name => "BlockSetAll";
    public override string shortcut => "bsa";
    public override string type => "Moderation";

    public override void Use(Player p, string message)
    {
        if (message.Length == 0)
        {
            Help(p);
            return;
        }

        string[] text = message.SplitSpaces();
        string exception = "";
        if (text.Length > 1)
        {
            exception = text[1];
        }

        for (int i = 1; i <= 767; i++)
        {
            if (i.ToString() == exception) continue;
            Command.Find("blockset").Use(p, i.ToString() + " " + text[0]);
        }
    }

    public override void Help(Player p)
    {
        p.Message("&T/BlockSetAll <rank> &H-Sets all blocks' permission to <rank>.");
    }
}
