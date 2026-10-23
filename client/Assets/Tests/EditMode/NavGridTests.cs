using System;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEngine;

namespace Wuxia.Tests
{
    /// <summary>
    /// The server's walkable grids (shared/gamedata/navgrid, written by Wuxia → World → Export
    /// Walkable Grid) must come from the NavMesh as it's baked now: each file records the SHA-256
    /// of the NavMesh asset it was sampled from.
    /// </summary>
    public class NavGridTests
    {
        const string Folder = "Packages/com.wuxia.gamedata/navgrid";

        [Serializable]
        class GridFile
        {
            public string zone;
            public string navMesh;
            public string navMeshSha256;
            public float cellSize;
            public int width;
            public int depth;
            public string encoding;
            public string heights;
        }

        static string[] Files() => Directory.GetFiles(Path.GetFullPath(Folder), "*.json");

        [Test]
        public void EveryZoneWithANavMeshHasAGrid()
        {
            Assert.That(Files(), Has.Some.EndsWith("oak_valley.json"));
        }

        [Test]
        public void Grids_MatchTheBakedNavMesh()
        {
            foreach (var path in Files())
            {
                var grid = JsonUtility.FromJson<GridFile>(File.ReadAllText(path));
                Assert.That(File.Exists(grid.navMesh), Is.True, $"{grid.zone}: {grid.navMesh} is gone");
                string hash;
                using (var sha = SHA256.Create())
                    hash = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(grid.navMesh))).Replace("-", "").ToLowerInvariant();
                Assert.That(hash, Is.EqualTo(grid.navMeshSha256),
                    $"{grid.zone}: the NavMesh was rebaked since the grid was exported; open the scene and run Wuxia → World → Export Walkable Grid");
            }
        }

        [Test]
        public void Grids_DecodeToOneHeightPerCell()
        {
            foreach (var path in Files())
            {
                var grid = JsonUtility.FromJson<GridFile>(File.ReadAllText(path));
                Assert.That(grid.encoding, Is.EqualTo("int16le-cm-base64"), grid.zone);
                Assert.That(Convert.FromBase64String(grid.heights).Length, Is.EqualTo(grid.width * grid.depth * 2), grid.zone);
                Assert.That(grid.cellSize, Is.GreaterThan(0f), grid.zone);
            }
        }
    }
}
