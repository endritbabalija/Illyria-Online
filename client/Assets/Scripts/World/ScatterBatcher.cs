using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Wuxia.World
{
    /// <summary>
    /// Merges the mesh renderers under this object into one mesh per grid cell and material when the
    /// scene loads, so a forest of a few hundred trees draws as a few dozen meshes on a phone. Objects
    /// with a LOD group merge their LOD 0 and their last LOD separately and the cell swaps between the
    /// two by screen size. The original objects stay (colliders and all) with their renderers turned
    /// off, so the scene is still edited tree by tree. Leave scattered objects off Batching Static:
    /// this replaces that.
    /// </summary>
    public class ScatterBatcher : MonoBehaviour
    {
        [Tooltip("Size of a merged cell on the ground plane (metres).")]
        [SerializeField] float cellSize = 40f;
        [Tooltip("A cell shows its objects' full-detail LOD above this share of the screen height and their distant LOD below it (0: LOD groups ignored, only LOD 0 is merged).")]
        [Range(0f, 1f)]
        [SerializeField] float detailScreenHeight;
        [Tooltip("Hide a cell once it's below this share of the screen height (0: never).")]
        [Range(0f, 1f)]
        [SerializeField] float cullScreenHeight;
        [Tooltip("Full-detail cells cast their shadows from the distant LOD meshes instead (shadows don't need the detail).")]
        [SerializeField] bool shadowsFromDistantLod = true;

        void Awake() => Merge();

        void Merge()
        {
            var cells = new Dictionary<(int x, int z), Dictionary<(bool far, Material material, ShadowCastingMode shadows), List<CombineInstance>>>();
            foreach (var r in GetComponentsInChildren<MeshRenderer>())
            {
                if (!r.enabled || !r.TryGetComponent<MeshFilter>(out var filter) || filter.sharedMesh == null) continue;
                var far = false;
                var lodGroup = r.GetComponentInParent<LODGroup>();
                if (lodGroup != null)
                {
                    var lods = lodGroup.GetLODs();
                    var level = System.Array.FindIndex(lods, l => System.Array.IndexOf(l.renderers, r) >= 0);
                    far = level > 0 && level == lods.Length - 1;
                    lodGroup.enabled = false;
                    if (level > 0 && (!far || detailScreenHeight <= 0f))
                    {
                        r.enabled = false;
                        continue;
                    }
                }

                var centre = r.bounds.center;
                var cell = (Mathf.FloorToInt(centre.x / cellSize), Mathf.FloorToInt(centre.z / cellSize));
                if (!cells.TryGetValue(cell, out var groups)) cells[cell] = groups = new Dictionary<(bool, Material, ShadowCastingMode), List<CombineInstance>>();
                var materials = r.sharedMaterials;
                for (var i = 0; i < filter.sharedMesh.subMeshCount && i < materials.Length; i++)
                {
                    var key = (far, materials[i], r.shadowCastingMode);
                    if (!groups.TryGetValue(key, out var list)) groups[key] = list = new List<CombineInstance>();
                    list.Add(new CombineInstance { mesh = filter.sharedMesh, subMeshIndex = i, transform = r.localToWorldMatrix });
                }
                r.enabled = false;
            }

            foreach (var cell in cells)
            {
                var root = new GameObject($"{name} {cell.Key.x},{cell.Key.z}");
                root.transform.SetParent(transform, false);
                root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                root.transform.localScale = Vector3.one;
                var near = new List<Renderer>();
                var distant = new List<Renderer>();
                var hasDistant = false;
                foreach (var key in cell.Value.Keys) hasDistant |= key.far;
                var shadowProxies = hasDistant && shadowsFromDistantLod;
                foreach (var group in cell.Value)
                {
                    var (far, material, shadows) = group.Key;
                    var mesh = new Mesh { name = $"{root.name} {material.name}{(far ? " far" : "")}", indexFormat = IndexFormat.UInt32 };
                    mesh.CombineMeshes(group.Value.ToArray(), true, true);
                    var renderer = AddRenderer(root.transform, mesh, material, far || !shadowProxies ? shadows : ShadowCastingMode.Off);
                    (far ? distant : near).Add(renderer);
                    if (far && shadowProxies && shadows != ShadowCastingMode.Off)
                        near.Add(AddRenderer(root.transform, mesh, material, ShadowCastingMode.ShadowsOnly));
                }

                var levels = new List<LOD>();
                if (distant.Count > 0)
                {
                    levels.Add(new LOD(detailScreenHeight, near.ToArray()));
                    levels.Add(new LOD(cullScreenHeight, distant.ToArray()));
                }
                else if (cullScreenHeight > 0f) levels.Add(new LOD(cullScreenHeight, near.ToArray()));
                if (levels.Count == 0) continue;
                var cellLod = root.AddComponent<LODGroup>();
                cellLod.SetLODs(levels.ToArray());
                cellLod.RecalculateBounds();
            }
        }

        static MeshRenderer AddRenderer(Transform parent, Mesh mesh, Material material, ShadowCastingMode shadows)
        {
            var go = new GameObject(shadows == ShadowCastingMode.ShadowsOnly ? mesh.name + " shadow" : mesh.name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = shadows;
            return renderer;
        }
    }
}
