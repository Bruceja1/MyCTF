using MCGalaxy.Config;
using MCGalaxy.Games;
using MCGalaxy.Maths;
using MCGalaxy;

namespace MyCTF;

public sealed class MyCTFMapConfig : RoundsGameMapConfig
{
    [ConfigVec3("red-spawn", null)]
    public Vec3U16 RedSpawn;

    [ConfigVec3("red-pos", null)]
    public Vec3U16 RedFlagPos;

    [ConfigBlock("red-block", null, 0)]
    public ushort RedFlagBlock;

    [ConfigVec3("blue-spawn", null)]
    public Vec3U16 BlueSpawn;

    [ConfigVec3("blue-pos", null)]
    public Vec3U16 BlueFlagPos;

    [ConfigBlock("blue-block", null, 0)]
    public ushort BlueFlagBlock;

    [ConfigInt("map.line.z", null, 0, int.MinValue, int.MaxValue)]
    public int ZDivider;

    [ConfigInt("time-in-minutes", null, 10, int.MinValue, int.MaxValue)]
    public int Time;


    //[ConfigInt("game.maxpoints", null, 3, int.MinValue, int.MaxValue)]
    //public int RoundPoints = 3;

    //[ConfigInt("game.tag.points-gain", null, 5, int.MinValue, int.MaxValue)]
    //public int Tag_PointsGained = 5;

    //[ConfigInt("game.tag.points-lose", null, 5, int.MinValue, int.MaxValue)]
    //public int Tag_PointsLost = 5;

    //[ConfigInt("game.capture.points-gain", null, 10, int.MinValue, int.MaxValue)]
    //public int Capture_PointsGained = 10;

    //[ConfigInt("game.capture.points-lose", null, 10, int.MinValue, int.MaxValue)]
    //public int Capture_PointsLost = 10;

    private const string propsDir = "properties/MyCTF/";

    private static ConfigElement[] cfg;

    public override void Load(string map)
    {
        if (cfg == null)
        {
            cfg = ConfigElement.GetAll(typeof(MyCTFMapConfig));
        }

        LoadFrom(cfg, "properties/MyCTF/", map);
    }

    public override void Save(string map)
    {
        if (cfg == null)
        {
            cfg = ConfigElement.GetAll(typeof(MyCTFMapConfig));
        }

        SaveTo(cfg, "properties/MyCTF/", map);
    }

    public override void SetDefaults(Level lvl)
    {
        ZDivider = lvl.Length / 2;
        RedFlagBlock = 21;
        BlueFlagBlock = 29;
        ushort x = (ushort)(lvl.Width / 2);
        ushort num = (ushort)(lvl.Height / 2);
        ushort y = (ushort)(num + 2);
        ushort z = (ushort)(lvl.Length - 1);
        RedFlagPos = new Vec3U16(x, y, 0);
        RedSpawn = new Vec3U16(x, num, 0);
        BlueFlagPos = new Vec3U16(x, y, z);
        BlueSpawn = new Vec3U16(x, num, z);
        Time = 10;
    }
}