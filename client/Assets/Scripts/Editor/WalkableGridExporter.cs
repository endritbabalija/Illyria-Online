using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Wuxia.EditorTools
{
    /// <summary>
    /// Writes the open zone's walkable ground for the server: the baked NavMesh, kept to the part
    /// reachable from the respawn point (so the slopes outside the valley walls and the ledge tops
    /// are left out), sampled on a 1 m grid of ground heights. The Nakama server reads it to check
    /// moves, path monsters and place them on the ground (shared/gamedata/navgrid/&lt;zone&gt;.json).
    /// The file records a SHA-256 of the NavMesh asset, and an EditMode test fails when they no
    /// longer match, so rerun this after every NavMesh bake.
    /// Menu: Wuxia → World → Export Walkable Grid. From MCP: Wuxia.EditorTools.WalkableGridExporter.Export().
    /// </summary>
    public static class WalkableGridExporter
    {
        public const string OutputFolder = "Packages/com.wuxia.gamedata/navgrid";
        public const float CellSize = 1f;
        /// <summary>Height value of a cell nobody can walk on.</summary>
        public const short Blocked = short.MinValue;
        const string SpawnPointPath = "Gameplay/RespawnPoint";
        /// <summary>NavMesh vertices closer than this are treated as the same vertex when grouping triangles.</summary>
        const float WeldDistance = 0.01f;

        /// <summary>The file layout; field names are what the server reads (server/src/navgrid.ts).</summary>
        [Serializable]
        public class GridFile
        {
            public int version = 1;
            public string zone;
            public string scene;
            /// <summary>The NavMesh asset this was sampled from, and the SHA-256 of its bytes.</summary>
            public string navMesh;
            public string navMeshSha256;
            public float cellSize;
            /// <summary>World X and Z of the grid's south-west corner.</summary>
            public float originX;
            public float originZ;
            /// <summary>Cells along X and along Z.</summary>
            public int width;
            public int depth;
            /// <summary>"int16le-cm-base64": one little-endian Int16 per cell, row by row (Z outer, X inner), ground height in centimetres; -32768 = not walkable.</summary>
            public string encoding = "int16le-cm-base64";
            public string heights;
        }

        [MenuItem("Wuxia/World/Export Walkable Grid")]
        static void ExportMenu() => Debug.Log(Export());

        public static string Export()
        {
            var started = DateTime.Now;
            var scene = SceneManager.GetActiveScene();
            var zone = ZoneIdFor(scene.name);
            var navMeshPath = NavMeshAssetPath(scene);
            var spawn = GameObject.Find(SpawnPointPath);
            if (spawn == null) throw new InvalidOperationException($"No {SpawnPointPath} in {scene.name}");
            if (!NavMesh.SamplePosition(spawn.transform.position, out var seed, 3f, NavMesh.AllAreas))
                throw new InvalidOperationException("The respawn point isn't on the NavMesh; bake it first");

            var mesh = NavMesh.CalculateTriangulation();
            var reachable = ReachableTriangles(mesh, seed.position, out var groups, out var pathChecks);

            // Bounds of the reachable part, whole cells.
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            for (var t = 0; t < reachable.Length; t++)
            {
                if (!reachable[t]) continue;
                for (var k = 0; k < 3; k++)
                {
                    var v = mesh.vertices[mesh.indices[t * 3 + k]];
                    min = Vector2.Min(min, new Vector2(v.x, v.z));
                    max = Vector2.Max(max, new Vector2(v.x, v.z));
                }
            }
            var originX = Mathf.Floor(min.x / CellSize) * CellSize;
            var originZ = Mathf.Floor(min.y / CellSize) * CellSize;
            var width = Mathf.CeilToInt((max.x - originX) / CellSize);
            var depth = Mathf.CeilToInt((max.y - originZ) / CellSize);

            var heights = new short[width * depth];
            for (var i = 0; i < heights.Length; i++) heights[i] = Blocked;
            var walkable = 0;
            for (var t = 0; t < reachable.Length; t++)
                if (reachable[t]) walkable += Rasterize(mesh, t, originX, originZ, width, depth, heights);

            var bytes = new byte[heights.Length * 2];
            for (var i = 0; i < heights.Length; i++)
            {
                bytes[i * 2] = (byte)(heights[i] & 0xff);
                bytes[i * 2 + 1] = (byte)((heights[i] >> 8) & 0xff);
            }

            var file = new GridFile
            {
                zone = zone,
                scene = scene.path,
                navMesh = navMeshPath,
                navMeshSha256 = Sha256(navMeshPath),
                cellSize = CellSize,
                originX = originX,
                originZ = originZ,
                width = width,
                depth = depth,
                heights = Convert.ToBase64String(bytes),
            };
            var folder = Path.GetFullPath(OutputFolder);
            Directory.CreateDirectory(folder);
            var outPath = Path.Combine(folder, zone + ".json");
            File.WriteAllText(outPath, JsonUtility.ToJson(file, true) + "\n");
            AssetDatabase.Refresh();

            var reachableCount = 0;
            foreach (var r in reachable) if (r) reachableCount++;
            return $"[NavGrid] {zone}: {width}×{depth} cells of {CellSize} m from ({originX}, {originZ}), {walkable} walkable; " +
                   $"{reachableCount}/{reachable.Length} NavMesh triangles reachable from the respawn point ({groups} groups, {pathChecks} path checks), " +
                   $"{(DateTime.Now - started).TotalSeconds:0.0}s → {outPath}";
        }

        /// <summary>
        /// Triangles reachable from <paramref name="seed"/>: groups triangles that share (welded)
        /// vertices, then asks Unity's pathfinder once per group whether the seed reaches it, which
        /// also catches groups joined across tile borders without a shared vertex.
        /// </summary>
        static bool[] ReachableTriangles(NavMeshTriangulation mesh, Vector3 seed, out int groupCount, out int pathChecks)
        {
            var triCount = mesh.indices.Length / 3;
            var weld = new Dictionary<Vector3Int, int>();
            var vertexId = new int[mesh.vertices.Length];
            for (var i = 0; i < mesh.vertices.Length; i++)
            {
                var v = mesh.vertices[i];
                var key = new Vector3Int(Mathf.RoundToInt(v.x / WeldDistance), Mathf.RoundToInt(v.y / WeldDistance), Mathf.RoundToInt(v.z / WeldDistance));
                if (!weld.TryGetValue(key, out var id)) weld[key] = id = weld.Count;
                vertexId[i] = id;
            }

            var parent = new int[weld.Count];
            for (var i = 0; i < parent.Length; i++) parent[i] = i;
            int Find(int x)
            {
                while (parent[x] != x) x = parent[x] = parent[parent[x]];
                return x;
            }
            for (var t = 0; t < triCount; t++)
            {
                var a = Find(vertexId[mesh.indices[t * 3]]);
                parent[Find(vertexId[mesh.indices[t * 3 + 1]])] = a;
                parent[Find(vertexId[mesh.indices[t * 3 + 2]])] = a;
            }

            var groupReachable = new Dictionary<int, bool>();
            var path = new NavMeshPath();
            pathChecks = 0;
            var result = new bool[triCount];
            for (var t = 0; t < triCount; t++)
            {
                var group = Find(vertexId[mesh.indices[t * 3]]);
                if (!groupReachable.TryGetValue(group, out var ok))
                {
                    var c = (mesh.vertices[mesh.indices[t * 3]] + mesh.vertices[mesh.indices[t * 3 + 1]] + mesh.vertices[mesh.indices[t * 3 + 2]]) / 3f;
                    ok = NavMesh.SamplePosition(c, out var hit, 0.5f, NavMesh.AllAreas)
                         && NavMesh.CalculatePath(seed, hit.position, NavMesh.AllAreas, path)
                         && path.status == NavMeshPathStatus.PathComplete;
                    pathChecks++;
                    groupReachable[group] = ok;
                }
                result[t] = ok;
            }
            groupCount = groupReachable.Count;
            return result;
        }

        /// <summary>Marks every cell whose centre lies in the triangle (seen from above) with the triangle's height there; the highest wins.</summary>
        static int Rasterize(NavMeshTriangulation mesh, int t, float originX, float originZ, int width, int depth, short[] heights)
        {
            var a = mesh.vertices[mesh.indices[t * 3]];
            var b = mesh.vertices[mesh.indices[t * 3 + 1]];
            var c = mesh.vertices[mesh.indices[t * 3 + 2]];
            var x0 = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.x, Mathf.Min(b.x, c.x)) - originX) / CellSize));
            var x1 = Mathf.Min(width - 1, Mathf.FloorToInt((Mathf.Max(a.x, Mathf.Max(b.x, c.x)) - originX) / CellSize));
            var z0 = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.z, Mathf.Min(b.z, c.z)) - originZ) / CellSize));
            var z1 = Mathf.Min(depth - 1, Mathf.FloorToInt((Mathf.Max(a.z, Mathf.Max(b.z, c.z)) - originZ) / CellSize));
            var area = (b.x - a.x) * (c.z - a.z) - (c.x - a.x) * (b.z - a.z);
            if (Mathf.Abs(area) < 1e-6f) return 0;

            var marked = 0;
            for (var z = z0; z <= z1; z++)
            for (var x = x0; x <= x1; x++)
            {
                var px = originX + (x + 0.5f) * CellSize;
                var pz = originZ + (z + 0.5f) * CellSize;
                var w1 = ((px - a.x) * (c.z - a.z) - (c.x - a.x) * (pz - a.z)) / area;
                var w2 = ((b.x - a.x) * (pz - a.z) - (px - a.x) * (b.z - a.z)) / area;
                var w0 = 1f - w1 - w2;
                const float edge = -1e-4f;
                if (w0 < edge || w1 < edge || w2 < edge) continue;
                var y = w0 * a.y + w1 * b.y + w2 * c.y;
                var cm = (short)Mathf.Clamp(Mathf.RoundToInt(y * 100f), short.MinValue + 1, short.MaxValue);
                var i = z * width + x;
                if (heights[i] == Blocked) marked++;
                if (heights[i] == Blocked || cm > heights[i]) heights[i] = cm;
            }
            return marked;
        }

        /// <summary>OakValley → oak_valley, the zone id in zones.json.</summary>
        public static string ZoneIdFor(string sceneName)
        {
            var sb = new System.Text.StringBuilder();
            for (var i = 0; i < sceneName.Length; i++)
            {
                var ch = sceneName[i];
                if (char.IsUpper(ch) && i > 0) sb.Append('_');
                sb.Append(char.ToLowerInvariant(ch));
            }
            return sb.ToString();
        }

        /// <summary>The scene's baked NavMesh asset (in the folder named after the scene).</summary>
        public static string NavMeshAssetPath(Scene scene)
        {
            var folder = Path.ChangeExtension(scene.path, null);
            var guids = AssetDatabase.FindAssets("t:NavMeshData", new[] { folder });
            if (guids.Length != 1) throw new InvalidOperationException($"Expected one NavMesh asset in {folder}, found {guids.Length}; bake the NavMesh first");
            return AssetDatabase.GUIDToAssetPath(guids[0]);
        }

        public static string Sha256(string assetPath)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Path.GetFullPath(assetPath)))).Replace("-", "").ToLowerInvariant();
        }
    }
}
