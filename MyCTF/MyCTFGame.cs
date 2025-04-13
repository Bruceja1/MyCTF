// Uses MCGalaxy's built-in CTF plugin as the foundation
// MCGalaxy can be found here: https://github.com/ClassiCube/MCGalaxy

using System;
using System.Collections.Generic;
using System.Threading;
using MCGalaxy;
using MCGalaxy.Blocks.Physics;
using MCGalaxy.Commands.Fun;
using MCGalaxy.Events;
using MCGalaxy.Events.EntityEvents;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Games;
using MCGalaxy.Maths;
using MCGalaxy.Modules.Games.CTF;
using MCGalaxy.SQL;
using MCGalaxy.Commands.CPE;
using System.Runtime.InteropServices;
using MCGalaxy.Events.GameEvents;
using MCGalaxy.DB;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using MCGalaxy.Network;
using static System.Net.Mime.MediaTypeNames;
using System.Xml.Linq;
using MCGalaxy.Commands;
using MyCTF;
using MyCTF.Events;
using System.Linq;
using MCGalaxy.Commands.World;

namespace MyCTF;

public class MyCTFGame : RoundsGame
{
    private struct MyCtfStats
    {
        public int Points;
        public int Captures;
        public int Tags;
        public int Kills;
        public int XP;
        public int Killstreak;
    }
    
    private MyCTFMapConfig cfg = new MyCTFMapConfig();

    public MyCTFConfig Config = new MyCTFConfig();

    private MyCtfTeam Red = new MyCtfTeam("Red", "&c");

    private MyCtfTeam Blue = new MyCtfTeam("Blue", "&9");

    public static MyCTFGame Instance = new MyCTFGame();

    private const string myctfExtrasKey = "MCG_MYCTF_DATA";

    private static ColumnDesc[] myctfTable = new ColumnDesc[8]
    {
        new ColumnDesc("ID", ColumnType.Integer, 0, autoInc: true, priKey: true, notNull: true),
        new ColumnDesc("Name", ColumnType.VarChar, 20),
        new ColumnDesc("Points", ColumnType.UInt24),
        new ColumnDesc("Captures", ColumnType.UInt24),
        new ColumnDesc("tags", ColumnType.UInt24),
        new ColumnDesc("Kills", ColumnType.UInt24),
        new ColumnDesc("XP", ColumnType.UInt24),
        new ColumnDesc("Killstreak", ColumnType.UInt24),
    };

    public override string GameName => "MyCTF";

    protected override string WelcomeMessage => "&9Capture the Flag &Sis running! Type &T/MyCTF go &Sto join";
    private MyCTFTimer timer = new MyCTFTimer();
    private Dictionary<string, MyCtfStats> roundStats = new Dictionary<string, MyCtfStats>();
    public override RoundsGameConfig GetConfig()
    {
        return Config;
    }

    public MyCTFGame()
    {
        Picker = new SimpleLevelPicker();
    }

    internal static MyCtfData Get(Player p)
    {
        MyCtfData ctfData = TryGet(p);
        if (ctfData != null)
        {
            return ctfData;
        }

        ctfData = new MyCtfData();
        MyCtfStats ctfStats = LoadStats(p.name);
        ctfData.Captures = ctfStats.Captures;
        ctfData.Points = ctfStats.Points;
        ctfData.Tags = ctfStats.Tags;
        ctfData.Kills = ctfStats.Kills;
        ctfData.XP = ctfStats.XP;
        ctfData.Killstreak = ctfStats.Killstreak;
        p.Extras["MCG_MYCTF_DATA"] = ctfData; // TODO: Why not p.Extras[myctfExtrasKey] = ctfData; ?
        return ctfData;
    }

    private static MyCtfData TryGet(Player p)
    {
        p.Extras.TryGet("MCG_MYCTF_DATA", out var value); // TODO: Why not p.Extras.TryGet(myctfExtrasKey, out var value); ?

        // The original line:
        // return (MyCtfData)value; 
        // Caused the following exception when unloading and reloading the plugin:
        // Type: InvalidCastException. For more details see error logs on 28/03/25 at 21:00:52
        // I believe it has to do with leftover data in p.Extras["MCG_MYCTF_DATA"]
        // The error only occurs after unloading and reloading the plugin

        // This line fixes the error but:
        // Works when just loading the plugin for the first time
        // When reloading the plugin the following happens:
        // Using /mc start bruceja7 and then using /mc status results in ObjectNullReference
        // After the teams have been assigned using /mc status works fine
        // Update: noticed there's a small window between teams being assigned and /mc status not returning
        // an ObjectNullReference anymore when trying to show CtfData
        return value as MyCtfData;
    }

    public override void UpdateMapConfig()
    {
        MyCTFMapConfig cTFMapConfig = new MyCTFMapConfig();
        cTFMapConfig.SetDefaults(Map);
        cTFMapConfig.Load(Map.name);
        cfg = cTFMapConfig;
        Red.FlagBlock = cTFMapConfig.RedFlagBlock;
        Red.FlagPos = cTFMapConfig.RedFlagPos;
        Red.SpawnPos = cTFMapConfig.RedSpawn;
        Blue.FlagBlock = cTFMapConfig.BlueFlagBlock;
        Blue.FlagPos = cTFMapConfig.BlueFlagPos;
        Blue.SpawnPos = cTFMapConfig.BlueSpawn;
    }

    protected override List<Player> GetPlayers()
    {
        List<Player> list = new List<Player>();
        list.AddRange(Red.Members.Items);
        list.AddRange(Blue.Members.Items);
        return list;
    }

    public override void OutputStatus(Player p)
    {
        p.Message("{0} &Steam: {1} captures", Blue.ColoredName, Blue.Captures);
        p.Message("{0} &Steam: {1} captures", Red.ColoredName, Red.Captures);
        MyCtfData playerData = Get(p);
        p.Message($"Captures: {playerData.Captures.ToString()}");
        p.Message($"Tags: {playerData.Tags.ToString()}");
        p.Message($"Kills: {playerData.Kills.ToString()}");
        p.Message($"Points: {playerData.Points.ToString()}");
        p.Message($"XP: {playerData.XP.ToString()}");
        p.Message($"Highest killstreak: {playerData.Killstreak.ToString()}");
        p.Message($"HasFlag: {playerData.HasFlag.ToString()}");
        p.Message($"TagCooldown {playerData.TagCooldown.ToString()}");
        p.Message($"TeamChatting: {playerData.TeamChatting.ToString()}");
        p.Message($"LastHeadPos: {playerData.LastHeadPos.ToString()}");
        p.Message($"Your team is: {(TeamOf(p) == null ? "No team" : TeamOf(p).Name)}");
    }

    protected override void StartGame()
    {             
        Blue.RespawnFlag(Map);
        Red.RespawnFlag(Map);
        ResetTeams();
        Database.CreateTable("MyCTF", myctfTable);
    }

    protected override void EndGame()
    {
        ResetTeams();
        ResetFlagsState();
    }

    private void ResetTeams()
    {
        //Blue.Members.Clear();
        //Red.Members.Clear();
        foreach (Player p in Blue.Members.Items)
        {
            RemoveFromTeam(p);
        }
        foreach (Player p in Red.Members.Items)
        {
            RemoveFromTeam(p);
        }
        Blue.Captures = 0;
        Red.Captures = 0;
    }

    private void ResetFlagsState()
    {
        Blue.RespawnFlag(Map);
        Red.RespawnFlag(Map);
        Player[] items = PlayerInfo.Online.Items;
        Player[] array = items;
        foreach (Player player in array)
        {
            if (player.level == Map)
            {
                MyCtfData ctfData = Get(player);
                if (ctfData.HasFlag)
                {
                    ctfData.HasFlag = false;
                    ResetPlayerFlag(player, ctfData);
                }
            }
        }
    }

    public override void PlayerJoinedGame(Player p)
    {
        bool announce = false;
        HandleSentMap(p, Map, Map);
        HandleJoinedLevel(p, Map, Map, ref announce);       
    }

    public override void PlayerLeftGame(Player p)
    {
        MyCtfTeam ctfTeam = TeamOf(p);
        if (ctfTeam != null)
        {
            //ctfTeam.Members.Remove(p);
            DropFlag(p, ctfTeam);
            RemoveFromTeam(p);           
            ResetPlayerColor(p);
            UpdateTabList(p);
        }
    }

    private void AutoAssignTeam(Player p)
    {
        if (Blue.Members.Count > Red.Members.Count)
        {
            JoinTeam(p, Red);
            return;
        }

        if (Red.Members.Count > Blue.Members.Count)
        {
            JoinTeam(p, Blue);
            return;
        }

        bool flag = new Random().Next(2) == 0;
        JoinTeam(p, flag ? Red : Blue);
    }

    private void JoinTeam(Player p, MyCtfTeam team)
    {
        if (team.Members.Count > Opposing(team).Members.Count)
        {
            p.Message("&cThis team is full!");
            return;           
        }
        Get(p).HasFlag = false;
        team.Members.Add(p);
        p.UpdateColor(team.Color);
        Map.Message(p.ColoredName + Config.InfoColor + " joined the " + team.ColoredName + Config.InfoColor + " team");
        p.Message(Config.InfoColor + "You are now on the " + team.ColoredName + Config.InfoColor + " team!");
        //UpdateTabList(p);
        // Trying to find out how to update the name above a player's head only.
        //p.DisplayName = "hi";
        //p.BrushName = "test";       
        //p.name = "test";
        //p.SkinName = "test";
        //p.SuperName = "test";
    }

    private void LeaveTeam(Player p)
    {
        MyCtfTeam team = TeamOf(p);
        if (team == null)
        {
            p.Message(Config.InfoColor + "You are not on a team!");
            return;
        }
        if (RoundInProgress)
        {
            p.Message(Config.InfoColor + "You cannot leave your team during a match!");
            return;
        }
        RemoveFromTeam(p);      
        Map.Message(p.ColoredName + Config.InfoColor + " left the " + team.ColoredName + Config.InfoColor + " team");
        p.Message(Config.InfoColor + "You have left the " + team.ColoredName + Config.InfoColor + " team.");
             
    }

    private void RemoveFromTeam(Player p)
    {
        MyCtfTeam team = TeamOf(p);
        if (team == null)
        {
            return;
        }
        team.Members.Remove(p);
        ResetPlayerColor(p);
    }

    private bool OnOwnTeamSide(int z, MyCtfTeam team)
    {
        int z2 = team.FlagPos.Z;
        int zDivider = cfg.ZDivider;
        if (z2 < zDivider && z < zDivider)
        {
            return true;
        }

        if (z2 > zDivider && z > zDivider)
        {
            return true;
        }

        return false;
    }

    private MyCtfTeam TeamOf(Player p)
    {
        if (Red.Members.Contains(p))
        {
            return Red;
        }

        if (Blue.Members.Contains(p))
        {
            return Blue;
        }

        return null;
    }

    private MyCtfTeam Opposing(MyCtfTeam team)
    {
        if (team != Red)
        {
            return Red;
        }

        return Blue;
    }

    private static MyCtfStats ParseStats(ISqlRecord record)
    {
        MyCtfStats result = default(MyCtfStats);
        result.Points = record.GetInt("Points");
        result.Captures = record.GetInt("Captures");
        result.Tags = record.GetInt("Tags");
        result.Kills = record.GetInt("Kills");
        result.XP = record.GetInt("XP");
        result.Killstreak = record.GetInt("Killstreak");
        return result;
    }

    private static MyCtfStats LoadStats(string name)
    {
        MyCtfStats stats = default(MyCtfStats);
        Database.ReadRows("MyCTF", "*", delegate (ISqlRecord record)
        {
            stats = ParseStats(record);
        }, "WHERE Name=@0", name);
        return stats;
    }

    protected override void SaveStats(Player p)
    {
        MyCtfData ctfData = TryGet(p);
        if (ctfData == null)
        {
            return;
        }

        if (ctfData != null && (ctfData.Points != 0 || ctfData.Captures != 0 || ctfData.Tags != 0) || ctfData.Kills != 0 || ctfData.XP != 0 || ctfData.Killstreak != 0)
        {
            object[] args = new object[7] { ctfData.Points, ctfData.Captures, ctfData.Tags, ctfData.Kills, ctfData.XP, ctfData.Killstreak, p.name };
            if (Database.UpdateRows("MyCTF", "Points=@0, Captures=@1, tags=@2, Kills=@3, XP=@4, Killstreak=@5", "WHERE Name=@6", args) == 0)
            {
                Database.AddRow("MyCTF", "Points, Captures, tags, Kills, XP, Killstreak, Name", args);
            }
        }
    }

    protected override void HookEventHandlers()
    {
        IEvent<OnPlayerDied>.Register(HandlePlayerDied, Priority.High);
        IEvent<OnPlayerChat>.Register(HandlePlayerChat, Priority.High);
        IEvent<OnPlayerCommand>.Register(HandlePlayerCommand, Priority.High);
        IEvent<OnBlockChanging>.Register(HandleBlockChanging, Priority.High);
        IEvent<OnPlayerSpawning>.Register(HandlePlayerSpawning, Priority.High);
        //IEvent<OnTabListEntryAdded>.Register(HandleTabListEntryAdded, Priority.High);
        IEvent<OnSentMap>.Register(HandleSentMap, Priority.High);
        IEvent<OnJoinedLevel>.Register(HandleJoinedLevel, Priority.High);
        IEvent<OnWeaponContact>.Register(HandleWeaponContact, Priority.High);
        base.HookEventHandlers();
    }

    protected override void UnhookEventHandlers()
    {
        IEvent<OnPlayerDied>.Unregister(HandlePlayerDied);
        IEvent<OnPlayerChat>.Unregister(HandlePlayerChat);
        IEvent<OnPlayerCommand>.Unregister(HandlePlayerCommand);
        IEvent<OnBlockChanging>.Unregister(HandleBlockChanging);
        IEvent<OnPlayerSpawning>.Unregister(HandlePlayerSpawning);
        //IEvent<OnTabListEntryAdded>.Unregister(HandleTabListEntryAdded);
        IEvent<OnSentMap>.Unregister(HandleSentMap);
        IEvent<OnJoinedLevel>.Unregister(HandleJoinedLevel);
        IEvent<OnWeaponContact>.Unregister(HandleWeaponContact);
        base.UnhookEventHandlers();
    }

    private void HandlePlayerDied(Player p, ushort deathblock, ref TimeSpan cooldown)
    {
        if (p.level == Map && Get(p).HasFlag)
        {
            MyCtfTeam ctfTeam = TeamOf(p);
            if (ctfTeam != null)
            {
                DropFlag(p, ctfTeam);
            }
        }
    }

    private void HandlePlayerChat(Player p, string message)
    {
        if (p.level != Map || !Get(p).TeamChatting)
        {
            Chat.MessageChat(ChatScope.Level, p, p.group.ColoredName + "• " + p.color + p.prefix + p.ColoredName + ": " + Config.ChatColor + message, Map, null);
            p.cancelchat = true;
            return;
        }

        MyCtfTeam team = TeamOf(p);
        if (team != null)
        {
            string text = team.Color + " - to " + team.Name;
            Chat.MessageChat(ChatScope.Level, p, text + " - λNICK: &f" + message, Map, (Player pl, object arg) => pl.Game.Referee || TeamOf(pl) == team);
            p.cancelchat = true;
        }
    }

    private void HandlePlayerCommand(Player p, string cmd, string args, CommandData data)
    {
        if (p.level == Map && !(cmd != "teamchat"))
        {
            MyCtfData ctfData = Get(p);
            if (ctfData.TeamChatting)
            {
                p.Message("You are no longer chatting with your team!");
            }
            else
            {
                p.Message("You are now chatting with your team!");
            }

            ctfData.TeamChatting = !ctfData.TeamChatting;
            p.cancelcommand = true;
        }
    }

    private void HandleBlockChanging(Player p, ushort x, ushort y, ushort z, ushort block, bool placing, ref bool cancel)
    {
        if (p.level != Map)
        {
            p.Message("You are not on the map!");
            return;
        }

        if (!RoundInProgress)
        {
            p.Message("The round has not started yet!");
            p.RevertBlock(x, y, z);
            cancel = true;
            return;
        }

        MyCtfTeam ctfTeam = TeamOf(p);
        if (ctfTeam == null)
        {
            p.RevertBlock(x, y, z);
            cancel = true;
            p.Message("You are not on a team!");
            return;
        }

        Vec3U16 vec3U = new Vec3U16(x, y, z);
        if (vec3U == Opposing(ctfTeam).FlagPos && !Map.IsAirAt(x, y, z))
        {
            TakeFlag(p, ctfTeam);
        }

        if (vec3U == ctfTeam.FlagPos && !Map.IsAirAt(x, y, z))
        {
            ReturnFlag(p, ctfTeam);
            cancel = true;
        }

        if (Map.IsAirAt(x, y ,z) && (vec3U == Opposing(ctfTeam).FlagPos || vec3U == ctfTeam.FlagPos))
        {
            p.RevertBlock(x, y, z);
            cancel = true;
        }
    }

    private void HandlePlayerSpawning(Player p, ref Position pos, ref byte yaw, ref byte pitch, bool respawning)
    {
        if (p.level != Map)
        {
            return;
        }

        MyCtfTeam ctfTeam = TeamOf(p);
        if (ctfTeam != null)
        {
            if (respawning)
            {
                DropFlag(p, ctfTeam);
            }

            Vec3U16 spawnPos = ctfTeam.SpawnPos;
            pos = Position.FromFeetBlockCoords(spawnPos.X, spawnPos.Y, spawnPos.Z);
            yaw = GetSpawnOrientation(p).RotY;
        }
    }

    //private void HandleTabListEntryAdded(Entity entity, ref string tabName, ref string tabGroup, Player dst)
    //{
    //    if (entity is Player player && player.level == Map)
    //    {
    //        MyCtfTeam ctfTeam = TeamOf(player);
    //        MyCtfData ctfData = TryGet(player);
    //        if (player.Game.Referee)
    //        {
    //            tabGroup = "&2Referees";
    //        }
    //        else if (ctfTeam != null)
    //        {
    //            tabGroup = ctfTeam.ColoredName + " team";
    //        }
    //        else
    //        {
    //            tabGroup = "&7Spectators";
    //        }
    //    }
    //}

    // During the countdown:
    // Players can join the map and select a team with /mc join red/blue
    // After the countdown is over, players that are not in a team yet are assigned a random team.
    private void HandleSentMap(Player p, Level prevLevel, Level level)
    {
        if (level == Map)
        {
            if (!roundStats.ContainsKey(p.truename))
            {
                roundStats.Add(p.truename, new MyCtfStats());
            }
            ResetKillstreak(p);
            OutputMapSummary(p, Map.name, Map.Config);

            // Randomly assigns team regardless if the countdown is still in progress
            // but when countdown is in progress, no auto team should be assigned.
            if (TeamOf(p) == null && RoundInProgress)
            {
                AutoAssignTeam(p);
            }
        }
    }

    private void HandleJoinedLevel(Player p, Level prevLevel, Level level, ref bool announce)
    {
        HandleJoinedCommon(p, prevLevel, level, ref announce);
    }

    protected override void DoRound()
    {
        if (Running)
        {
            roundStats.Clear();
            timer.Set(cfg.Time);
            RoundInProgress = true;
            Player[] items = PlayerInfo.Online.Items;
            Player[] array = items;
            foreach (Player player in array)
            {
                if (player.level == Map)
                {                   
                    PlayerJoinedGame(player);
                    MoveToTeamSpawn(player);                   
                }              
            }
            
            while (Running && RoundInProgress && !HasSomeoneWon() && !EmptyTeam() && !timer.timeout)
            {
                Tick();
                Thread.Sleep(Config.CollisionsCheckInterval);
            }
        }
    }

    private bool HasSomeoneWon()
    {
        if (Blue.Captures < Config.MaxCaptures)
        {
            return Red.Captures >= Config.MaxCaptures;
        }

        return true;
    }

    private void Tick()
    {
        timer.DoTimer();
        int dist = (int)(Config.TagDistance * 32f);
        Player[] items = PlayerInfo.Online.Items;
        Player[] array = items;
        foreach (Player player in array)
        {
            if (player.level != Map)
            {              
                continue;
            }
            UpdateStatusHUD(player);
            UpdateTimerHUD(player);
            UpdateTabList(player);

            MyCtfTeam ctfTeam = TeamOf(player);
            MyCtfData ctfData = Get(player);
            if (ctfData.HasFlag)
            {
                DrawPlayerFlag(player, ctfData);
            }

            if (ctfTeam == null || ctfData.TagCooldown || !OnOwnTeamSide(player.Pos.BlockZ, ctfTeam))
            {
                continue;
            }

            MyCtfTeam ctfTeam2 = Opposing(ctfTeam);
            Player[] items2 = ctfTeam2.Members.Items;
            Player[] array2 = items2;
            foreach (Player player2 in array2)
            {
                if (IGame.InRange(player, player2, dist))
                {
                    MyCtfData ctfData2 = Get(player2);
                    ctfData2.TagCooldown = true;
                    player2.Message(player.ColoredName + " &Stagged you!");
                    PlayerActions.Respawn(player2);
                    if (ctfData2.HasFlag)
                    {
                        DropFlag(player, ctfTeam2);                       
                    }

                    //ctfData.Points += cfg.Tag_PointsGained;
                    //ctfData2.Points -= cfg.Tag_PointsLost;
                    ctfData.Tags++;
                    ctfData2.TagCooldown = false;
                }
            }
        }
    }

    private void ResetPlayerFlag(Player p, MyCtfData data)
    {
        //Vec3S32 lastHeadPos = data.LastHeadPos;
        //ushort x = (ushort)lastHeadPos.X;
        //ushort y = (ushort)lastHeadPos.Y;
        //ushort z = (ushort)lastHeadPos.Z;
        //data.LastHeadPos = default(Vec3S32);
        //Map.BroadcastRevert(x, y, z);
        RemoveFlagBot(p);
    }

    private void DrawPlayerFlag(Player p, MyCtfData data)
    {
        Vec3S32 blockCoords = p.Pos.BlockCoords;
        blockCoords.Y += 3;
        //if (!(blockCoords == data.LastHeadPos))
        //{
        //    ResetPlayerFlag(p, data);
        //    data.LastHeadPos = blockCoords;
        //    ushort x = (ushort)blockCoords.X;
        //    ushort y = (ushort)blockCoords.Y;
        //    ushort z = (ushort)blockCoords.Z;
        //    MyCtfTeam ctfTeam = Opposing(TeamOf(p));
        //    Map.BroadcastChange(x, y, z, ctfTeam.FlagBlock);
        //}
        PlayerBot flagBot = FindFlagBot(p);

        Position playerPos = p.Pos;
        playerPos.Y += Config.FlagBotYOffset;
        flagBot.Pos = playerPos;

        Orientation playerRot = p.Rot;   
        playerRot.HeadX = 0;
        flagBot.Rot = playerRot;       
    }

    public override void EndRound()
    {
        if (RoundInProgress)
        {
            RoundInProgress = false;
            timer.Stop();
            if (Blue.Captures > Red.Captures)
            {
                Map.Message(Blue.ColoredName + Config.InfoColor + " won this round of CTF!");
                foreach (Player p in Blue.Members.Items)
                {
                    if (p == null)
                    {
                        continue;
                    }
                    AwardXP(p, Config.WinXPReward);
                }
            }
            else if (Red.Captures > Blue.Captures)
            {
                Map.Message(Red.ColoredName + Config.InfoColor + " won this round of CTF!");
                foreach (Player p in Red.Members.Items)
                {
                    if (p == null)
                    {
                        continue;
                    }
                    AwardXP(p, Config.WinXPReward);
                }
            }
            else
            {
                Map.Message(Config.InfoColor + "The round ended in a tie!");
            }           
            ResetFlagsState();
            ResetTeams();
            DisplayBestRoundStats();
            foreach (Player player in PlayerInfo.Online.Items)
            {
                DisplayRoundStats(player);                
                ResetPlayerColor(player);
                UpdateStatusHUD(player);
                UpdateTabList(player);
            }           
            Map.Message("Starting next round!");
        }
    }

    private void TakeFlag(Player p, MyCtfTeam team)
    {     
        MyCtfTeam ctfTeam = Opposing(team);

        string message = team.Color + p.DisplayName + Config.InfoColor + " has taken the " + ctfTeam.ColoredName + Config.InfoColor + " team's flag!";
        Map.Message(message);
        Command.Find("Announce").Use(Player.Console, "global " + message);

        MyCtfData ctfData = Get(p);
        ctfData.HasFlag = true;
        SpawnFlagBot(p);
        DrawPlayerFlag(p, ctfData);
    }

    private void ReturnFlag(Player p, MyCtfTeam team)
    {
        Vec3U16 flagPos = team.FlagPos;
        p.RevertBlock(flagPos.X, flagPos.Y, flagPos.Z);
        MyCtfData ctfData = Get(p);
        if (ctfData.HasFlag)
        {
            MyCtfTeam opposing = Opposing(team);
            string message = team.Color + p.DisplayName + Config.InfoColor + " has captured the " + opposing.Color + opposing.Name + Config.InfoColor + " team's flag!";

            Map.Message(message);
            Command.Find("Announce").Use(Player.Console, "global " + message);

            ctfData.HasFlag = false;
            ResetPlayerFlag(p, ctfData);
            //ctfData.Points += cfg.Capture_PointsGained;
            team.Captures++;
            MyCtfTeam ctfTeam = Opposing(team);
            ctfTeam.RespawnFlag(Map);

            IncreaseStat(p, "Captures");
            AwardXP(p, Config.CaptureXPReward);           
        }
        else
        {
            p.Message(Config.InfoColor + "You cannot take your own flag!");
        }
    }

    private void DropFlag(Player p, MyCtfTeam team)
    {
        MyCtfData ctfData = Get(p);
        if (ctfData.HasFlag)
        {
            MyCtfTeam opposing = Opposing(team);
            string message = team.Color + p.DisplayName + Config.InfoColor + " has dropped the " + opposing.Color + opposing.Name + Config.InfoColor + " team's flag!";          
            ctfData.HasFlag = false;
            ResetPlayerFlag(p, ctfData);
            Map.Message(message);
            Command.Find("Announce").Use(Player.Console, "global " + message);
            //ctfData.Points -= cfg.Capture_PointsLost;
            MyCtfTeam ctfTeam = Opposing(team);
            ctfTeam.RespawnFlag(Map);
        }
    }

    private void HandleWeaponContact(Player p, Player opponent)
    {
        MyCtfTeam playerTeam = TeamOf(p);
        MyCtfTeam opponentTeam = TeamOf(opponent);

        if (playerTeam != null && opponentTeam != null && playerTeam != opponentTeam)
        {
            string deathMessage = opponent.ColoredName + Config.InfoColor + " was killed by " + p.ColoredName!;
            if (opponent.HandleDeath(4, GetKillstreakMessage(p)))
            {
                Map.Message(deathMessage);
                IncreaseStat(p, "Kills");
                IncreaseStat(p, "Killstreak");
                AwardXP(p, Config.KillXPReward);
                ResetKillstreak(opponent);
                return;
            }
            p.Message("&cThis player has just respawned!");
        }
    }

    // TODO: use built-in CpeMessage announce type
    protected void Countdown()
    {
        DateTime startTime = DateTime.Now;
        DateTime now = DateTime.Now;
        TimeSpan elapsedTime = now - startTime;

        string message = "";

        while (elapsedTime.Seconds < Config.CountdownTimer)
        {
            if (!Running | RoundInProgress)
            {
                return;
            }
            message = $"global &bMatch starts in &f{Config.CountdownTimer - elapsedTime.Seconds} &bseconds!";

            Thread.Sleep(100); // Prevents the while loop from freezing the server

            if (elapsedTime.Seconds % 10 == 0)
            {
                Command.Find("Announce").Use(Player.Console, message);
            }

            // When the countdown reaches 5, announce the time left every second instead of every ten seconds.
            if (Config.CountdownTimer - elapsedTime.Seconds <= 5 && elapsedTime.Seconds % 1 == 0)
            {
                Command.Find("Announce").Use(Player.Console, message);
            }

            now = DateTime.Now;
            elapsedTime = now - startTime;
        }

        int playerCount = 0;
        foreach (Player pl in Map.players)
        {
            if (!pl.IsAfk && !pl.Game.Referee)
            {
                playerCount++;
            }
        }

        if (playerCount >= 2)
        {
            Command.Find("Announce").Use(Player.Console, $"global &aGood luck!");
            return;
        }

        else
        {
            Command.Find("Announce").Use(Player.Console, $"global &4Need &f2 &4or more players to start!");

            if (Running)
            {
                Thread.Sleep(5000);
                Countdown();
                return;
            }
            return;
        }
    }

    // override start
    // add countdown
    // Spaghetti code but it works tho
    public override void Start(Player p, string map, int rounds)
    {
        p.Message("&5Start called!");
        if (rounds == 0)
        {
            rounds = int.MaxValue;
        }

        map = GetStartMap(p, map);
        if (map == null)
        {
            p.Message("No maps have been setup for {0} yet", GameName);
            return;
        }

        if (!SetMap(map))
        {
            p.Message("Failed to load initial map!");
            return;
        }

        Chat.MessageGlobal("{0} is starting on {1}&S!", GameName, Map.ColoredName);
        Logger.Log(LogType.GameActivity, "[{0}] Game started", GameName);
        StartGame();
        RoundsLeft = rounds;
        Running = true;
        IGame.RunningGames.Add(this);
        OnStateChangedEvent.Call(this);
        HookEventHandlers();       

        Server.StartThread(out var thread, "Game_ " + GameName, RunGame);
        Utils.SetBackgroundMode(thread);
    }

    private void RunGame()
    {
        try
        {
            while (Running && RoundsLeft > 0)
            {
                ResetFlagsState();
                Countdown();
                RoundInProgress = false;
                if (RoundsLeft != int.MaxValue)
                {
                    RoundsLeft--;
                }

                if (Map != null)
                {
                    Logger.Log(LogType.GameActivity, "[{0}] Round started on {1}", GameName, Map.ColoredName);
                }

                DoRound();
                if (Running)
                {
                    EndRound();
                }

                if (Running)
                {
                    VoteAndMoveToNextMap();
                }
            }
            End();
        }
        catch (Exception ex)
        {
            Logger.LogError("Error in game " + GameName, ex);
            Chat.MessageGlobal("&W" + GameName + " disabled due to an error.");
            try
            {
                End();
            }
            catch (Exception ex2)
            {
                Logger.LogError(ex2);
            }
        }

        IGame.RunningGames.Remove(this);
    }

    public static void ClearData(Player p) // Debugging
    {
        p.Extras.Remove("MCG_MYCTF_DATA");
        p.Message("p.Extras data cleared");
    }

    // TODO: move message validation to CmdMyCTF
    public void HandleJoinCmd(Player p, string[] message)
    {
        if (message[1].CaselessEq("Blue"))
        {
            JoinTeam(p, Blue);
        }

        else if (message[1].CaselessEq("Red"))
        {
            JoinTeam(p, Red);
        }

        else
        {
            p.Message("&bPlease enter a valid team name. Usage: &a/MyCTF &ejoin blue/red");
        }
    }

    private bool EmptyTeam()
    {
        if (Red.Members.Count == 0 | Blue.Members.Count == 0)
        {
            return true;
        }
        return false;
    }

    private void MoveToTeamSpawn(Player p)
    {
        MyCtfTeam ctfTeam = TeamOf(p);
        Position spawnPos = new Position(ctfTeam.SpawnPos.X, ctfTeam.SpawnPos.Y, ctfTeam.SpawnPos.Z);
        Orientation spawnOrientation = GetSpawnOrientation(p);
        p.SendPosition(Position.FromFeetBlockCoords(spawnPos.X, spawnPos.Y, spawnPos.Z), spawnOrientation);
    }

    private static void ResetPlayerColor(Player p)
    {
        p.SetColor(PlayerInfo.DefaultColor(p));             
    }

    private Orientation GetSpawnOrientation(Player p)
    {
        Orientation orientation = new Orientation(0, 0);
        Vec3U16 flagPos = TeamOf(p).FlagPos;
        Vec3U16 enemyFlagPos = Opposing(TeamOf(p)).FlagPos;
        
        int dx = flagPos.X - enemyFlagPos.X;
        int dz = flagPos.Z - enemyFlagPos.Z;

        if (Math.Abs(dx) >= Math.Abs(dz))
        {
            // Look alongside the x-axis
            // yaw is 90 or 270

            // if it's negative, yaw is 90
            if (dx <= 0)
            {
                orientation.RotY = Orientation.DegreesToPacked(90);
            }
            // if it's positive, yaw is 270
            else if (dx > 0)
            {
                orientation.RotY = Orientation.DegreesToPacked(270);              
            }
        }
        else if (Math.Abs(dx)  < Math.Abs(dz))  
        {
            // Look alongside the z-axis
            // yaw is 0 or 180

            // if it's negative, yaw is 180
            if (dz <= 0)
            {
                orientation.RotY = Orientation.DegreesToPacked(180);               
            }

            // if it's positive, yaw is 0
            else if (dz > 0)
            {
                orientation.RotY = Orientation.DegreesToPacked(0);
            }
        }
        else
        {
            orientation.RotY = p.Rot.RotY;
        }
        return orientation;
    }
    public string GetFlagOf(Player p)
    {
        MyCtfData ctfData = Get(p);
        if (TeamOf(p) == null || !ctfData.HasFlag)
        {
            return "";
        }
        MyCtfTeam team = TeamOf(p);
        MyCtfTeam opposing = Opposing(team);
        return opposing.Color + "▒";
    }

    public string GetGroupOf(Player p)
    {
        MyCtfTeam ctfTeam = TeamOf(p);
        MyCtfData ctfData = Get(p);

        if (p.Level != Map)
        {
            return "&fOn " + p.Level.MapName;
        }
        if (p.Game.Referee)
        {
            return "&2Referees";
        }
        else if (ctfTeam != null)
        {
            return ctfTeam.ColoredName + " team ";
        }
        else if (Running)
        {
            return "&7Spectators";
        }
        else
        {
            return null;
        }
    }

    public void UpdateTabList(Player p, bool self = true)
    {       
        if (!PlayerInfo.Online.Items.Contains(p))
        {
            return;
        }
        OnCTFPlayerInfoUpdatedEvent.Call(p, GetFlagOf(p), GetGroupOf(p));
        Player[] items = PlayerInfo.Online.Items;
        Player[] array = items;
        foreach (Player player in array)
        {
            if (p == player)
            {
                if (self)
                {
                    TabList.Add(player, p, byte.MaxValue);
                }
            }
            else if (Server.Config.TablistGlobal || p.level == player.level)
            {
                if (player.CanSee(p))
                {
                    TabList.Add(player, p, p.id);
                }

                if (p.CanSee(player))
                {
                    TabList.Add(p, player, player.id);
                }
            }
        }
    }

    private void SpawnFlagBot(Player p)
    {
        MyCtfTeam team = TeamOf(p);
        if (team == null || p.Level != Map)
        {
            return;
        }
        string botName = team == Blue ? "Red" : "Blue";
        botName = botName.Replace(' ', '_');
        
        PlayerBot flagBot = new PlayerBot(botName, Map);
        flagBot.Owner = Player.Console.name;
        flagBot.CreationDate = DateTime.UtcNow.ToUnixTime();
        flagBot.DisplayName = "empty";
        flagBot.Pos = p.Pos;
        flagBot.Model = (team == Blue ? Red.FlagBlock.ToString() : Blue.FlagBlock.ToString()) + "|" + Config.FlagBotScale;
   
        Orientation rot = new Orientation();
        rot.RotY = p.Rot.RotY;
        rot.HeadX = 0;
        flagBot.Rot = rot;

        PlayerBot.Add(flagBot);
    }

    private void RemoveFlagBot(Player p)
    {
        PlayerBot flagBot = FindFlagBot(p);
        if (flagBot != null)
        {
            PlayerBot.Remove(flagBot);
        }
    }

    private PlayerBot FindFlagBot(Player p)
    {
        MyCtfTeam team = TeamOf(p);
        PlayerBot flagBot = null;
        foreach (PlayerBot bot in Map.Bots.Items)
        {
            if (bot.name == Opposing(team).Name)
            {
                flagBot = bot;
                break;
            }
        }
        return flagBot;
    }

    private void AwardXP(Player p, int amount)
    {
        IncreaseStat(p, "XP", amount);
        string message = "&a+" + amount + " &aXP";
        p.Message(message);
        p.SendCpeMessage(CpeMessageType.SmallAnnouncement, message);

        SaveStats(p);
        CheckForPromotion(p);        
    }

    private void UpdateStatusHUD(Player p)
    {
        if (p == null || Map == null || Blue == null || Red == null)
        {
            // TODO: ResetStatusHUD()
            //p.SendCpeMessage(CpeMessageType.Status1, "");
            //p.SendCpeMessage(CpeMessageType.Status2, "");
            //p.SendCpeMessage(CpeMessageType.Status3, "");
            return;
        }

        MyCtfData ctfData = Get(p);
        string mapStatus = Config.InfoColor + "Rank: " + p.group.ColoredName + Config.InfoColor + " | " + "&a" + (ctfData != null ? ctfData.XP.ToString() : "?") + " XP" + Config.InfoColor + " | " + "Map: " + "&f" + Map.name;
        string blueStatus = Blue.Color + Blue.Name + ": " + Config.InfoColor + "Players: " + "&f" + Blue.Members.Count + Config.InfoColor + " | " + "Captures: &f" + Blue.Captures + "/" + "&f" + Config.MaxCaptures;
        string redStatus = Red.Color + Red.Name + ": " + Config.InfoColor + "Players: " + "&f" + Red.Members.Count + Config.InfoColor + " | " + "Captures: &f" + Red.Captures + "/" + "&f" + Config.MaxCaptures;
      
        p.SendCpeMessage(CpeMessageType.Status1, blueStatus);
        p.SendCpeMessage(CpeMessageType.Status2, redStatus);
        p.SendCpeMessage(CpeMessageType.Status3, mapStatus);      
    }

    private void UpdateTimerHUD(Player p)
    {
        string timerStatus = Config.InfoColor + "Time left: " + "&f" + timer.Display();
        int timeLeft = timer.GetSecondsLeft();
        string timeLeftMessage = "&f" + timeLeft + (timeLeft == 1 ? " &4Second" : " &4Seconds") + " Left!";
        p.SendCpeMessage(CpeMessageType.BottomRight1, timerStatus);
        if (timeLeft == 60 || (timeLeft <= 30 && timeLeft % 10 == 0) || timeLeft <= 10)
        {
            if (timeLeft != 0 && timer.secondHasPassed)
            {
                p.SendCpeMessage(CpeMessageType.Announcement, timeLeftMessage);
                p.SendCpeMessage(CpeMessageType.Normal, timeLeftMessage);
            }
        }
    }

    private int GetNextRankRequirement(Group rank)
    {
        if (rank.Name == "Unskilled")
        {
            return 100;
        }
        Group previousRank = GetPreviousOrNextRank(rank, true);
        return (int)Math.Round(GetNextRankRequirement(previousRank) * 1.4);        
    }

    private Group GetPreviousOrNextRank(Group rank, bool previous)
    {   
        if (rank.Name == "Banned")
        {
            return null;
        }
        List<Group> ranks = Group.AllRanks;
        for (int i = 0; i < ranks.Count; i++)
        {
            if (ranks[i] == rank)
            {
                // return the player's previous rank
                if (previous)
                {
                    return ranks[i - 1];
                }
                // return the player's next rank
                else if (!previous)
                {
                    return ranks[i + 1];
                }             
            }
        }
        return null;
    }

    private bool HasMaxRank(Player p)
    {
        Group rank = p.group;
        if (rank.Name == "Flagmaster" || rank.Name == "Moderator" || rank.Name == "Developer")
        {
            return true;
        }      
        return false;
    }

    public void ShowXP(Player p)
    {
        MyCtfData ctfData = Get(p);
        if (ctfData == null)
        {
            p.Message("&cCould not retrieve your XP. Is CTF running?");
            return;
        }
        Group rank = p.group;
        int xp = ctfData.XP;
        if (HasMaxRank(p))
        {
            p.Message(Config.InfoColor + "You have " + "&a" + xp.ToString() + " XP" + Config.InfoColor + " and you cannot rank up any further!");
            return;
        }  
        int required = GetNextRankRequirement(rank);
        Group nextRank = GetPreviousOrNextRank(rank, false);
        p.Message(Config.InfoColor + "You have " + "&a" + xp.ToString() + " XP" + Config.InfoColor + " and need " + "&a" + (required - xp).ToString() + " XP" + Config.InfoColor + " more to rank up to " + nextRank.ColoredName + Config.InfoColor + "!");
    }

    private void CheckForPromotion(Player p)
    {
        if (HasMaxRank(p))
        {
            return;
        }
        Group rank = p.group;
        MyCtfData ctfData = Get(p);
        int xp = ctfData.XP;
        int requirement = GetNextRankRequirement(rank);
        if (xp >= requirement)
        {
            Group nextRank = GetPreviousOrNextRank(rank, false);
            Command.Find("setrank").Use(Player.Console, p.truename + " " + nextRank.Name);
            MyCtfTeam team = TeamOf(p);
            if (team != null)
            {
                p.Message("Resetting your color to " + team.Color);
                p.UpdateColor(team.Color);
            }
            Map.Message(p.ColoredName + Config.InfoColor + " has reached the rank " + nextRank.ColoredName + Config.InfoColor + "!");
            p.SendCpeMessage(CpeMessageType.Announcement, Config.InfoColor + "You are now ranked " + nextRank.ColoredName + Config.InfoColor + "!");
        }
    }

    private void IncreaseStat(Player p, string stat, int amount = 1)
    {
        string name = p.truename;
        MyCtfStats stats = roundStats[name];
        MyCtfData ctfData = Get(p);
        if (stat.CaselessEq("Kills"))
        {
            stats.Kills += amount;
            ctfData.Kills += amount;
        }
        else if (stat.CaselessEq("Captures"))
        {
            stats.Captures += amount;
            ctfData.Captures += amount;
        }
        else if (stat.CaselessEq("XP"))
        {
            stats.XP += amount;
            ctfData.XP += amount;
        }
        else if (stat.CaselessEq("Killstreak"))
        {
            stats.Killstreak += amount;
            if (stats.Killstreak > ctfData.Killstreak)
            {
                ctfData.Killstreak = stats.Killstreak;
            }
        }
        else
        {
            p.Message("&cAn error occurred while updating your round stats. Tell Bruceja he is a bad coder.");
        }
        roundStats[name] = stats;
    }

    private void DisplayRoundStats(Player p)
    {
        string name = p.truename;
        p.Message(Config.InfoColor + "Your stats for this round:");
        p.Message("&f" + roundStats[name].Kills.ToString() + Config.InfoColor + " kills.");
        p.Message("&f" + roundStats[name].Captures.ToString() + Config.InfoColor + " captures.");
        p.Message(Config.InfoColor + "You earned " + "&a" + roundStats[name].XP.ToString() + " XP" + Config.InfoColor + " this round.");
    }

    private void DisplayBestRoundStats()
    {
        int displayCount = Config.RoundStatsSummaryCount;
        if (roundStats.Keys.Count < displayCount)
        {
            displayCount = roundStats.Keys.Count;
        }

        List<Player> players = new List<Player>();
        foreach (string name in roundStats.Keys)
        {
            Player player = PlayerInfo.FindExact(name);
            players.Add(player);
        }
        
        players.Sort((a, b) => roundStats[a.truename].Kills.CompareTo(roundStats[b.truename].Kills));
        players.Reverse();
        Map.Message(Config.InfoColor + "Most Kills:");
        for (int i = 0; i < displayCount; i++)
        {
            int placement = i + 1;
            Player player = players[i];
            Map.Message(Config.InfoColor + placement.ToString() + ". " + player.ColoredName + Config.InfoColor + " - " + "&f" + roundStats[player.truename].Kills.ToString() + Config.InfoColor + ".");
        }

        players.Sort((a, b) => roundStats[a.truename].Captures.CompareTo(roundStats[b.truename].Captures));
        players.Reverse();
        Map.Message(Config.InfoColor + "Most Captures:");
        for (int i = 0; i < displayCount; i++)
        {
            int placement = i + 1;
            Player player = players[i];
            Map.Message(Config.InfoColor + placement.ToString() + ". " + player.ColoredName + Config.InfoColor + " - " + "&f" + roundStats[player.truename].Captures.ToString() + Config.InfoColor + ".");
        }
    }

    private string GetKillstreakMessage(Player p)
    {
        int killstreak = roundStats[p.truename].Killstreak;
        // Need to increment here because the message gets sent before the stat is updated.
        killstreak++;
        string format = p.ColoredName + Config.InfoColor;

        switch (killstreak)
        {
            case 1:
                return "";  
            case 2:
                return format + " is awesome! - double kill!";
            case 3:
                return format + " is amazing! - triple kill!";
            case 4:
                return format + " is insane! - quadruple kill!";
            case 5:
                return format + " is OUTRAGEOUS! - quintuple kill!";
            case 6:
                return format + " is HOT! - sextuple kill!";
            default:
                return format + " is UNSTOPPABLE! - " + killstreak.ToString() + " killstreak!";
        }
    }

    private void ResetKillstreak(Player p)
    {
        string name = p.truename;
        MyCtfStats stats = roundStats[name];
        stats.Killstreak = 0;
        roundStats[name] = stats;
    }
}   
