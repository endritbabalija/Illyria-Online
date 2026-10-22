using System;
using UnityEngine;
using Wuxia.Combat;
using Wuxia.Data;

namespace Wuxia.World
{
    /// <summary>
    /// Per-zone root: loads the shared game data, knows the player and the Han respawn point, and
    /// counts kills. Runs first so everything else can read <see cref="GameData"/> from Start on.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class ZoneSession : MonoBehaviour
    {
        public static ZoneSession Instance { get; private set; }

        [Tooltip("combat.json from the com.wuxia.gamedata package (shared with the server).")]
        [SerializeField] TextAsset combatData;
        [SerializeField] string zoneName = "Oak Valley";
        [Tooltip("Where the player revives after dying: in front of the zone's Han.")]
        [SerializeField] Transform respawnPoint;
        [SerializeField] Combatant player;
        [SerializeField] int targetFrameRate = 60;

        public string ZoneName => zoneName;
        public Transform RespawnPoint => respawnPoint;
        public Combatant Player => player;
        public int Kills { get; private set; }

        public event Action<int> KillsChanged;

        void Awake()
        {
            Instance = this;
            GameData.Load(combatData);
            // Android defaults to 30 fps; target lock combat reads much better at 60.
            Application.targetFrameRate = targetFrameRate;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void RegisterKill(Combatant monster)
        {
            Kills++;
            Debug.Log($"[Combat] {monster.DisplayName} slain in {zoneName} ({Kills} total)");
            KillsChanged?.Invoke(Kills);
        }
    }
}
