using MCGalaxy.Maths;

namespace MCGalaxy.Modules.Games.MyCTF;

internal sealed class MyCtfData
{
    public int Captures;
    public int Kills;

    public int Tags;

    public int Points;
    public int XP;

    public bool HasFlag;

    public bool TagCooldown;

    public bool TeamChatting;

    public Vec3S32 LastHeadPos;
}