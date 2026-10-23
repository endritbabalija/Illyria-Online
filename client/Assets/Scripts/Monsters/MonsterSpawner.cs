using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Wuxia.Combat;

namespace Wuxia.Monsters
{
    /// <summary>A den: keeps <see cref="count"/> monsters alive around this point, respawning the dead.</summary>
    public class MonsterSpawner : MonoBehaviour
    {
        [SerializeField] MonsterAI prefab;
        [SerializeField] int count = 5;
        [SerializeField] float radius = 6f;

        readonly List<MonsterAI> pack = new List<MonsterAI>();

        void Start()
        {
            for (var i = 0; i < count; i++)
            {
                var position = RandomPoint();
                var monster = Instantiate(prefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), transform);
                monster.name = $"{prefab.name} {i + 1}";
                pack.Add(monster);
                monster.Spawn(this, position);
            }
        }

        public void OnDespawned(MonsterAI monster) => StartCoroutine(RespawnAfterDelay(monster));

        IEnumerator RespawnAfterDelay(MonsterAI monster)
        {
            yield return new WaitForSeconds(monster.RespawnDelay);
            monster.Spawn(this, RandomPoint());
        }

        public void AlertPack(MonsterAI caller, Combatant attacker)
        {
            foreach (var member in pack)
                if (member != caller && member.isActiveAndEnabled)
                    member.Assist(attacker, caller.transform.position);
        }

        /// <summary>A NavMesh point in the den, clear of other monsters if one can be found.</summary>
        Vector3 RandomPoint()
        {
            Vector3? fallback = null;
            for (var attempt = 0; attempt < 10; attempt++)
            {
                var offset = Random.insideUnitCircle * radius;
                if (!NavMesh.SamplePosition(transform.position + new Vector3(offset.x, 0f, offset.y), out var hit, 2f, NavMesh.AllAreas))
                    continue;
                if (MonsterAI.IsFree(hit.position, prefab.PersonalSpace)) return hit.position;
                fallback ??= hit.position;
            }
            return fallback ?? transform.position;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
