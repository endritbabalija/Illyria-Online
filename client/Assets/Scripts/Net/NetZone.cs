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
        static readonly System.Reflection.FieldInfo SocketErrorField =
            typeof(Socket).GetField("ReceivedError", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        public static NetZone Instance { get; private set; }

        /// <summary>
        /// In a server match, or reconnecting to one: the server decides combat, items and loot. While
        /// reconnecting nothing is sent (actions are refused with a notice), but nothing falls back to
        /// offline rules either.
        /// </summary>
        public static bool Online => Instance != null && (Instance.InMatch || Instance.reconnecting && Instance.playing != null);

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
        [Tooltip("Seconds before the first reconnect attempt after a dropped connection; doubles per failed attempt.")]
        [SerializeField] float reconnectDelay = 1f;
        [SerializeField] float reconnectMaxDelay = 10f;
        [Tooltip("Seconds between position updates while moving.")]
        [SerializeField] float sendInterval = 0.1f;
        [Tooltip("Seconds between re-sends of our target lock while the server's lock differs (refused at the range edge, or dropped).")]
        [SerializeField] float targetSyncInterval = 0.5f;

        [Header("Scene")]
        [SerializeField] PlayerCombat player;
        [SerializeField] PlayerSkills skills;
        [SerializeField] DrangueLeap leap;
        [SerializeField] GroundLoot loot;
        [SerializeField] SkillFx skillFx;
        [Tooltip("Other players' form effects and bow shots.")]
        [SerializeField] CombatEffects combatEffects;
        [Tooltip("The player's clips and weapon models, copied for other players.")]
        [SerializeField] PlayerAnimator playerAnimator;
        [SerializeField] PlayerAppearance playerAppearance;
        [SerializeField] Camera viewCamera;
        [Tooltip("Shown after logging in, and from the Skills panel: pick or create the character to play.")]
        [SerializeField] CharacterSelect characterSelect;
        [Tooltip("Shown while the connection is lost and being restored.")]
        [SerializeField] GameObject connectionBanner;
        [SerializeField] Text connectionText;
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
        bool reconnecting;
        bool rejoined;
        bool quitting;
        /// <summary>The character we play (or are rejoining as); null until one is chosen.</summary>
        string playing;
        int nextRid;
        float nextSend;
        /// <summary>The monster the server has us locked on, from its target notices; null for none.</summary>
        string serverTarget;
        float nextTargetSync;
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
            HideBanner();
            if (connectOnStart) _ = StartOnline();
        }

        void OnDestroy()
        {
            quitting = true;
            if (Instance == this) Instance = null;
            if (socket != null) _ = socket.CloseAsync();
        }

        // ================================================================ connecting

        async Task StartOnline()
        {
            NetCharacterList list;
            try
            {
                // Only reaching the server and logging in are timed; once in, the player takes their
                // time choosing, and a join may wait out a lock (the character still saving in the zone
                // it just left), within joinRetries.
                var timeout = Task.Delay(TimeSpan.FromSeconds(connectTimeout));
                var connect = Connect();
                if (await Task.WhenAny(connect, timeout) == timeout) throw new TimeoutException($"no answer from {scheme}://{host}:{port} in {connectTimeout:0}s");
                await connect;
                list = await ListCharacters();
            }
            catch (Exception e)
            {
                GoOffline(e.Message);
                return;
            }

            if (characterSelect != null) characterSelect.Show(list.characters, player.Character.id, false, Choose);
            else
            {
                // No select screen in this scene: play the Player's character, named after it.
                try { await Choose(player.Character.id, HasCharacter(list, player.Character.id) ? null : player.Character.name); }
                catch (Exception e) { Debug.LogError($"[Net] Joining failed: {e.Message}"); }
            }
        }

        async Task Connect()
        {
            if (client == null) client = new Client(scheme, host, port, serverKey, UnityWebRequestAdapter.Instance) { Timeout = 5 };
            if (session == null || session.HasExpired(DateTime.UtcNow.AddMinutes(1)))
                session = await client.AuthenticateDeviceAsync(DeviceId());
            if (socket != null)
            {
                // A dropped socket is replaced, not reused; the old one's events must not reach us.
                socket.ReceivedMatchState -= OnMatchState;
                socket.Closed -= OnClosed;
            }
            socket = client.NewSocket(useMainThread: true);
            // Nakama's socket starts with a handler that logs every receive error with Debug.LogError;
            // a dropped connection is expected here (we reconnect), so it becomes a warning.
            if (socket is Socket) SocketErrorField?.SetValue(socket, null);
            socket.ReceivedError += e => Debug.LogWarning($"[Net] Socket error: {e.Message}");
            socket.ReceivedMatchState += OnMatchState;
            socket.Closed += OnClosed;
            await socket.ConnectAsync(session, true, 5);
            Debug.Log($"[Net] Logged in at {scheme}://{host}:{port} as user {session.UserId}");
        }

        async Task<NetCharacterList> ListCharacters()
        {
            var list = JsonUtility.FromJson<NetCharacterList>((await client.RpcAsync(session, "character_list", "{}")).Payload);
            if (list.characters == null) list.characters = new NetCharacterSummary[0];
            return list;
        }

        static bool HasCharacter(NetCharacterList list, string characterId) =>
            Array.FindIndex(list.characters, c => c.characterId == characterId) >= 0;

        /// <summary>
        /// The character select's choice: creates the character first when <paramref name="newName"/> is
        /// given, then plays it (leaving the current channel for another character). Throws with the
        /// server's wording when the name is refused or the join fails; the Welcome closes the screen.
        /// </summary>
        public async Task Choose(string characterId, string newName)
        {
            if (joining) throw new InvalidOperationException("Already joining; wait a moment");
            if (socket == null || !socket.IsConnected) throw new InvalidOperationException(reconnecting ? "Reconnecting to the server…" : "Not connected to the server");
            if (!string.IsNullOrEmpty(newName))
            {
                await client.RpcAsync(session, "character_create", Json.Object("characterId", characterId, "name", newName));
                Debug.Log($"[Net] Created {newName} ({characterId})");
                // So a join that fails now offers Play, not Create, on the next try.
                if (characterSelect != null && characterSelect.IsOpen) characterSelect.Refresh((await ListCharacters()).characters);
            }
            if (InMatch && characterId == player.Character.id)
            {
                if (characterSelect != null) characterSelect.Hide();
                return;
            }
            if (InMatch)
            {
                var leaving = matchId;
                LeaveLocally();
                await socket.LeaveMatchAsync(leaving);
            }
            playing = characterId;
            await JoinAs(characterId);
        }

        /// <summary>Joins a channel of the zone as this character, retrying while it's still held elsewhere.</summary>
        async Task JoinAs(string characterId)
        {
            joining = true;
            try
            {
                // A character still held by the zone we just left (it's saving) is refused for a moment.
                for (var attempt = 0; ; attempt++)
                {
                    if (offline || quitting) return;
                    var join = JsonUtility.FromJson<NetZoneJoin>((await client.RpcAsync(session, "zone_join", Json.Object("zone", zoneId))).Payload);
                    try
                    {
                        await socket.JoinMatchAsync(join.matchId, new Dictionary<string, string> { { "characterId", characterId } });
                        matchId = join.matchId;
                        Channel = join.channel.ToString();
                        return;
                    }
                    catch (Exception e) when (attempt < joinRetries && socket.IsConnected)
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

        /// <summary>Online "play the other character" (scripts): leaves the channel and rejoins as that character, which must exist.</summary>
        public async void SwitchCharacter(string characterId)
        {
            try { await Choose(characterId, null); }
            catch (Exception e)
            {
                Debug.LogError($"[Net] Switching to {characterId} failed: {e.Message}");
                player.Tell("Couldn't switch character; see the Console");
            }
        }

        /// <summary>Opens the character select over the zone (the Skills panel's button); closing it keeps playing.</summary>
        public async void OpenCharacterSelect()
        {
            if (characterSelect == null || !InMatch) return;
            try { characterSelect.Show((await ListCharacters()).characters, player.Character.id, true, Choose); }
            catch (Exception e) { player.Tell("Couldn't list your characters: " + e.Message); }
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
            if (quitting || offline || reconnecting) return;
            var wasIn = InMatch;
            LeaveLocally();
            Debug.LogWarning($"[Net] Disconnected{(wasIn ? $" from {ZoneSession.Instance.ZoneName} channel {Channel}" : "")}{(string.IsNullOrEmpty(reason) ? "" : ": " + reason)}");
            _ = Reconnect();
        }

        /// <summary>
        /// After a dropped connection: logs in again (a new session if it ran out), opens a new socket
        /// and rejoins the zone as the same character, backing off between attempts until it works.
        /// The server saved the character when the old socket left, so the rejoin picks up from there.
        /// Meanwhile <see cref="Online"/> stays true: nothing falls back to offline rules, and actions
        /// are refused with a notice.
        /// </summary>
        async Task Reconnect()
        {
            reconnecting = true;
            ShowBanner("Connection lost · reconnecting…");
            try
            {
                for (var attempt = 1; !quitting && !offline; attempt++)
                {
                    try
                    {
                        await Connect();
                        if (playing != null)
                        {
                            rejoined = true;
                            await JoinAs(playing);
                        }
                        else
                        {
                            if (characterSelect != null && characterSelect.IsOpen) characterSelect.Refresh((await ListCharacters()).characters);
                            HideBanner();
                        }
                        Debug.Log($"[Net] Reconnected after {attempt} attempt(s)");
                        return;
                    }
                    catch (Exception e)
                    {
                        var wait = Mathf.Min(reconnectMaxDelay, reconnectDelay * Mathf.Pow(2f, attempt - 1));
                        Debug.Log($"[Net] Reconnect attempt {attempt} failed ({e.Message}); next in {wait:0}s");
                        ShowBanner($"Connection lost · reconnecting (attempt {attempt + 1})…");
                        await Task.Delay(TimeSpan.FromSeconds(wait));
                    }
                }
            }
            finally
            {
                reconnecting = false;
            }
        }

        void ShowBanner(string text)
        {
            if (connectionBanner == null) return;
            connectionBanner.SetActive(true);
            if (connectionText != null) connectionText.text = text;
        }

        void HideBanner()
        {
            if (connectionBanner != null) connectionBanner.SetActive(false);
        }

        /// <summary>Forgets the channel's monsters, players and drops (leaving, or before a rejoin's Welcome).</summary>
        void LeaveLocally()
        {
            InMatch = false;
            matchId = null;
            foreach (var m in monsters.Values) if (m != null) Destroy(m.gameObject);
            monsters.Clear();
            foreach (var p in players.Values) if (p != null) Destroy(p.gameObject);
            players.Clear();
            pending.Clear();
            serverTarget = null;
            if (loot != null) loot.ClearNet();
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
            if (!InMatch || player.IsDead) return;
            SyncTarget();
            if (leap.IsMoving || Time.unscaledTime < nextSend) return;
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
            nextTargetSync = Time.unscaledTime + targetSyncInterval;
            Send(NetOp.Target, Json.Object("id", NetIdOf(target)));
        }

        /// <summary>
        /// Our lock and the server's can disagree at the edge of loseTargetRange, where the two measure
        /// from slightly different positions: a lock the server refused, or one it dropped that we kept,
        /// would leave us standing by the monster without attacking. Re-send ours until they match.
        /// </summary>
        void SyncTarget()
        {
            if (Time.unscaledTime < nextTargetSync) return;
            var target = player.Target;
            if (NetIdOf(target) != serverTarget) SendTarget(target);
        }

        static string NetIdOf(Combatant target)
        {
            var monster = target != null ? target.GetComponent<MonsterAI>() : null;
            return monster != null && !string.IsNullOrEmpty(monster.NetId) ? monster.NetId : null;
        }

        public void SendCast(string formId, string targetId, Vector3 facing) =>
            Send(NetOp.Cast, Json.Object("form", formId, "target", targetId, "dir", new[] { facing.x, facing.z }));

        public void SendLeap(string ledgeName, Vector3 aim) =>
            Send(NetOp.Leap, Json.Object("ledge", ledgeName, "dir", new[] { aim.x, aim.z }));

        /// <summary>An item or book action (a Json.Object with "a"); a refusal shows the server's wording as a notice.</summary>
        public void SendAction(string json, Action<NetResult> answered = null)
        {
            if (!InMatch)
            {
                player.Tell("Not connected; reconnecting to the server…");
                return;
            }
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
                        OnWelcome(JsonUtility.FromJson<NetWelcome>(json), state.MatchId);
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

        void OnWelcome(NetWelcome w, string joined)
        {
            LeaveLocally();
            // Can arrive before JoinMatchAsync's answer is handled.
            matchId = joined;
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
            playing = w.self.characterId;
            HideBanner();
            if (characterSelect != null) characterSelect.Hide();
            Debug.Log($"[Net] {(rejoined ? "Rejoined" : "Joined")} {ZoneSession.Instance.ZoneName} channel {Channel} as {w.self.name} ({w.self.characterId}): {players.Count} other player(s), {monsters.Count} monsters");
            player.Tell($"{(rejoined ? "Reconnected" : "Online")} · {ZoneSession.Instance.ZoneName} channel {Channel}");
            rejoined = false;
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
                    else if (players.TryGetValue(p.id, out var remote))
                    {
                        remote.SetWeapon(p.weapon);
                        remote.SetState(Ground(new Vector3(p.x, p.y, p.z)), p.yaw, p.dead);
                    }
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
                    case "cast": OnCast(e); break;
                    case "leap":
                        if (e.id != you && players.TryGetValue(e.id ?? "", out var leaper) && leaper != null && e.from != null && e.to != null && e.from.Length == 3 && e.to.Length == 3)
                            leaper.Leap(new Vector3(e.from[0], e.from[1], e.from[2]), new Vector3(e.to[0], e.to[1], e.to[2]), e.dur, !string.IsNullOrEmpty(e.ledge));
                        break;
                    case "gear":
                        if (players.TryGetValue(e.id ?? "", out var geared) && geared != null) geared.SetWeapon(e.weapon);
                        break;
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
            else if (e.kind == "basic" && target != null && players.TryGetValue(e.src ?? "", out var striker) && striker != null)
            {
                striker.PlayAttack(target.transform.position);
                if (combatEffects != null && striker.Weapon != null)
                    combatEffects.PlayBasic(striker.Weapon.id, striker.Hands, striker.transform.position, target);
            }
            if (!e.miss && players.TryGetValue(e.dst ?? "", out var struck) && struck != null) struck.Flinch();
            if (target == null) return;
            target.ReceiveHit(attacker, new AttackResult
            {
                Hit = !e.miss,
                Crit = e.crit,
                Damage = e.miss ? 0 : e.dmg,
                Kind = e.kind == "skill" ? HitKind.Skill : e.kind == "poison" ? HitKind.Poison : HitKind.Basic,
            });
        }

        /// <summary>Another player's form went off: their cast animation and its area on the ground (ours played on the press).</summary>
        void OnCast(NetEvent e)
        {
            if (e.src == you || !players.TryGetValue(e.src ?? "", out var caster) || caster == null) return;
            var form = GameData.Skills.FindForm(e.form);
            if (form == null) return;
            var aimed = CombatantOf(e.tgt);
            caster.PlayForm(form, aimed != null ? aimed.transform.position : (Vector3?)null);
            var origin = caster.transform.position;
            var facing = e.dir != null && e.dir.Length == 2 ? new Vector3(e.dir[0], 0f, e.dir[1]) : caster.transform.forward;
            if (combatEffects != null && caster.Weapon != null)
                combatEffects.PlayForm(form, caster.Weapon.id, origin, facing, aimed, caster.Hands);
            if (skillFx == null) return;
            skillFx.Play(form, origin, facing.normalized, aimed != null ? aimed.transform.position : origin);
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
            // A refused lock is re-sent while we keep it (SyncTarget), so its wording would only flicker.
            if (!r.ok && r.a != "target" && !string.IsNullOrEmpty(r.text)) player.Tell(r.text);
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
            if (n.code == "target")
            {
                // The client drops dead or far targets on its own; this only tells SyncTarget what the server has.
                serverTarget = string.IsNullOrEmpty(n.id) ? null : n.id;
                return;
            }
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
            remote.Init(p.id, p.name, p.character, Ground(new Vector3(p.x, p.y, p.z)), p.yaw, playerAnimator, playerAppearance);
            remote.SetWeapon(p.weapon);
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
