using MCGalaxy;
using MCGalaxy.DB;
using MCGalaxy.Modules.Awards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyCTF;

internal class MyCTFOfflineProfileStat : MyCTFProfileStat
{
    public static List<OfflineStatPrinter> Stats = new List<OfflineStatPrinter>
    {
        SetPlayerData,
        CoreLine,
        MyCTFLine,
        delegate(Player p, PlayerData who)
        {
            MyCTFOnlineProfileStat.MiscLine(p, who.Name, who.Deaths, who.Money);
        },
        TimeSpentLine,
        LoginLine,
        delegate(Player p, PlayerData who)
        {
            MyCTFOnlineProfileStat.LoginsLine(p, who.Logins, who.Kicks);
        },
        delegate(Player p, PlayerData who)
        {
            MyCTFOnlineProfileStat.BanLine(p, who.Name);
        },
        delegate(Player p, PlayerData who)
        {
            MyCTFOnlineProfileStat.SpecialGroupLine(p, who.Name);
        },
        delegate(Player p, PlayerData who)
        {
            MyCTFOnlineProfileStat.IPLine(p, who.Name, who.IP);
        }
    };

    public static bool HasAllAwards(string name)
    {
        List<string> awards = PlayerAwards.Get(name);
        int totalAwards = AwardsList.Awards.Count;
        if (totalAwards != 0 && awards != null)
        {
            return awards.Count == totalAwards;
        }
        return false;
    }

    public static void SetPlayerData(Player p, PlayerData data)
    {
        hasAllAwards = HasAllAwards(data.Name);
        ctfData = MyCTFGame.GetOfflineStats(data);
    }

    public static void CoreLine(Player p, PlayerData data)
    {
        Group group = Group.GroupIn(data.Name);
        string text = ((data.Color.Length == 0) ? group.Color : data.Color);
        string text2 = ((data.Title.Length == 0) ? "" : (text + "[" + data.TitleColor + data.Title + text + "] "));
        string text3 = PlayerDB.LoadNick(data.Name);
        string text4 = text3 ?? Server.ToRawUsername(data.Name);
        string fullName = text2 + text + text4;
        CommonCoreLine(p, fullName, data.Name, group, data.Messages);
    }

    internal static void CommonCoreLine(Player p, string fullName, string name, Group grp, int messages)
    {
        p.Message("{0} {2}({1}) {2}has:", fullName, name, hasAllAwards ? "&6" : "&S");
        p.Message("  {2}Rank of {0}{2}, wrote &a{1} {2}messages", grp.ColoredName, messages, hasAllAwards ? "&6" : "&S");
        List<Pronouns> @for = Pronouns.GetFor(name);
        if (@for[0] != Pronouns.Default)
        {
            p.Message("  Pronouns: &a{0}", @for.Join((Pronouns pro) => pro.Name));
        }
    }

    public static void MyCTFLine(Player p, PlayerData who)
    {
        p.Message("  &a{0} &aXP{3}, &f{1} {3}Kills, &f{2} {3}Captures", ctfData.XP, ctfData.Kills, ctfData.Captures, hasAllAwards ? "&6" : "&S");
        p.Message("  {1}Highest Killstreak: &f{0}", ctfData.Killstreak, hasAllAwards ? "&6" : "&S");
    }

    public static void TimeSpentLine(Player p, PlayerData who)
    {
        p.Message("  {1}Spent &a{0} {1}on the server", who.TotalTime.Shorten(), hasAllAwards ? "&6" : "&S");
    }

    public static void LoginLine(Player p, PlayerData who)
    {
        p.Message("  {2}First login &a{0}{2}, last login &a{1}", who.FirstLogin.ToString("yyyy-MM-dd"), who.LastLogin.ToString("yyyy-MM-dd"), hasAllAwards ? "&6" : "&S");
    }
}

