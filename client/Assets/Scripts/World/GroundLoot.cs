using System.Collections.Generic;
using UnityEngine;
using Wuxia.Combat;
using Wuxia.Data;
using Wuxia.Items;
using Wuxia.Monsters;
using Wuxia.Player;
using Wuxia.UI;

namespace Wuxia.World
{
    /// <summary>
    /// Loot on the ground: rolls a slain monster's loot table, scatters the drops around the corpse
    /// (gold as a coin pile, items as their model or a pouch, each with a beam and glow disc in the
    /// rarity colour), and auto-loots them once the living player is within autoLootRadius
    /// (items.json loot block). A pickup goes through <see cref="PlayerItems.PickUp"/>, then the model
    /// flies to the player. A full bag leaves the drop where it is; drops vanish after groundTime.
    /// Everything is pooled and the per-frame work is a flat distance check per drop.
    /// </summary>
    public class GroundLoot : MonoBehaviour
    {
        enum Phase { Pop, Ground, Fly }

        class Drop
        {
            public LootDrop Loot;
            public string Source;
            public GroundDropView View;
            public GameObject Model;
            public GameObject ModelPrefab;
            public ItemModelRegistry.Entry Entry;
            public Phase Phase;
            public Vector3 From;
            public Vector3 Rest;
            public float SpawnTime;
            public float PhaseTime;
            public float ExpireAt;
            /// <summary>Items.Version when pickup last failed (bag full); retried only after the bag changes.</summary>
            public int BlockedVersion = -1;
        }

        [SerializeField] PlayerCombat player;
        [SerializeField] ItemModelRegistry models;
        [SerializeField] FloatingCombatText floatingText;
        [Tooltip("LootDrop.prefab: root with GroundDropView, beam and glow disc.")]
        [SerializeField] GroundDropView dropPrefab;
        [Tooltip("Unlit transparent material; copied once per rarity colour.")]
        [SerializeField] Material glowMaterial;
        [Tooltip("What the drop spot is snapped onto.")]
        [SerializeField] LayerMask groundMask = 1;
        [SerializeField] Color goldColor = new Color(1f, 0.82f, 0.25f);
        [SerializeField] float glowAlpha = 0.45f;
        [SerializeField] float scatterMin = 0.6f;
        [SerializeField] float scatterMax = 1.4f;
        [SerializeField] float popDuration = 0.35f;
        [SerializeField] float popHeight = 0.9f;
        [SerializeField] float flyDuration = 0.3f;
        [Tooltip("Where on the player picked-up loot flies to, above the feet.")]
        [SerializeField] float flyHeight = 1.2f;
        [SerializeField] float discPulse = 0.12f;
        [SerializeField] int prewarm = 8;

        readonly List<Drop> active = new List<Drop>();
        readonly Stack<Drop> freeDrops = new Stack<Drop>();
        readonly Stack<GroundDropView> freeViews = new Stack<GroundDropView>();
        readonly Dictionary<GameObject, Stack<GameObject>> freeModels = new Dictionary<GameObject, Stack<GameObject>>();
        readonly Dictionary<string, Material> glowByRarity = new Dictionary<string, Material>();
        readonly System.Random rng = new System.Random();
        LootRules rules;
        Material goldGlow;
        int bagFullNoticeVersion = -1;

        /// <summary>Drops lying on the ground right now (for tests and the perf check).</summary>
        public int ActiveCount => active.Count;

        void Start()
        {
            rules = GameData.Items.loot;
            popDuration = Mathf.Min(popDuration, Mathf.Max(0.05f, rules.pickupDelay));
            goldGlow = MakeGlow(goldColor);
            for (var i = 0; i < prewarm; i++) freeViews.Push(CreateView());
            ZoneSession.Instance.MonsterSlain += OnMonsterSlain;
        }

        void OnDestroy()
        {
            if (ZoneSession.Instance != null) ZoneSession.Instance.MonsterSlain -= OnMonsterSlain;
            foreach (var m in glowByRarity.Values) Destroy(m);
            if (goldGlow != null) Destroy(goldGlow);
        }

        void OnMonsterSlain(Combatant monster)
        {
            var ai = monster.GetComponent<MonsterAI>();
            if (ai == null) return;
            var loot = LootRoller.RollFor(ai.Data.id, player.Items.Factory, rng);
            if (loot.Count == 0) return;
            Spawn(loot, monster.transform.position, monster.DisplayName);
        }

        /// <summary>Scatters drops around <paramref name="origin"/>; <paramref name="source"/> goes in the ledger ("loot: Grey Wolf").</summary>
        public void Spawn(List<LootDrop> loot, Vector3 origin, string source)
        {
            var names = new System.Text.StringBuilder();
            var angle = (float)rng.NextDouble() * 360f;
            var step = 360f / loot.Count;
            for (var i = 0; i < loot.Count; i++)
            {
                var spot = origin + Quaternion.Euler(0f, angle + step * i, 0f) * Vector3.forward * Random.Range(scatterMin, scatterMax);
                if (loot.Count == 1) spot = origin + (spot - origin) * 0.5f;
                SpawnOne(loot[i], origin, Snap(spot), source);
                if (names.Length > 0) names.Append(", ");
                names.Append(loot[i]);
            }
            Debug.Log($"[Loot] {source} dropped {names}");
        }

        Vector3 Snap(Vector3 spot)
        {
            if (Physics.Raycast(spot + Vector3.up * 3f, Vector3.down, out var hit, 8f, groundMask, QueryTriggerInteraction.Ignore))
                return hit.point;
            return spot;
        }

        void SpawnOne(LootDrop loot, Vector3 from, Vector3 rest, string source)
        {
            var drop = freeDrops.Count > 0 ? freeDrops.Pop() : new Drop();
            drop.Loot = loot;
            drop.Source = source;
            drop.Entry = loot.IsGold ? models.Gold : models.Find(loot.Item.Def.modelKey) ?? models.Pouch;
            drop.View = freeViews.Count > 0 ? freeViews.Pop() : CreateView();
            drop.ModelPrefab = drop.Entry.prefab;
            drop.Model = TakeModel(drop.ModelPrefab, drop.View.ModelSlot);
            if (drop.Model != null)
            {
                drop.Model.transform.localPosition = drop.Entry.groundOffset;
                drop.Model.transform.localRotation = Quaternion.Euler(drop.Entry.groundEuler);
            }

            var glow = loot.IsGold ? goldGlow : GlowFor(loot.Item.Rarity);
            drop.View.Disc.sharedMaterial = glow;
            drop.View.Beam.sharedMaterial = glow;
            // Gold stays modest: a glow disc, no beam.
            drop.View.Beam.gameObject.SetActive(!loot.IsGold);

            var t = drop.View.transform;
            t.localScale = Vector3.one;
            t.SetPositionAndRotation(from, Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f));
            drop.View.gameObject.SetActive(true);

            var now = Time.time;
            drop.From = from;
            drop.Rest = rest;
            drop.Phase = Phase.Pop;
            drop.SpawnTime = now;
            drop.PhaseTime = now;
            drop.ExpireAt = now + rules.groundTime;
            drop.BlockedVersion = -1;
            active.Add(drop);
        }

        void Update()
        {
            if (active.Count == 0) return;
            var now = Time.time;
            var playerPos = player.transform.position;
            var canLoot = !player.IsDead;
            var radius2 = rules.autoLootRadius * rules.autoLootRadius;

            for (var i = active.Count - 1; i >= 0; i--)
            {
                var drop = active[i];
                var t = drop.View.transform;

                if (drop.Phase == Phase.Fly)
                {
                    var k = (now - drop.PhaseTime) / flyDuration;
                    if (k >= 1f)
                    {
                        Release(i);
                        continue;
                    }
                    t.position = Vector3.Lerp(drop.From, playerPos + Vector3.up * flyHeight, k * k);
                    t.localScale = Vector3.one * (1f - 0.7f * k);
                    continue;
                }

                if (now >= drop.ExpireAt || drop.Loot.Taken)
                {
                    Release(i);
                    continue;
                }

                if (drop.Phase == Phase.Pop)
                {
                    var k = (now - drop.PhaseTime) / popDuration;
                    if (k >= 1f)
                    {
                        drop.Phase = Phase.Ground;
                        t.position = drop.Rest;
                    }
                    else t.position = Vector3.Lerp(drop.From, drop.Rest, k) + Vector3.up * (popHeight * 4f * k * (1f - k));
                }
                else
                {
                    var e = drop.Entry;
                    var wave = Mathf.Sin((now - drop.SpawnTime) * 3f);
                    if (e.groundBob > 0f) t.position = drop.Rest + Vector3.up * (e.groundBob * (0.5f + 0.5f * wave));
                    if (e.groundSpin != 0f) t.Rotate(0f, e.groundSpin * Time.deltaTime, 0f, Space.World);
                    drop.View.Disc.transform.localScale = DiscScale(1f + discPulse * wave);
                }

                if (!canLoot || now < drop.SpawnTime + rules.pickupDelay) continue;
                var dx = drop.Rest.x - playerPos.x;
                var dz = drop.Rest.z - playerPos.z;
                if (dx * dx + dz * dz <= radius2) TryPickUp(drop, now);
            }
        }

        Vector3 discBase = Vector3.one;

        Vector3 DiscScale(float k) => new Vector3(discBase.x * k, discBase.y, discBase.z * k);

        void TryPickUp(Drop drop, float now)
        {
            var items = player.Items;
            if (drop.BlockedVersion == items.Version) return;
            var result = items.PickUp(drop.Loot, drop.Source);
            if (result == PickupBlock.BagFull)
            {
                drop.BlockedVersion = items.Version;
                if (bagFullNoticeVersion != items.Version)
                {
                    bagFullNoticeVersion = items.Version;
                    player.Tell("Your bag is full");
                }
                return;
            }
            if (result != PickupBlock.None) return; // AlreadyTaken: released next frame

            var lift = player.Combatant.AimHeight;
            if (drop.Loot.IsGold)
                floatingText.ShowAt(player.transform.position + Vector3.up * lift, $"+{drop.Loot.Gold} gold", goldColor, 0.9f);
            else
            {
                var item = drop.Loot.Item;
                var text = item.Count > 1 ? $"{item.Name} ×{item.Count}" : item.Name;
                floatingText.ShowAt(player.transform.position + Vector3.up * (lift + 0.45f), text, ItemText.RarityColor(item.Rarity), 0.9f);
            }

            drop.Phase = Phase.Fly;
            drop.PhaseTime = now;
            drop.From = drop.View.transform.position;
            drop.View.Beam.gameObject.SetActive(false);
        }

        void Release(int index)
        {
            var drop = active[index];
            active.RemoveAt(index);
            GiveBackModel(drop.ModelPrefab, drop.Model);
            drop.View.gameObject.SetActive(false);
            drop.View.Disc.transform.localScale = discBase;
            freeViews.Push(drop.View);
            drop.View = null;
            drop.Model = null;
            drop.ModelPrefab = null;
            drop.Loot = null;
            drop.Entry = null;
            freeDrops.Push(drop);
        }

        GroundDropView CreateView()
        {
            var view = Instantiate(dropPrefab, transform);
            view.gameObject.SetActive(false);
            discBase = view.Disc.transform.localScale;
            return view;
        }

        GameObject TakeModel(GameObject prefab, Transform parent)
        {
            if (prefab == null) return null;
            GameObject model;
            if (freeModels.TryGetValue(prefab, out var stack) && stack.Count > 0)
            {
                model = stack.Pop();
                model.transform.SetParent(parent, false);
            }
            else
            {
                model = Instantiate(prefab, parent, false);
                foreach (var c in model.GetComponentsInChildren<Collider>(true)) c.enabled = false;
                // Small things on the ground: the glow disc reads better than a shadow and costs nothing.
                foreach (var r in model.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            model.SetActive(true);
            return model;
        }

        void GiveBackModel(GameObject prefab, GameObject model)
        {
            if (prefab == null || model == null) return;
            model.SetActive(false);
            if (!freeModels.TryGetValue(prefab, out var stack))
            {
                stack = new Stack<GameObject>();
                freeModels[prefab] = stack;
            }
            stack.Push(model);
        }

        Material GlowFor(RarityData rarity)
        {
            var key = rarity != null ? rarity.id : ItemsData.CommonRarity;
            if (!glowByRarity.TryGetValue(key, out var material))
            {
                material = MakeGlow(ItemText.RarityColor(rarity));
                glowByRarity[key] = material;
            }
            return material;
        }

        Material MakeGlow(Color color)
        {
            var material = new Material(glowMaterial) { name = glowMaterial.name + " " + ColorUtility.ToHtmlStringRGB(color) };
            color.a = glowAlpha;
            material.SetColor("_BaseColor", color);
            return material;
        }
    }
}
