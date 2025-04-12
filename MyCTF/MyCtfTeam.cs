using MCGalaxy.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCGalaxy;

namespace MyCTF;

internal sealed class MyCtfTeam
{
    public string Name;

    public string Color;

    public int Captures;

    public Vec3U16 FlagPos;

    public Vec3U16 SpawnPos;

    public ushort FlagBlock;

    public VolatileArray<Player> Members = new VolatileArray<Player>();

    public string ColoredName => Color + Name;

    public MyCtfTeam(string name, string color)
    {
        Name = name;
        Color = color;
    }

    public void RespawnFlag(Level lvl)
    {
        Vec3U16 flagPos = FlagPos;
        lvl.Blockchange(flagPos.X, flagPos.Y, flagPos.Z, FlagBlock);
    }
}

