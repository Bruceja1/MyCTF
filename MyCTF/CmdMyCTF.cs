using MCGalaxy.Commands;
using MCGalaxy.Commands.Fun;
using MCGalaxy.Games;
using MCGalaxy.Maths;

namespace MCGalaxy.Modules.Games.MyCTF;

internal sealed class CmdMyCTF : RoundsGameCmd
{
    public override string name => "MyCTF";

    public override string shortcut => "mc";

    protected override RoundsGame Game => MyCTFGame.Instance;

    public override CommandPerm[] ExtraPerms => new CommandPerm[1]
    {
        new CommandPerm(LevelPermission.Operator, "can manage CTF")
    };

    protected override void HandleSet(Player p, RoundsGame game, string[] args)
    {
        string a = args[1];
        MyCTFMapConfig cTFMapConfig = new MyCTFMapConfig();
        LoadMapConfig(p, cTFMapConfig);
        if (a.CaselessEq("bluespawn"))
        {
            cTFMapConfig.BlueSpawn = (Vec3U16)p.Pos.FeetBlockCoords;
            Vec3U16 blueSpawn = cTFMapConfig.BlueSpawn;
            p.Message("Set spawn of blue team to &b" + blueSpawn.ToString());
            SaveMapConfig(p, cTFMapConfig);
        }
        else if (a.CaselessEq("redspawn"))
        {
            cTFMapConfig.RedSpawn = (Vec3U16)p.Pos.FeetBlockCoords;
            Vec3U16 blueSpawn = cTFMapConfig.RedSpawn;
            p.Message("Set spawn of red team to &b" + blueSpawn.ToString());
            SaveMapConfig(p, cTFMapConfig);
        }
        else if (a.CaselessEq("blueflag"))
        {
            p.Message("Place or delete a block to set blue team's flag.");
            p.MakeSelection(1, cTFMapConfig, BlueFlagCallback);
        }
        else if (a.CaselessEq("redflag"))
        {
            p.Message("Place or delete a block to set red team's flag.");
            p.MakeSelection(1, cTFMapConfig, RedFlagCallback);
        }
        else if (a.CaselessEq("divider"))
        {
            cTFMapConfig.ZDivider = p.Pos.BlockZ;
            p.Message("Set Z line divider to {0}.", cTFMapConfig.ZDivider);
            SaveMapConfig(p, cTFMapConfig);
        }
        else
        {
            Help(p, "set");
        }
    }

    private bool BlueFlagCallback(Player p, Vec3S32[] marks, object state, ushort block)
    {
        MyCTFMapConfig cTFMapConfig = (MyCTFMapConfig)state;
        Vec3U16 vec3U = (cTFMapConfig.BlueFlagPos = (Vec3U16)marks[0]);
        p.Message("Set flag position of blue team to ({0})", vec3U);
        block = p.level.GetBlock(vec3U.X, vec3U.Y, vec3U.Z);
        if (block == 0)
        {
            block = 29;
        }

        cTFMapConfig.BlueFlagBlock = block;
        p.Message("Set flag block of blue team to {0}", Block.GetName(p, block));
        SaveMapConfig(p, cTFMapConfig);
        return false;
    }

    private bool RedFlagCallback(Player p, Vec3S32[] marks, object state, ushort block)
    {
        MyCTFMapConfig cTFMapConfig = (MyCTFMapConfig)state;
        Vec3U16 vec3U = (cTFMapConfig.RedFlagPos = (Vec3U16)marks[0]);
        p.Message("Set flag position of red team to ({0})", vec3U);
        block = p.level.GetBlock(vec3U.X, vec3U.Y, vec3U.Z);
        if (block == 0)
        {
            block = 21;
        }

        cTFMapConfig.RedFlagBlock = block;
        p.Message("Set flag block of red team to {0}", Block.GetName(p, block));
        SaveMapConfig(p, cTFMapConfig);
        return false;
    }

    public override void Help(Player p, string message)
    {
        if (message.CaselessEq("set"))
        {
            p.Message("&T/MyCTF set redspawn/bluespawn");
            p.Message("&HSets spawn of red/blue team to your position.");
            p.Message("&T/MyCTF set redflag/blueflag");
            p.Message("&HSets flag position and block of red/blue team to the next block you place or delete.");
            p.Message("&T/MyCTF set divider");
            p.Message("&HSets the divider line to your current Z position.");
            p.Message("   &HRed team tags blue team when the Z position is less than the divider, blue teams tags when Z position is more.");
        }
        else
        {
            Help(p);
        }
    }

    public override void Help(Player p)
    {
        p.Message("&T/MyCTF start <map> &H- Starts CTF game");
        p.Message("&T/MyCTF stop &H- Stops CTF game");
        p.Message("&T/MyCTF end &H- Ends current round of CTF");
        p.Message("&T/MyCTF add/remove &H- Adds/removes current map from map list");
        p.Message("&T/MyCTF set [property] &H- Sets a property. See &T/Help MyCTF set");
        p.Message("&T/MyCTF status &H- View stats of both teams");
        p.Message("&T/MyCTF go &H- Moves you to the current CTF map");
    }
}