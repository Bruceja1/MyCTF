using MCGalaxy.Config;
using MCGalaxy.Games;
using System.Collections.Generic;

namespace MyCTF;

public sealed class MyCTFConfig : RoundsGameConfig
{
    [ConfigFloat("tag-distance", "Collisions", 1f, float.NegativeInfinity, float.PositiveInfinity)]
    public float TagDistance = 1f;

    [ConfigInt("collisions-check-interval", "Collisions", 150, 20, 2000)]
    public int CollisionsCheckInterval = 150;

    [ConfigInt("countdown-timer", "Game properties", 5)]
    public int CountdownTimer = 300;

    [ConfigString("info-color", "Game properties", "&6")]
    public string InfoColor = "&6";

    [ConfigString("chat-color", "Game properties", "&f")]
    public string ChatColor = "&7";

    [ConfigFloat("flag-bot-scale", "Game properties", (float)0.8)]
    public float FlagBotScale = (float)0.8;

    [ConfigInt("flag-bot-y-offset", "Game properties", 85)]
    public int FlagBotYOffset = 85;

    [ConfigInt("max-captures", "Game properties", 5)]
    public int MaxCaptures = 5;

    [ConfigInt("round-stats-summary-count", "Game properties", 3)]
    public int RoundStatsSummaryCount = 3;

    [ConfigInt("kill-xp-reward", "XP Rewards", 2)]
    public int KillXPReward = 2;

    [ConfigInt("capture-xp-reward", "XP Rewards", 5)]
    public int CaptureXPReward = 5;

    [ConfigInt("win-xp-reward", "XP Rewards", 8)]
    public int WinXPReward = 8;

    [ConfigStringList("item-blocks", "Item Blocks")]
    public List<string> ItemBlocks = new List<string>(){ "13" };


    public override bool AllowAutoload => false;

    protected override string GameName => "MyCTF";
}