using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Wuxia.Combat;
using Wuxia.Data;
using Wuxia.Items;
using Wuxia.Monsters;
using Wuxia.Player;
using Wuxia.UI;
using Wuxia.World;

namespace Wuxia.Net
{
    /// <summary>
    /// Plays the zone online (M4): logs in with the device id, makes sure the character exists on
    /// the server, joins a channel of this zone and from then on shows what the server decides.
    /// Monsters are the scene's monster prefabs in remote mode (<see cref="MonsterAI.SpawnRemote"/>),
    /// other players are <see cref="RemotePlayer"/>s, and our own hits, HP, items, skills, loot,
    /// deaths and revives come from the server. The player still moves, aims and animates locally;
    /// moves go up 10 times a second and a refused one comes back as a correction.
    /// If the server can't be reached the zone falls back to offline play (local monsters and rules).
    /// Protocol: server/PROTOCOL.md.
    /// </summary>
    [DefaultExecutionOrder(-190)] // after ZoneSession loads the game data, before the spawners' Start
    public class NetZone : MonoBehaviour
    {
        const string DeviceIdKey = "nakama.device_id";

        public static NetZone Instance { get; private set; }

        /// <summary>In a server match: the server decides combat, items and loot.</summary>
        public static bool Online => Instance != null && Instance.InMatch;

        /// <summary>Connecting (or connected): local monster spawners wait instead of spawning.</summary>
        public static bool WillConnect => Instance != null && Instance.isActiveAndEnabled && Instance.connectOnStart && !Instance.offline;

        [Header("Server")]
        [Tooltip("Off: play offline with local rules, as in M1–M3.")]
        [SerializeField] bool connectOnStart = true;
        [SerializeField] string scheme = "http";
        [Tooltip("127.0.0.1 works in the Editor. On a phone, use the dev PC's LAN IP.")]
        [SerializeField] string host = "127.0.0.1";
        [SerializeField] int port = 7350;
        [SerializeField] string serverKey = "defaultkey";
        [Tooltip("The zone in shared/gamedata/zones.json this scene is.")]
        [SerializeField] string zoneId = "oak_valley";
        [Tooltip("Appended to this device's id, to log in as a second account from the same machine. Also: command line -wuxiaDevice <suffix>.")]
        [SerializeField] string deviceIdSuffix = "";
        [Tooltip("Seconds to wait for login and the first zone state before playing offline.")]
        [SerializeField] float connectTimeout = 10f;
        [Tooltip("Seconds between joins while the character is still held by another zone.")]
        [SerializeField] float joinRetryDelay = 2f;
        [SerializeField] int joinRetries = 8;
        [Tooltip("Seconds between position updates while moving.")]
        [SerializeField] float sendInterval = 0.1f;

        [Header("Scene")]
        [SerializeField] PlayerCombat player;
        [SerializeField] PlayerSkills skills;
        [SerializeField] DrangueLeap leap;
        [SerializeField] GroundLoot loot;
        [SerializeField] Camera viewCamera;
        [Tooltip("Character models for other players, by character id.")]
        [SerializeField] GameObject kreshnikModel;
        [SerializeField] GameObject zanasChosenModel;
        [Tooltip("A name label (Text under the HUD's label layer) copied for each other player.")]
        [SerializeField] Text labelTemplate;
        [SerializeField] Vector3 labelOffset = new Vector3(0f, 2.3f, 0f);

        IClient client;
        ISession session;
        ISocket socket;
        string matchId;
        string you;
        bool offline;
        bool joining;
        int nextRid;
        float nextSend;
        Vector3 lastSentPosition;
        float lastSentYaw;
        Transform remoteRoot;

        readonly Dictionary<string, MonsterAI> monsterPrefabs = new Dictionary<string, MonsterAI>();
        readonly Dictionary<string, Transform> packParents = new Dictionary<string, Transform>();
        readonly Dictionary<string, MonsterAI> monsters = new Dictionary<string, MonsterAI>();
        readonly Dictionary<string, RemotePlayer> players = new Dictionary<string, RemotePlayer>();
        readonly Dictionary<int, Action<NetResult>> pending = new Dictionary<int, Action<NetResult>>();

        public bool InMatch { get; private set; }
        public string Channel { get; private set; }

        void Awake()
        {
            Instance = this;
            if (!connectOnStart) return;
            foreach (var spawner in FindObjectsByType<MonsterSpawner>(FindObjectsSortMode.None))
            {
                packParents[spawner.name] = spawner.transform;
                if (spawner.Prefab != null) monsterPrefabs[spawner.Prefab.MonsterId] = spawner.Prefab;
            }
            remoteRoot = new GameObject("RemotePlayers").transform;
            remoteRoot.SetParent(transform, false);
        }

        void Start()
        {
            if (connectOnStart) _ = StartOnline();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (socket != null) _ = socket.CloseAsync();
        }

        // ================================================================ connecting

        async Task StartOnline()
        {
            try
            {
                // Only reaching the server and logging in are timed; once in, a join may wait out
                // a lock (the character still saving in the zone it just left), within joinRetries.
                var timeout = Task.Delay(TimeSpan.FromSeconds(connectTimeout));
                var connect = Connect();
                if (await Task.WhenAny(connect, timeout) == timeout) throw new TimeoutException($"no answer from {scheme}://{host}:{port} in {connectTimeout:0}s");
                await connect;
                await JoinAs(player.Character.id);
            }
            catch (Exception e)
            {
                GoOffline(e.Message);
            }
        }

        async Task Connect()
        {
            client = new Client(scheme, host, port, serverKey, UnityWebRequestAdapter.Instance) { Timeout = 5 };
            session = await client.AuthenticateDeviceAsync(DeviceId());
            socket = client.NewSocket(useMainThread: true);
            socket.ReceivedMatchState += OnMatchState;
            socket.Closed += OnClosed;
            await socket.ConnectAsync(session, true);
            Debug.Log($"[Net] Logged in at {scheme}://{host}:{port} as user {session.UserId}");
        }

        /// <summary>Makes sure the account has this character, then joins a channel of the zone as it.</summary>
        async Task JoinAs(string characterId)
        {
            joining = true;
            try
            {
                var list = JsonUtility.FromJson<NetCharacterList>((await client.RpcAsync(session, "character_list", "{}")).Payload);
                if (list.characters == null || Array.FindIndex(list.characters, c => c.characterId == characterId) < 0)
                {
                    var name = PlayerPrefs.GetString("wuxia.name." + characterId, GameData.Combat.GetCharacter(characterId).name);
                    await client.RpcAsync(session, "character_create", Json.Object("characterId", characterId, "name", name));
                    Debug.Log($"[Net] Created {name} ({characterId})");
                }

                // A character still held by the zone we just left (it's saving) is refused for a moment.
                for (var attempt = 0; ; attempt++)
                {
                    if (offline) return;
                    var join = JsonUtility.FromJson<NetZoneJoin>((await client.RpcAsync(session, "zone_join", Json.Object("zone", zoneId))).Payload);
                    try
                    {
                        await socket.JoinMatchAsync(join.matchId, new Dictionary<string, string> { { "characterId", characterId } });
                        matchId = join.matchId;
                        Channel = join.channel.ToString();
                        return;
                    }
                    catch (Exception e) when (attempt < joinRetries)
                    {
                        Debug.Log($"[Net] Join refused ({e.Message}); retrying in {joinRetryDelay:0}s");
                        await Task.Delay(TimeSpan.FromSeconds(joinRetryDelay));
                    }
                }
            }
            finally
            {
                joining = false;
            }
        }

        /// <summary>Online "play the other character": leaves the channel and rejoins as that character.</summary>
        public async void SwitchCharacter(string characterId)
        {
            if (!InMatch || joining) return;
            var leaving = matchId;
            LeaveLocally();
            try
            {
                await socket.LeaveMatchAsync(leaving);
                await JoinAs(characterId);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Net] Switching to {characterId} failed: {e.Message}");
                player.Tell("Couldn't switch character; see the Console");
            }
        }

        void GoOffline(string reason)
        {
            if (InMatch || offline) return;
            offline = true;
            Debug.LogWarning($"[Net] Playing offline: {reason}");
            player.Tell("Playing offline (no server)");
            foreach (var spawner in FindObjectsByType<MonsterSpawner>(FindObjectsSortMode.None)) spawner.SpawnLocal();
        }

        void OnClosed(string reason)
        {
            if (!InMatch) return;
            LeaveLocally();
            Debug.LogWarning($"[Net] Disconnected: {reason}");
            player.Tell("Disconnected from the server");
        }

        /// <summary>Forgets the channel's monsters, players and drops (leaving, or before a rejoin's Welcome).</summary>
        void LeaveLocally()
        {
            InMatch = false;
            foreach (var m in monsters.Values) if (m != null) Destroy(m.gameObject);
            monsters.Clear();
            foreach (var p in players.Values) if (p != null) Destroy(p.gameObject);
            players.Clear();
            pending.Clear();
        }

        string DeviceId()
        {
            var id = PlayerPrefs.GetString(DeviceIdKey, string.Empty);
            if (string.IsNullOrEmpty(id))
            {
                id = SystemInfo.deviceUniqueIdentifier;
                if (id == SystemInfo.unsupportedIdentifier || id.Length < 10) id = Guid.NewGuid().ToString();
                PlayerPrefs.SetString(DeviceIdKey, id);
                PlayerPrefs.Save();
            }
            var suffix = deviceIdSuffix;
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-wuxiaDevice") suffix = args[i + 1];
            return string.IsNullOrEmpty(suffix) ? id : id + "-" + suffix;
        }

        // ================================================================ sending

        void Update()
        {
            if (!InMatch || player.IsDead || leap.IsMoving || Time.unscaledTime < nextSend) return;
            var t = player.transform;
            var yaw = t.eulerAngles.y;
            if ((t.position - lastSentPosition).sqrMagnitude < 0.0025f && Mathf.Abs(Mathf.DeltaAngle(yaw, lastSentYaw)) < 3f) return;
            nextSend = Time.unscaledTime + sendInterval;
            lastSentPosition = t.position;
            lastSentYaw = yaw;
            Send(NetOp.Move, Json.Object("x", t.position.x, "y", t.position.y, "z", t.position.z, "yaw", yaw));
        }

        void Send(long op, string json)
        {
            if (socket == null || string.IsNullOrEmpty(matchId)) return;
            _ = socket.SendMatchStateAsync(matchId, op, json);
        }

        public void SendTarget(Combatant target)
        {
            var monster = target != null ? target.GetComponent<MonsterAI>() : null;
            Send(NetOp.Target, Json.Object("id", monster != null ? monster.NetId : null));
        }

        public void SendCast(string formId, string targetId, Vector3 facing) =>
            Send(NetOp.Cast, Json.Object("form", formId, "target", targetId, "dir", new[] { facing.x, facing.z }));

        public void SendLeap(string ledgeName, Vector3 aim) =>
            Send(NetOp.Leap, Json.Object("ledge", ledgeName, "dir", new[] { aim.x, aim.z }));

        /// <summary>An item or book action (a Json.Object with "a"); a refusal shows the server's wording as a notice.</summary>
        public void SendAction(string json, Action<NetResult> answered = null)
        {
            var rid = ++nextRid;
            if (answered != null) pending[rid] = answered;
            Send(NetOp.Action, "{\"rid\":" + rid + "," + json.Substring(1));
        }

        // ================================================================ receiving

        void OnMatchState(IMatchState state)
        {
            var json = Encoding.UTF8.GetString(state.State);
            try
            {
                switch (state.OpCode)
                {
                    case NetOp.Welcome:
                        if (offline)
                        {
                            // A join that finished after we gave up and went offline: don't mix the two.
                            _ = socket.LeaveMatchAsync(state.MatchId);
                            return;
                        }
                        // Can arrive before JoinMatchAsync's answer is handled.
                        matchId = state.MatchId;
                        OnWelcome(JsonUtility.FromJson<NetWelcome>(json));
                        break;
                    case NetOp.Snapshot: if (InMatch) OnSnapshot(JsonUtility.FromJson<NetSnapshot>(json)); break;
                    case NetOp.Events: if (InMatch) OnEvents(JsonUtility.FromJson<NetEvents>(json)); break;
                    case NetOp.Self: if (InMatch) ApplySelf(JsonUtility.FromJson<NetSelf>(json)); break;
                    case NetOp.Drops: if (InMatch) OnDrops(JsonUtility.FromJson<NetDrops>(json)); break;
                    case NetOp.Result: OnResult(JsonUtility.FromJson<NetResult>(json)); break;
                    case NetOp.Correction: OnCorrection(JsonUtility.FromJson<NetCorrection>(json)); break;
                    case NetOp.Notice: OnNotice(JsonUtility.FromJson<NetNotice>(json)); break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Net] Message {state.OpCode} failed: {e}\n{json}");
            }
        }

        void OnWelcome(NetWelcome w)
        {
            LeaveLocally();
            you = w.you;
            ApplySelf(w.self);
            foreach (var p in w.players)
            {
                if (p.id == you)
                {
                    player.Mover.Teleport(Ground(new Vector3(p.x, p.y, p.z)), Quaternion.Euler(0f, p.yaw, 0f));
                    player.Combatant.SetHp(p.hp, p.maxHp);
                    lastSentPosition = player.transform.position;
                }
                else AddPlayer(p);
            }
            foreach (var m in w.monsters) SpawnMonster(m);
            InMatch = true;
            Debug.Log($"[Net] Joined {ZoneSession.Instance.ZoneName} channel {Channel} as {w.self.name} ({w.self.characterId}): {players.Count} other player(s), {monsters.Count} monsters");
            player.Tell($"Online · {ZoneSession.Instance.ZoneName} channel {Channel}");
        }

        void ApplySelf(NetSelf s)
        {
            if (s.characterId != player.Character.id || player.Combatant.DisplayName != s.name)
                player.ApplyServerCharacter(s.characterId, s.name);

            var bag = new List<ItemInstance>();
            if (s.bag != null) foreach (var item in s.bag) bag.Add(ToItem(item));
            var worn = new List<ItemInstance>();
            for (var slot = 0; slot < Equipment.SlotCount; slot++) worn.Add(s.equipment != null ? ToItem(s.equipment.Get((EquipSlot)slot)) : null);
            var potionCooldowns = new List<KeyValuePair<string, float>>();
            if (s.potionCooldowns != null) foreach (var c in s.potionCooldowns) potionCooldowns.Add(new KeyValuePair<string, float>(c.id, c.left));
            player.Items.ApplyServerState(s.characterId, s.gold, bag, worn, s.quickSlots ?? new string[0], potionCooldowns, Time.time);

            if (s.skills != null) skills.ApplyServerSkills(s.skills, s.cooldowns);
            ZoneSession.Instance.SetKills(s.kills);
        }

        void OnSnapshot(NetSnapshot snap)
        {
            if (snap.p != null)
                foreach (var p in snap.p)
                {
                    if (p.id == you)
                    {
                        player.Combatant.SetHp(p.hp, p.maxHp);
                        if (skills.State != null) skills.State.SetSpirit(p.sp);
                        if (p.dead && !player.IsDead) player.Combatant.Kill(null);
                    }
                    else if (players.TryGetValue(p.id, out var remote)) remote.SetState(Ground(new Vector3(p.x, p.y, p.z)), p.yaw, p.dead);
                }
            if (snap.m != null)
                foreach (var m in snap.m)
                {
                    if (!monsters.TryGetValue(m.id, out var monster) || monster == null) { SpawnMonster(m); continue; }
                    if (m.state == "dead") continue;
                    monster.SetRemoteState(Ground(new Vector3(m.x, m.y, m.z)), m.yaw);
                    monster.Combatant.SetHp(m.hp, m.maxHp);
                }
        }

        void OnEvents(NetEvents batch)
        {
            if (batch.e == null) return;
            foreach (var e in batch.e)
            {
                switch (e.k)
                {
                    case "join": if (e.player != null && e.player.id != you) AddPlayer(e.player); break;
                    case "leave": RemovePlayer(e.id); break;
                    case "hit": OnHit(e); break;
                    case "status":
                        var statusTarget = CombatantOf(e.id);
                        if (statusTarget != null) statusTarget.ApplyStatus(GameData.Skills.GetStatus(e.s), e.dur, 0, null);
                        break;
                    case "die": OnDie(e); break;
                    case "revive":
                        if (e.id == you && e.at != null && e.at.Length == 3) player.ServerRevive(Ground(new Vector3(e.at[0], e.at[1], e.at[2])));
                        break;
                    case "despawn":
                        if (monsters.TryGetValue(e.id, out var gone) && gone != null) gone.DespawnRemote();
                        break;
                    case "spawn": if (e.monster != null) SpawnMonster(e.monster); break;
                }
            }
        }

        void OnHit(NetEvent e)
        {
            var target = CombatantOf(e.dst);
            Combatant attacker = null;
            if (e.src == you) attacker = player.Combatant;
            else if (!string.IsNullOrEmpty(e.src) && monsters.TryGetValue(e.src, out var biter) && biter != null)
            {
                attacker = biter.Combatant;
                if (e.kind == "basic")
                {
                    var victim = e.dst == you ? player.transform : players.TryGetValue(e.dst ?? "", out var rp) && rp != null ? rp.transform : null;
                    if (victim != null) biter.PlayRemoteAttack(victim.position);
                }
            }
            if (e.src == you && e.kind == "basic") player.PlayServerAttack(target);
            if (target == null) return;
            target.ReceiveHit(attacker, new AttackResult
            {
                Hit = !e.miss,
                Crit = e.crit,
                Damage = e.miss ? 0 : e.dmg,
                Kind = e.kind == "skill" ? HitKind.Skill : e.kind == "poison" ? HitKind.Poison : HitKind.Basic,
            });
        }

        void OnDie(NetEvent e)
        {
            if (e.id == you)
            {
                player.Combatant.Kill(null);
                return;
            }
            if (monsters.TryGetValue(e.id, out var monster) && monster != null)
            {
                monster.Combatant.Kill(e.by == you ? player.Combatant : null);
                if (e.by == you) Debug.Log($"[Combat] {monster.Combatant.DisplayName} slain in {ZoneSession.Instance.ZoneName} (server)");
            }
        }

        void OnDrops(NetDrops drops)
        {
            if (drops.add != null)
                foreach (var d in drops.add)
                {
                    var rest = new Vector3(d.x, d.y, d.z);
                    LootDrop pile;
                    if (d.item != null && !d.item.IsEmpty)
                    {
                        var item = ToItem(d.item);
                        if (item == null) continue;
                        pile = LootDrop.OfItem(item);
                    }
                    else if (d.gold > 0) pile = LootDrop.OfGold(d.gold);
                    else continue;
                    loot.SpawnNet(d.id, pile, rest + Vector3.up * 0.6f, rest, d.left);
                }
            if (drops.remove != null)
                foreach (var r in drops.remove) loot.RemoveNet(r.id, r.why == "picked");
        }

        void OnResult(NetResult r)
        {
            if (pending.TryGetValue(r.rid, out var answered))
            {
                pending.Remove(r.rid);
                answered(r);
            }
            if (!r.ok && !string.IsNullOrEmpty(r.text)) player.Tell(r.text);
        }

        void OnCorrection(NetCorrection c)
        {
            if (c.why == "revive") return; // the revive event moves us
            Debug.Log($"[Net] Move corrected ({c.why})");
            player.Mover.Teleport(new Vector3(c.x, c.y, c.z), player.transform.rotation);
            lastSentPosition = player.transform.position;
        }

        void OnNotice(NetNotice n)
        {
            if (n.code == "target") return; // the client drops dead or far targets on its own
            if (!string.IsNullOrEmpty(n.text)) player.Tell(n.text);
        }

        // ================================================================ world objects

        void SpawnMonster(NetMonster m)
        {
            if (m.state == "dead") return;
            var position = Ground(new Vector3(m.x, m.y, m.z));
            if (!monsters.TryGetValue(m.id, out var monster) || monster == null)
            {
                if (!monsterPrefabs.TryGetValue(m.monster, out var prefab))
                {
                    Debug.LogWarning($"[Net] No prefab for monster '{m.monster}' (no spawner in the scene uses one)");
                    return;
                }
                var parent = packParents.TryGetValue(m.pack ?? "", out var pack) ? pack : transform;
                monster = Instantiate(prefab, position, Quaternion.Euler(0f, m.yaw, 0f), parent);
                monster.name = $"{prefab.name} {m.id}";
                monsters[m.id] = monster;
            }
            monster.SpawnRemote(m.id, position, m.yaw, m.hp, m.maxHp);
        }

        void AddPlayer(NetPlayer p)
        {
            if (players.ContainsKey(p.id)) return;
            var model = p.character == "zanas_chosen" ? zanasChosenModel : kreshnikModel;
            var avatar = Instantiate(model, remoteRoot);
            avatar.name = $"Player {p.name}";
            foreach (var c in avatar.GetComponentsInChildren<Collider>()) c.enabled = false;
            var remote = avatar.AddComponent<RemotePlayer>();
            remote.Init(p.id, p.name, p.character, Ground(new Vector3(p.x, p.y, p.z)), p.yaw);
            remote.SetState(Ground(new Vector3(p.x, p.y, p.z)), p.yaw, p.dead);
            if (labelTemplate != null && viewCamera != null)
            {
                var label = Instantiate(labelTemplate, labelTemplate.transform.parent);
                label.name = $"{p.name}Name";
                label.text = p.name;
                label.gameObject.SetActive(true);
                avatar.AddComponent<WorldLabel>().Configure(avatar.transform, viewCamera, label.rectTransform, labelOffset);
                remote.Label = label.gameObject;
            }
            players[p.id] = remote;
            Debug.Log($"[Net] {p.name} ({p.character}) is here");
        }

        void RemovePlayer(string id)
        {
            if (!players.TryGetValue(id ?? "", out var remote)) return;
            players.Remove(id);
            if (remote != null)
            {
                Debug.Log($"[Net] {remote.DisplayName} left");
                Destroy(remote.gameObject);
            }
        }

        Combatant CombatantOf(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (id == you) return player.Combatant;
            return monsters.TryGetValue(id, out var m) && m != null ? m.Combatant : null;
        }

        static ItemInstance ToItem(NetItem item)
        {
            if (item == null || item.IsEmpty) return null;
            var def = GameData.Items.FindItem(item.def);
            if (def == null)
            {
                Debug.LogWarning($"[Net] Unknown item '{item.def}' from the server; is items.json the same version?");
                return null;
            }
            RarityData rarity = null;
            foreach (var r in GameData.Items.rarities) if (r.id == item.rarity) rarity = r;
            var lines = new List<BonusLine>();
            if (item.lines != null) foreach (var l in item.lines) lines.Add(new BonusLine(l.stat, l.value));
            return new ItemInstance(item.id, def, rarity ?? GameData.Items.GetRarity(ItemsData.CommonRarity), Mathf.Max(1, item.count), lines);
        }

        /// <summary>The server's ground heights come from the NavMesh export; snap onto the NavMesh itself here.</summary>
        static Vector3 Ground(Vector3 p) => NavMesh.SamplePosition(p, out var hit, 2f, NavMesh.AllAreas) ? hit.position : p;
    }
}
