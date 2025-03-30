using MCGalaxy.Config;
using MCGalaxy.Games;

namespace MCGalaxy.Modules.Games.MyCTF;

public sealed class MyCTFConfig : RoundsGameConfig
{
    [ConfigFloat("tag-distance", "Collisions", 1f, float.NegativeInfinity, float.PositiveInfinity)]
    public float TagDistance = 1f;

    [ConfigInt("collisions-check-interval", "Collisions", 150, 20, 2000)]
    public int CollisionsCheckInterval = 150;

    public override bool AllowAutoload => false;

    protected override string GameName => "MyCTF";
}