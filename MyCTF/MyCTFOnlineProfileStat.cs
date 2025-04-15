using MCGalaxy.Commands;
using MCGalaxy.DB;
using MCGalaxy.Eco;
using MCGalaxy.Modules.Awards;
using MCGalaxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace MyCTF;

internal class MyCTFOnlineProfileStat : MyCTFProfileStat
{
    public static List<OnlineStatPrinter> Stats = new List<OnlineStatPrinter>
    {
        SetPlayerData,
        CoreLine,
        MyCTFLine,
        delegate(Player p, Player who)
        {
            MiscLine(p, who.name, who.TimesDied, who.money);
        },
        TimeSpentLine,
        LoginLine,
        delegate(Player p, Player who)
        {
            LoginsLine(p, who.TimesVisited, who.TimesBeenKicked);
        },
        delegate(Player p, Player who)
        {
            BanLine(p, who.name);
        },
        delegate(Player p, Player who)
        {
            SpecialGroupLine(p, who.name);
        },
        delegate(Player p, Player who)
        {
            IPLine(p, who.name, who.ip);
        },
        IdleLine,
        EntityLine
    };

    public static void SetPlayerData(Player p, Player who)
    {
        hasAllAwards = MyCTFOfflineProfileStat.HasAllAwards(who.truename);
        ctfData = MyCTFGame.GetOfflineStats(who);
    }

    public static void CoreLine(Player p, Player who)
    {
        MyCTFOfflineProfileStat.CoreLine(p, PlayerDB.Match(who, who.name));
    }

    public static void MyCTFLine(Player p, Player who)
    {
        MyCTFOfflineProfileStat.MyCTFLine(p, PlayerDB.Match(who, who.name));
    }

    public static void MiscLine(Player p, string name, int deaths, int money)
    {
        if (Economy.Enabled)
        {
            p.Message("  &a{0} &cdeaths{4}, &a{2} &S{3}, &f{1} {4}awards", deaths, PlayerAwards.Summarise(name), money, Server.Config.Currency, hasAllAwards ? "&6" : "&S");
        }
        else
        {
            p.Message("  &a{0} &cdeaths{2}, &f{1} {2}awards", deaths, PlayerAwards.Summarise(name), hasAllAwards ? "&6" : "&S");
        }
    }

    public static void TimeSpentLine(Player p, Player who)
    {
        TimeSpan value = DateTime.UtcNow - who.SessionStartTime;
        p.Message("  {2}Spent &a{0} {2}on the server, &a{1} {2}this session", who.TotalTime.Shorten(), value.Shorten(), hasAllAwards ? "&6" : "&S");
    }

    public static void LoginLine(Player p, Player who)
    {
        p.Message("  {1}First login &a{0}{1}, and is currently &aonline", who.FirstLogin.ToString("yyyy-MM-dd"), hasAllAwards ? "&6" : "&S");
    }

    public static void LoginsLine(Player p, int logins, int kicks)
    {
        p.Message("  {2}Logged in &a{0} {2}times, &c{1} {2}of which ended in a kick", logins, kicks, hasAllAwards ? "&6" : "&S");
    }

    public static void BanLine(Player p, string name)
    {
        if (Group.BannedRank.Players.Contains(name))
        {
            Ban.GetBanData(name, out var banner, out var reason, out var _, out var _);
            if (banner != null)
            {
                p.Message("  Banned for {0} by {1}", reason, p.FormatNick(banner));
            }
            else
            {
                p.Message("  Is banned");
            }
        }
    }

    public static void SpecialGroupLine(Player p, string name)
    {
        name = Server.ToRawUsername(name);
        string a = Server.ToRawUsername(Server.Config.OwnerName);
        if (Server.Devs.CaselessContains(name))
        {
            p.Message("  {1}Player is an &9{0} Developer", Server.SoftwareName, hasAllAwards ? "&6" : "&S");
        }

        if (a.CaselessEq(name))
        {
            p.Message("  {0}Player is the &cServer owner", hasAllAwards ? "&6" : "&S");
        }
    }

    public static void IPLine(Player p, string name, string ip)
    {
        ItemPerms itemPerms = CommandExtraPerms.Find("WhoIs", 1);
        if (itemPerms.UsableBy(p))
        {
            string text = ip;
            if (Server.bannedIP.Contains(ip))
            {
                text = "&8" + ip + ", which is banned";
            }

            p.Message("  The IP of " + text);
            if (Server.Config.WhitelistedOnly && Server.whiteList.Contains(name))
            {
                p.Message("  Player is &fWhitelisted");
            }
        }
    }

    public static void IdleLine(Player p, Player who)
    {
        TimeSpan value = DateTime.UtcNow - who.LastAction;
        if (who.afkMessage != null)
        {
            p.Message("  {2}Idle for {0} (AFK {1}{2})", value.Shorten(), who.afkMessage, hasAllAwards ? "&6" : "&S");
        }
        else if (value.TotalMinutes >= 1.0)
        {
            p.Message("  {1}Idle for {0}", value.Shorten(), hasAllAwards ? "&6" : "&S");
        }
    }

    public static void EntityLine(Player p, Player who)
    {
        bool flag = !who.SkinName.CaselessEq(who.truename);
        bool flag2 = !who.Model.CaselessEq("humanoid") && !who.Model.CaselessEq("human");
        if (flag && flag2)
        {
            p.Message("  Skin: &f{0} &Smodel: &f{1}", who.SkinName, who.Model);
        }
        else if (flag)
        {
            p.Message("  Skin: &f{0}", who.SkinName);
        }
        else if (flag2)
        {
            p.Message("  Model: &f{0}", who.Model);
        }
    }
}
