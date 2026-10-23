using System;
using UnityEngine;
using Wuxia.Combat;
using Wuxia.Data;

namespace Wuxia.World
{
    /// <summary>
    /// Per-zone root: loads the shared game data, knows the player and the Han respawn point, counts
    /// kills and tracks which <see cref="ZoneArea"/> the player is in. Runs first so everything else can
    /// read <see cref="GameData"/> from Start on.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class ZoneSession : MonoBehaviour
    {
        public static ZoneSession Instance { get; private set; }

        [Tooltip("combat.json from the com.wuxia.gamedata package (shared with the server).")]
        [SerializeField] TextAsset combatData;
        [Tooltip("skills.json from the com.wuxia.gamedata package (shared with the server).")]
        [SerializeField] TextAsset skillsData;
        [Tooltip("items.json from the com.wuxia.gamedata package (shared with the server).")]
        [SerializeField] TextAsset itemsData;
        [SerializeField] string zoneName = "Oak Valley";
        [Tooltip("Where the player revives after dying: in front of the zone's Han.")]
        [SerializeField] Transform respawnPoint;
        [SerializeField] Combatant player;
        [SerializeField] int targetFrameRate = 60;
        [Tooltip("Seconds between checks of which area the player stands in.")]
        [SerializeField] float areaCheckInterval = 0.25f;

        float nextAreaCheck;

        public string ZoneName => zoneName;
        public Transform RespawnPoint => respawnPoint;
        public Combatant Player => player;
        public int Kills { get; private set; }
        /// <summary>The area the player is in (null between areas).</summary>
        public ZoneArea Area { get; private set; }

        public event Action<int> KillsChanged;
        /// <summary>A monster the player killed just died (before <see cref="KillsChanged"/>); roll its loot here.</summary>
        public event Action<Combatant> MonsterSlain;
        /// <summary>The player walked into another area (null: none).</summary>
        public event Action<ZoneArea> AreaChanged;

        void Awake()
        {
            Instance = this;
            GameData.Load(combatData, skillsData, itemsData);
            // Android defaults to 30 fps; target lock combat reads much better at 60.
            Application.targetFrameRate = targetFrameRate;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (player == null || Time.time < nextAreaCheck) return;
            nextAreaCheck = Time.time + areaCheckInterval;
            var area = ZoneArea.At(player.transform.position);
            if (area == Area) return;
            Area = area;
            if (area != null)
                Debug.Log($"[Zone] Entered {area.AreaName} in {zoneName}{(area.LevelText.Length > 0 ? $" ({area.LevelText})" : "")}");
            AreaChanged?.Invoke(area);
        }

        public void RegisterKill(Combatant monster)
        {
            Kills++;
            Debug.Log($"[Combat] {monster.DisplayName} slain in {zoneName} ({Kills} total)");
            MonsterSlain?.Invoke(monster);
            KillsChanged?.Invoke(Kills);
        }
    }
}
