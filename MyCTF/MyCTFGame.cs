




using System;
using System.Collections.Generic;
using System.Threading;
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

namespace MCGalaxy.Modules.Games.MyCTF;

public class MyCTFGame : RoundsGame
{
    private struct MyCtfStats
    {
        public int Points;

        public int Captures;

        public int Tags;
    }

    private MyCTFMapConfig cfg = new MyCTFMapConfig();

    public MyCTFConfig Config = new MyCTFConfig();

    private MyCtfTeam Red = new MyCtfTeam("Red", "&c");

    private MyCtfTeam Blue = new MyCtfTeam("Blue", "&9");

    public static MyCTFGame Instance = new MyCTFGame();

    private const string myctfExtrasKey = "MCG_MYCTF_DATA";

    private static ColumnDesc[] myctfTable = new ColumnDesc[5]
    {
        new ColumnDesc("ID", ColumnType.Integer, 0, autoInc: true, priKey: true, notNull: true),
        new ColumnDesc("Name", ColumnType.VarChar, 20),
        new ColumnDesc("Points", ColumnType.UInt24),
        new ColumnDesc("Captures", ColumnType.UInt24),
        new ColumnDesc("tags", ColumnType.UInt24)
    };

    public override string GameName => "MyCTF";

    protected override string WelcomeMessage => "&9Capture the Flag &Sis running! Type &T/MyCTF go &Sto join";
    private const int countdownTimer = 5;
    private const string infoColor = "&6";

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


        // TODO:
        // Save player in some dictionary. When the plugin unloads, the players in the dictionary
        // will have their MyCtf key in p.Extras set to null.
        // or: on plugin unload, cast value back to object?
        // or... ?

        // This line fixes the error but:
        // Works when just loading the plugin for the first time
        // When reloading the plugin the following happens:
        // Using /mc start bruceja7 and then using /mc status results in ObjectNullReference
        // After the teams have been assigned using /mc status works fine
        // Update: noticed there's a small window between teams being assigned and /mc status not returning
        // an ObjectNullReference anymore when trying to show CtfData
        // Cannot kill players with the lasergun anymore; lasergun seems to not be able to find the instance
        // of MyCTFGame anymore. Maybe setup an event that listens to when lasergun is used?
        // 
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
        MyCtfData playerData = TryGet(p);
        p.Message($"Captures: {playerData.Captures.ToString()}");
        p.Message($"Tags: {playerData.Tags.ToString()}");
        p.Message($"Points: {playerData.Points.ToString()}");
        p.Message($"HasFlag: {playerData.HasFlag.ToString()}");
        p.Message($"TagCooldown {playerData.TagCooldown.ToString()}");
        p.Message($"TeamChatting: {playerData.TeamChatting.ToString()}");
        p.Message($"LastHeadPos: {playerData.LastHeadPos.ToString()}");
        p.Message($"Your team is: {TeamOf(p).Name}");
    }

    protected override void StartGame()
    {
        //Player me = PlayerInfo.FindExact("Bruceja");

        //if (!me.Extras.Contains("MCG_MYCTF_DATA"))
        //{
        //    me.Message("Your extras dictionary is empty!");
        //}
        //else
        //{
        //    me.Message("You still have leftover data!");
        //}


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
        Blue.Members.Clear();
        Red.Members.Clear();
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
            ctfTeam.Members.Remove(p);
            DropFlag(p, ctfTeam);
            ResetPlayerColor(p);
        }
        ClearData(p); // For debugging only
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
        Map.Message(p.ColoredName + " &Sjoined the " + team.ColoredName + " &Steam");
        p.Message("You are now on the " + team.ColoredName + " team!");
        //TabList.Update(p, self: true);
        TabList.Add(p, p, byte.MaxValue);

        Map.Message($"This player is on team {team.Name}");
        Map.Message($"Setting {p.name}'s color to {team.Color}");       
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
        if (ctfData != null && (ctfData.Points != 0 || ctfData.Captures != 0 || ctfData.Tags != 0))
        {
            object[] args = new object[4] { ctfData.Points, ctfData.Captures, ctfData.Tags, p.name };
            if (Database.UpdateRows("MyCTF", "Points=@0, Captures=@1, tags=@2", "WHERE Name=@3", args) == 0)
            {
                Database.AddRow("MyCTF", "Points, Captures, tags, Name", args);
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
        IEvent<OnTabListEntryAdded>.Register(HandleTabListEntryAdded, Priority.High);
        IEvent<OnSentMap>.Register(HandleSentMap, Priority.High);
        IEvent<OnJoinedLevel>.Register(HandleJoinedLevel, Priority.High);
        IEvent<OnWeaponContact>.Register(HandleWeaponContact, Priority.High);
        base.HookEventHandlers();
        Map.Message("Event handlers hooked");
    }

    protected override void UnhookEventHandlers()
    {
        IEvent<OnPlayerDied>.Unregister(HandlePlayerDied);
        IEvent<OnPlayerChat>.Unregister(HandlePlayerChat);
        IEvent<OnPlayerCommand>.Unregister(HandlePlayerCommand);
        IEvent<OnBlockChanging>.Unregister(HandleBlockChanging);
        IEvent<OnPlayerSpawning>.Unregister(HandlePlayerSpawning);
        IEvent<OnTabListEntryAdded>.Unregister(HandleTabListEntryAdded);
        IEvent<OnSentMap>.Unregister(HandleSentMap);
        IEvent<OnJoinedLevel>.Unregister(HandleJoinedLevel);
        IEvent<OnWeaponContact>.Unregister(HandleWeaponContact);
        base.UnhookEventHandlers();
        Map.Message("Event handlers unhooked");
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

    private void HandleTabListEntryAdded(Entity entity, ref string tabName, ref string tabGroup, Player dst)
    {
        if (entity is Player player && player.level == Map)
        {
            MyCtfTeam ctfTeam = TeamOf(player);
            if (player.Game.Referee)
            {
                tabGroup = "&2Referees";
            }
            else if (ctfTeam != null)
            {
                tabGroup = ctfTeam.ColoredName + " team";
            }
            else
            {
                tabGroup = "&7Spectators";
            }
        }
    }

    // BUG: When player joins map during countdown, they are randomly assigned to a team.
    // if (TeamOf(p) == null && RoundInProgress) results in no players being assigned to a random team
    // when the countdown hits 0.

    // During the countdown:
    // Players can join the map and select a team with /mc join red/blue
    // After the countdown is over, players that are not in a team yet should be assigned a random team.
    private void HandleSentMap(Player p, Level prevLevel, Level level)
    {
        Map.Message($"HandleSentMap called for player {p.name}");
        if (level == Map)
        {
            OutputMapSummary(p, Map.name, Map.Config);

            // Randomly assigns team regardless if the countdown is still in progress
            // but when countdown is in progress, no auto team should be assigned.
            if (TeamOf(p) == null && RoundInProgress)
            {
                Map.Message($"Assigning random team for player {p.name}");
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
        Map.Message("&bDoRound called!");
        if (Running)
        {
            RoundInProgress = true;
            Player[] items = PlayerInfo.Online.Items;
            Player[] array = items;
            foreach (Player player in array)
            {
                Map.Message($"&aNow looking at player {player.name}");
                if (player.level == Map)
                {
                    Map.Message("&aSetting this player to a random team!");
                    PlayerJoinedGame(player);
                }
                MoveToTeamSpawn(player);
            }
            while (Running && RoundInProgress && !HasSomeoneWon() && !EmptyTeam())
            {
                Tick();
                Thread.Sleep(Config.CollisionsCheckInterval);
            }
        }
    }

    private bool HasSomeoneWon()
    {
        if (Blue.Captures < cfg.RoundPoints)
        {
            return Red.Captures >= cfg.RoundPoints;
        }

        return true;
    }

    private void Tick()
    {
        int dist = (int)(Config.TagDistance * 32f);
        Player[] items = PlayerInfo.Online.Items;
        Player[] array = items;
        foreach (Player player in array)
        {
            if (player.level != Map)
            {
                continue;
            }

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

                    ctfData.Points += cfg.Tag_PointsGained;
                    ctfData2.Points -= cfg.Tag_PointsLost;
                    ctfData.Tags++;
                    ctfData2.TagCooldown = false;
                }
            }
        }
    }

    private void ResetPlayerFlag(Player p, MyCtfData data)
    {
        Vec3S32 lastHeadPos = data.LastHeadPos;
        ushort x = (ushort)lastHeadPos.X;
        ushort y = (ushort)lastHeadPos.Y;
        ushort z = (ushort)lastHeadPos.Z;
        data.LastHeadPos = default(Vec3S32);
        Map.BroadcastRevert(x, y, z);
    }

    private void DrawPlayerFlag(Player p, MyCtfData data)
    {
        Vec3S32 blockCoords = p.Pos.BlockCoords;
        blockCoords.Y += 3;
        if (!(blockCoords == data.LastHeadPos))
        {
            ResetPlayerFlag(p, data);
            data.LastHeadPos = blockCoords;
            ushort x = (ushort)blockCoords.X;
            ushort y = (ushort)blockCoords.Y;
            ushort z = (ushort)blockCoords.Z;
            MyCtfTeam ctfTeam = Opposing(TeamOf(p));
            Map.BroadcastChange(x, y, z, ctfTeam.FlagBlock);
        }
    }

    public override void EndRound()
    {
        if (RoundInProgress)
        {
            RoundInProgress = false;
            if (Blue.Captures > Red.Captures)
            {
                Map.Message(Blue.ColoredName + infoColor + " won this round of CTF!");
            }
            else if (Red.Captures > Blue.Captures)
            {
                Map.Message(Red.ColoredName + infoColor + " won this round of CTF!");
            }
            else
            {
                Map.Message(infoColor + "The round ended in a tie!");
            }

            ResetTeams();
            ResetFlagsState();
            foreach (Player player in PlayerInfo.Online.Items)
            {
                ResetPlayerColor(player);
            }
            Map.Message("Starting next round!");
        }
    }

    private void TakeFlag(Player p, MyCtfTeam team)
    {     
        MyCtfTeam ctfTeam = Opposing(team);

        string message = team.Color + p.DisplayName + infoColor + " has taken the " + ctfTeam.ColoredName + infoColor + " team's flag!";
        Map.Message(message);
        Command.Find("Announce").Use(Player.Console, "global " + message);

        MyCtfData ctfData = Get(p);
        ctfData.HasFlag = true;
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
            string message = team.Color + p.DisplayName + infoColor + " has captured the " + opposing.Color + opposing.Name + infoColor + " team's flag!";

            Map.Message(message);
            Command.Find("Announce").Use(Player.Console, "global " + message);

            ctfData.HasFlag = false;
            ResetPlayerFlag(p, ctfData);
            ctfData.Points += cfg.Capture_PointsGained;
            ctfData.Captures++;
            team.Captures++;
            MyCtfTeam ctfTeam = Opposing(team);
            ctfTeam.RespawnFlag(Map);
        }
        else
        {
            p.Message(infoColor + "You cannot take your own flag!");
        }
    }

    private void DropFlag(Player p, MyCtfTeam team)
    {
        MyCtfData ctfData = Get(p);
        if (ctfData.HasFlag)
        {
            MyCtfTeam opposing = Opposing(team);
            string message = team.Color + p.DisplayName + infoColor + " has dropped the " + opposing.Color + opposing.Name + infoColor + " team's flag!";          
            ctfData.HasFlag = false;
            ResetPlayerFlag(p, ctfData);
            Map.Message(message);
            Command.Find("Announce").Use(Player.Console, "global " + message);
            ctfData.Points -= cfg.Capture_PointsLost;
            MyCtfTeam ctfTeam = Opposing(team);
            ctfTeam.RespawnFlag(Map);
        }
    }

    private void HandleWeaponContact(Player p, Player opponent)
    {
        // On plugin reload this method is not being called anymore
        // Reloading lavalaser plugin does not fix it
        // Loading the lavalaser plugin after starting MyCTF does not fix it

        Map.Message("HandleWeaponContact called"); // Debugging
        p.Message("HandleWeaponContact called");

        MyCtfTeam playerTeam = TeamOf(p);
        MyCtfTeam opponentTeam = TeamOf(opponent);
        p.Message("Your team is {0}", playerTeam.Name); // Debugging
        p.Message("Opponent's team is {0}", opponentTeam.Name);

        if (playerTeam != opponentTeam)
        {
            string deathMessage = opponent.ColoredName + infoColor + " was killed by " + p.ColoredName!;
            opponent.HandleDeath(4, deathMessage);
        }
    }

    // TODO: use built-in CpeMessage announce type
    protected void Countdown()
    {
        DateTime startTime = DateTime.Now;
        DateTime now = DateTime.Now;
        TimeSpan elapsedTime = now - startTime;

        string message = "";

        while (elapsedTime.Seconds < countdownTimer)
        {
            if (!Running | RoundInProgress)
            {
                return;
            }
            message = $"global &bMatch starts in &f{countdownTimer - elapsedTime.Seconds} &bseconds!";

            Thread.Sleep(100); // Prevents the while loop from freezing the server

            if (elapsedTime.Seconds % 10 == 0)
            {
                Command.Find("Announce").Use(Player.Console, message);
            }

            // When the countdown reaches 5, announce the time left every second instead of every ten seconds.
            if (countdownTimer - elapsedTime.Seconds <= 5 && elapsedTime.Seconds % 1 == 0)
            {
                Command.Find("Announce").Use(Player.Console, message);
            }

            now = DateTime.Now;
            elapsedTime = now - startTime;
        }

        int playerCount = 0;
        foreach (Player pl in Map.players)
        {
            Map.Message($"Player {pl.name} is AFK? {pl.IsAfk}. is Ref? {pl.Game.Referee}");
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
        Map.Message("&6RunGame called!");
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
        Map.Message("This player's team is " + ctfTeam.Name);
        Position spawnPos = new Position(ctfTeam.SpawnPos.X, ctfTeam.SpawnPos.Y, ctfTeam.SpawnPos.Z);
        Map.Message($"This player will be sent to {ctfTeam.SpawnPos.X}, {ctfTeam.SpawnPos.Y}, {ctfTeam.SpawnPos.Z}");
        Orientation spawnOrientation = GetSpawnOrientation(p);
        p.SendPosition(Position.FromFeetBlockCoords(spawnPos.X, spawnPos.Y, spawnPos.Z), spawnOrientation);
    }

    private void ResetPlayerColor(Player p)
    {
        Map.Message($"Resetting {p.name}'s color");
        p.UpdateColor(PlayerInfo.DefaultColor(p));
               
    }

    private Orientation GetSpawnOrientation(Player p)
    {
        //Position middlePos = new((int)Math.Floor(Map.Length / 2.0), 0, (int)Math.Floor(Map.Width / 2.0));
        //Map.Message("The middlePos is " + middlePos.X + " " + middlePos.Y + " " + middlePos.Z);

        Orientation orientation = new Orientation(0, 0);
        Vec3U16 flagPos = TeamOf(p).FlagPos;
        Vec3U16 enemyFlagPos = Opposing(TeamOf(p)).FlagPos;
        
        int dx = flagPos.X - enemyFlagPos.X;
        int dz = flagPos.Z - enemyFlagPos.Z;

        if (Math.Abs(dx) >= Math.Abs(dz))
        {
            Map.Message("&eLooking alongside x-axis");
            // Look alongside the x-axis
            // yaw is 90 or 270

            // if it's negative, yaw is 90
            if (dx <= 0)
            {
                Map.Message("&eYaw is 90");
                orientation.RotY = Orientation.DegreesToPacked(90);
            }
            // if it's positive, yaw is 270
            else if (dx > 0)
            {
                Map.Message("&eYaw is 270");
                orientation.RotY = Orientation.DegreesToPacked(270);              
            }

        }
        else if (Math.Abs(dx)  < Math.Abs(dz))  
        {
            Map.Message("&eLooking alongside z-axis");
            // Look alongside the z-axis
            // yaw is 0 or 180

            // if it's negative, yaw is 180
            if (dz <= 0)
            {
                Map.Message("&eYaw is 180");
                orientation.RotY = Orientation.DegreesToPacked(180);               
            }

            // if it's positive, yaw is 0
            else if (dz > 0)
            {
                Map.Message("&eYaw is 0");
                orientation.RotY = Orientation.DegreesToPacked(0);
            }
        }
        else
        {
            Map.Message("&eReturning player rotation");
            orientation.RotY = p.Rot.RotY;
        }
        return orientation;
    }
}
