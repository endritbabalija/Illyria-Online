using System;
using System.Globalization;
using System.Text;
using Wuxia.Data;

namespace Wuxia.Net
{
    // Shapes of the zone match messages (server/PROTOCOL.md, server/src/zone.ts), read with
    // JsonUtility. Field names match the server's JSON keys. JsonUtility turns a JSON null object
    // into an empty instance and a null string into "", so "nothing" checks look at ids.

    /// <summary>Match op codes (server/src/protocol.ts).</summary>
    public static class NetOp
    {
        public const long Move = 1, Target = 2, Cast = 3, Leap = 4, Action = 5;
        public const long Welcome = 100, Snapshot = 101, Events = 102, Self = 103, Drops = 104, Result = 105, Correction = 106, Notice = 107;
    }

    [Serializable]
    public class NetStatus
    {
        public string id;
        public float left;
    }

    [Serializable]
    public class NetPlayer
    {
        public string id;
        public string name;
        public string character;
        public string weapon;
        public float x, y, z, yaw;
        public int hp, maxHp, sp, maxSp;
        public bool dead;
        public NetStatus[] st;
    }

    [Serializable]
    public class NetMonster
    {
        public string id;
        public string monster;
        public string pack;
        public float x, y, z, yaw;
        public int hp, maxHp;
        public string state;
        public string target;
        public NetStatus[] st;
    }

    [Serializable]
    public class NetBonusLine
    {
        public string stat;
        public float value;
    }

    [Serializable]
    public class NetItem
    {
        public string id;
        public string def;
        public string rarity;
        public int count;
        public NetBonusLine[] lines;

        public bool IsEmpty => string.IsNullOrEmpty(def);
    }

    [Serializable]
    public class NetEquipment
    {
        public NetItem weapon, helm, armor, boots, ring, amulet;

        public NetItem Get(EquipSlot slot)
        {
            switch (slot)
            {
                case EquipSlot.Weapon: return weapon;
                case EquipSlot.Helm: return helm;
                case EquipSlot.Armor: return armor;
                case EquipSlot.Boots: return boots;
                case EquipSlot.Ring: return ring;
                case EquipSlot.Amulet: return amulet;
                default: return null;
            }
        }
    }

    [Serializable]
    public class NetBookLevel
    {
        public string id;
        public int level;
    }

    [Serializable]
    public class NetSkills
    {
        public NetBookLevel[] books;
        public int points;
        public int meterKills;
    }

    [Serializable]
    public class NetCooldown
    {
        public string id;
        public float left;
    }

    /// <summary>Your own character: Self, and the self part of Welcome.</summary>
    [Serializable]
    public class NetSelf
    {
        public string characterId;
        public string name;
        public int gold;
        public NetItem[] bag;
        public NetEquipment equipment;
        public string[] quickSlots;
        public NetSkills skills;
        public int kills;
        public NetCooldown[] cooldowns;
        public NetCooldown[] potionCooldowns;
    }

    [Serializable]
    public class NetWelcome
    {
        public string you;
        public string zone;
        public float t;
        public NetSelf self;
        public NetPlayer[] players;
        public NetMonster[] monsters;
    }

    [Serializable]
    public class NetSnapshot
    {
        public float t;
        public NetPlayer[] p;
        public NetMonster[] m;
    }

    /// <summary>One event; which fields are set depends on <see cref="k"/> (PROTOCOL.md, Events).</summary>
    [Serializable]
    public class NetEvent
    {
        public string k;
        public string id, src, dst, by, kind, form, tgt, s, ledge, weapon, weaponType;
        public int dmg, n;
        public bool crit, miss;
        public float dur;
        public float[] dir, at, from, to;
        public NetPlayer player;
        public NetMonster monster;
    }

    [Serializable]
    public class NetEvents
    {
        public float t;
        public NetEvent[] e;
    }

    [Serializable]
    public class NetDrop
    {
        public string id;
        public float x, y, z;
        public int gold;
        public NetItem item;
        public float left;
    }

    [Serializable]
    public class NetDropGone
    {
        public string id;
        public string why;
    }

    [Serializable]
    public class NetDrops
    {
        public NetDrop[] add;
        public NetDropGone[] remove;
    }

    [Serializable]
    public class NetResult
    {
        public string a;
        public int rid;
        public bool ok;
        public string error;
        public string text;
        public string id;
        public bool queued;
    }

    [Serializable]
    public class NetCorrection
    {
        public float x, y, z;
        public string why;
    }

    [Serializable]
    public class NetNotice
    {
        public string code;
        public string text;
        public string id;
    }

    // RPC answers.

    [Serializable]
    public class NetCharacterSummary
    {
        public string characterId;
        public string name;
    }

    [Serializable]
    public class NetCharacterList
    {
        public NetCharacterSummary[] characters;
    }

    [Serializable]
    public class NetZoneJoin
    {
        public string matchId;
        public string zone;
        public int channel;
    }

    /// <summary>Builds small JSON objects for outgoing messages: Json.Object("a", "equip", "item", id).</summary>
    public static class Json
    {
        /// <param name="pairs">Key, value, key, value… Values: string, bool, int, float, float[] or null.</param>
        public static string Object(params object[] pairs)
        {
            var sb = new StringBuilder("{");
            for (var i = 0; i + 1 < pairs.Length; i += 2)
            {
                if (i > 0) sb.Append(',');
                AppendString(sb, (string)pairs[i]);
                sb.Append(':');
                AppendValue(sb, pairs[i + 1]);
            }
            return sb.Append('}').ToString();
        }

        static void AppendValue(StringBuilder sb, object value)
        {
            switch (value)
            {
                case null: sb.Append("null"); break;
                case string s: AppendString(sb, s); break;
                case bool b: sb.Append(b ? "true" : "false"); break;
                case int n: sb.Append(n.ToString(CultureInfo.InvariantCulture)); break;
                case float f: sb.Append(f.ToString("0.###", CultureInfo.InvariantCulture)); break;
                case float[] list:
                    sb.Append('[');
                    for (var i = 0; i < list.Length; i++)
                    {
                        if (i > 0) sb.Append(',');
                        sb.Append(list[i].ToString("0.###", CultureInfo.InvariantCulture));
                    }
                    sb.Append(']');
                    break;
                default: throw new ArgumentException($"Json.Object can't write a {value.GetType().Name}");
            }
        }

        static void AppendString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (var c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }
    }
}
