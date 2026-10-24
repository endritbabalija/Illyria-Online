using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Wuxia.Monsters;
using Wuxia.World;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace Wuxia.EditorTools
{
    /// <summary>
    /// Builds Oak Valley's layout into the open OakValley scene, south to north: the south gate and
    /// the Han, the meadow (shepherd's fold), the oak forest (dried spring), the river and its stone
    /// bridge, the bandit camp on a hill (ruined tower), the wolf den and the north pass to Serpent
    /// Marsh. One Unity terrain (heights and painted layers from <see cref="Height"/> and
    /// <see cref="Paint"/>), procedural ground textures, placeholder trees, rocks and props, invisible
    /// bounds, leap ledges and <see cref="ZoneArea"/>s. Re-running replaces <c>Environment/Generated</c>
    /// and <c>Gameplay/Areas</c> and moves the gameplay anchors (Han, respawn point, Han Keeper,
    /// shepherd, monster spawners, signs) to their spots; missing spawners are created. Rebake the NavMesh afterwards.
    /// Menu: Wuxia → World → Build Oak Valley; MCP: <c>return Wuxia.EditorTools.OakValleyBuilder.Build();</c>
    /// </summary>
    public static class OakValleyBuilder
    {
        const string ArtRoot = "Assets/Art/OakValley";
        const string TerrainDataPath = "Assets/Scenes/OakValley/OakValleyTerrain.asset";
        const int HeightResolution = 513;
        const int AlphaResolution = 512;
        const int TextureSize = 512;

        /// <summary>Solid for the player and monsters (and baked into the NavMesh) but ignored by the camera, taps and loot.</summary>
        public const int ObstacleLayer = 11;
        const int WaterLayer = 4;
        const int NpcLayer = 10;

        // ---------------------------------------------------------------- layout (world metres, x east, z north)

        static readonly Vector3 TerrainOrigin = new Vector3(-170f, -6f, -230f);
        static readonly Vector3 TerrainSize = new Vector3(340f, 90f, 460f);
        const float SouthWallZ = -208f;
        const float NorthWallZ = 208f;
        const float BoundsInset = 2.5f;

        const float RiverHalfWidth = 5.5f;
        const float BridgeX = 10f;
        const float BridgeLength = 22f;
        const float BridgeWidth = 4.4f;
        const float BridgeRise = 2.2f;
        const float ArchRadius = 5.5f;

        static readonly Vector3 HanRoot = new Vector3(-16f, 0f, -192f);
        static readonly Vector2 HanPad = new Vector2(-22f, -186f);
        static readonly Vector2 Gate = new Vector2(0f, -206f);
        static readonly Vector2 Hut = new Vector2(58f, -130f);
        static readonly Vector2 ShepherdSpot = new Vector2(47f, -124f);
        static readonly Vector2 Spring = new Vector2(-58f, -15f);
        static readonly Vector2 Camp = new Vector2(-80f, 111f);
        static readonly Vector2 Tower = new Vector2(-88f, 117f);
        const float CampRise = 7f;
        const float PalisadeRadius = 16f;
        static readonly Vector2 Den = new Vector2(82f, 168f);
        static readonly Vector2 Cave = new Vector2(104f, 174f);
        static readonly Vector2 NorthExit = new Vector2(0f, 203f);
        static readonly Vector2 MeadowWestPack = new Vector2(-42f, -136f);
        static readonly Vector2 MeadowEastPack = new Vector2(44f, -88f);
        static readonly Vector2 DenPack = new Vector2(80f, 166f);
        static readonly Vector2 BoarSpringHerd = new Vector2(-51f, -9f);
        static readonly Vector2 BoarEastHerd = new Vector2(42f, 20f);
        static readonly Vector2 BanditCampGang = new Vector2(-77f, 108f);
        static readonly Vector2 BanditLookouts = new Vector2(-62f, 107f);

        /// <summary>Valley floor height: level at the Han, rising gently towards the north pass.</summary>
        static float FloorY(float z) => 4.5f * Smooth(-175f, 200f, z);
        static float CentreX(float z) => 10f * Mathf.Sin(z / 110f);

        /// <summary>Distance from the valley's centre line to the foot of the slopes on one side (-1 west, +1 east); the two sides meander independently.</summary>
        static float HalfWidth(float z, float side) => 114f + 13f * Mathf.Sin(z / 70f + 1f + 1.7f * side) + 7f * Mathf.Sin(z / 29f + 2f + 2.3f * side);

        /// <summary>Where the oak forest starts (south edge), wavering across the valley.</summary>
        static float ForestEdge(float x) => -68f + (Perlin(x / 45f + 300f, 0.5f) - 0.5f) * 34f + (Perlin(x / 13f, 7.5f) - 0.5f) * 10f;

        /// <summary>Signed distance outside the walkable valley (negative inside), with notches for the south gate and the north pass.</summary>
        static float Outside(float x, float z)
        {
            var offset = x - CentreX(z);
            var side = Mathf.Abs(offset) - HalfWidth(z, Mathf.Sign(offset));
            var south = SouthWallZ - z - 45f * (1f - Smooth(7f, 15f, Mathf.Abs(x)));
            var north = z - NorthWallZ - 45f * (1f - Smooth(9f, 17f, Mathf.Abs(x)));
            return SoftMax(SoftMax(side, south, 8f), north, 8f);
        }

        static float RiverZ(float x) => 54f + 9f * Mathf.Sin(x / 42f + 0.6f) + 4f * Mathf.Sin(x / 17f);
        static float RiverSlope(float x) => 9f / 42f * Mathf.Cos(x / 42f + 0.6f) + 4f / 17f * Mathf.Cos(x / 17f);

        static float RiverDistance(float x, float z)
        {
            var s = RiverSlope(x);
            return Mathf.Abs(z - RiverZ(x)) / Mathf.Sqrt(1f + s * s);
        }

        static float WaterY(float x) => FloorY(RiverZ(x)) - 0.6f;
        static float BridgeBase => FloorY(RiverZ(BridgeX)) - 0.05f;
        static Vector3 BridgeCentre => new Vector3(BridgeX, BridgeBase, RiverZ(BridgeX));

        /// <summary>Along the bridge (south to north), square to the river.</summary>
        static Vector3 BridgeAxis
        {
            get
            {
                var along = new Vector3(1f, 0f, RiverSlope(BridgeX)).normalized;
                return new Vector3(-along.z, 0f, along.x);
            }
        }

        struct Road
        {
            public float HalfWidth;
            public Vector2[] Points;
        }

        static Road[] roads;

        static Road[] Roads => roads ??= new[]
        {
            new Road
            {
                HalfWidth = 2.6f, Points = new[]
                {
                    new Vector2(0f, -232f), new Vector2(0f, -206f), new Vector2(-1f, -180f), new Vector2(8f, -150f), new Vector2(12f, -120f),
                    new Vector2(-3f, -92f), new Vector2(6f, -62f), new Vector2(-6f, -30f), new Vector2(4f, 5f), new Vector2(14f, 30f),
                    Flat(BridgeCentre - BridgeAxis * 17f), Flat(BridgeCentre - BridgeAxis * 11f),
                    Flat(BridgeCentre + BridgeAxis * 11f), Flat(BridgeCentre + BridgeAxis * 17f),
                    new Vector2(2f, 100f), new Vector2(-6f, 128f), new Vector2(0f, 160f), new Vector2(2f, 190f), new Vector2(0f, 232f),
                },
            },
            // Han door, shepherd's fold, dried spring, bandit camp, wolf den.
            new Road { HalfWidth = 1.7f, Points = new[] { new Vector2(-1f, -180f), new Vector2(-16f, -181f), new Vector2(-16f, -187f) } },
            new Road { HalfWidth = 1.6f, Points = new[] { new Vector2(12f, -120f), new Vector2(32f, -125f), new Vector2(51f, -128f) } },
            new Road { HalfWidth = 1.5f, Points = new[] { new Vector2(-6f, -30f), new Vector2(-28f, -24f), new Vector2(-50f, -17f) } },
            new Road { HalfWidth = 2f, Points = new[] { new Vector2(2f, 100f), new Vector2(-22f, 104f), new Vector2(-46f, 108f), new Vector2(-64f, 111f) } },
            new Road { HalfWidth = 1.8f, Points = new[] { new Vector2(0f, 160f), new Vector2(28f, 165f), new Vector2(52f, 168f), new Vector2(72f, 168f) } },
        };

        /// <summary>Distance from the nearest road's edge (negative on a road).</summary>
        static float RoadEdge(float x, float z)
        {
            var p = new Vector2(x, z);
            var best = float.MaxValue;
            foreach (var road in Roads)
                for (var i = 0; i + 1 < road.Points.Length; i++)
                    best = Mathf.Min(best, SegmentDistance(p, road.Points[i], road.Points[i + 1]) - road.HalfWidth);
            return best;
        }

        /// <summary>A flattened spot: blends the ground to <see cref="Y"/> within <see cref="R"/>, back to the land over <see cref="Fall"/>.</summary>
        struct Pad
        {
            public Vector2 C;
            public float R, Fall, Y;
        }

        static Pad[] pads;

        static Pad[] Pads => pads ??= new[]
        {
            new Pad { C = HanPad, R = 18f, Fall = 10f, Y = 0f },
            new Pad { C = Gate, R = 9f, Fall = 6f, Y = 0f },
            new Pad { C = Hut, R = 12f, Fall = 8f, Y = FloorY(Hut.y) },
            new Pad { C = Spring, R = 10f, Fall = 8f, Y = FloorY(Spring.y) - 0.2f },
            new Pad { C = Camp, R = 18f, Fall = 20f, Y = FloorY(Camp.y) + CampRise },
            new Pad { C = Den, R = 13f, Fall = 10f, Y = FloorY(Den.y) - 1.6f },
            new Pad { C = NorthExit, R = 10f, Fall = 8f, Y = FloorY(NorthExit.y) },
            new Pad { C = Flat(BridgeCentre - BridgeAxis * 12.5f), R = 3.5f, Fall = 5f, Y = BridgeBase },
            new Pad { C = Flat(BridgeCentre + BridgeAxis * 12.5f), R = 3.5f, Fall = 5f, Y = BridgeBase },
        };

        /// <summary>Spots kept free of scattered trees and rocks.</summary>
        static (Vector2 c, float r)[] keepouts;

        static (Vector2 c, float r)[] Keepouts => keepouts ??= new[]
        {
            (HanPad, 24f), (Gate, 15f), (Hut, 15f), (new Vector2(46f, -134f), 12f), (Spring, 11f), (Camp, 22f), (Den, 17f), (Cave, 10f),
            (NorthExit, 14f), (Flat(BridgeCentre), 17f), (MeadowWestPack, 11f), (MeadowEastPack, 11f), (BoarEastHerd, 12f), (Flat(HanRoot) + new Vector2(0f, 14f), 10f),
        };

        static float Height(float x, float z)
        {
            var floor = FloorY(z);
            var river = RiverDistance(x, z);
            var calm = Smooth(RiverHalfWidth, RiverHalfWidth + 18f, river);
            var noise = ((Perlin(x / 55f, z / 55f) - 0.5f) * 2.6f + (Perlin(x / 16f + 40f, z / 16f + 40f) - 0.5f) * 0.7f) * calm;
            var h = floor + noise;

            var d = Outside(x, z);
            h += 2.5f * Smooth(-16f, 0f, d);
            if (d > 0f) h += 26f * Smooth(0f, 55f, d) + 34f * Smooth(10f, 70f, d) * Ridge(x, z);

            var road = RoadEdge(x, z);
            h = Mathf.Lerp(h, floor + noise * 0.25f - 0.12f, 0.85f * (1f - Smooth(0f, 4f, road)));

            foreach (var pad in Pads)
            {
                var w = 1f - Smooth(pad.R, pad.R + pad.Fall, Vector2.Distance(new Vector2(x, z), pad.C));
                if (w > 0f) h = Mathf.Lerp(h, pad.Y, w);
            }

            if (river < RiverHalfWidth + 4f)
            {
                var k = Mathf.Min(1f, river / RiverHalfWidth);
                var bed = WaterY(x) - 1.2f + 0.8f * k * k;
                h = Mathf.Lerp(bed, h, Smooth(RiverHalfWidth, RiverHalfWidth + 4f, river));
            }
            return h;
        }

        static float Ridge(float x, float z)
        {
            var r = 1f - Mathf.Abs(Perlin(x / 34f + 80f, z / 34f + 80f) * 2f - 1f);
            return r * r * (0.6f + 0.8f * Perlin(x / 90f + 7f, z / 90f + 7f));
        }

        // Terrain layers, in order.
        const int Meadow = 0, Forest = 1, Dirt = 2, Stone = 3;

        /// <summary>Layer weights at a point: meadow grass, forest floor (mossy south of the river, patches north), dirt roads and yards, rock on slopes and in the riverbed.</summary>
        static void Paint(float x, float z, float steepness, float[] w)
        {
            var jitter = (Perlin(x / 12f + 300f, z / 12f) - 0.5f) * 14f;
            var edge = ForestEdge(x);
            var forest = Smooth(edge - 5f, edge + 5f, z + jitter * 0.4f) * (1f - Smooth(RiverZ(x) - 7f, RiverZ(x) + 1f, z + jitter * 0.3f));
            var north = Smooth(RiverZ(x) + 4f, RiverZ(x) + 16f, z) * Smooth(0.5f, 0.68f, Perlin(x / 22f + 500f, z / 22f)) * 0.75f;
            var moss = Mathf.Max(forest, north);
            w[Meadow] = 1f - moss;
            w[Forest] = moss;
            w[Dirt] = w[Stone] = 0f;

            var p = new Vector2(x, z);
            Blend(w, Dirt, 1f - Smooth(13f, 19f, Vector2.Distance(p, Camp) + jitter * 0.3f));
            Blend(w, Dirt, 1f - Smooth(5f, 9f, Vector2.Distance(p, new Vector2(-16f, -182f)) + jitter * 0.2f));
            Blend(w, Dirt, 0.8f * (1f - Smooth(10f, 16f, Vector2.Distance(p, Den) + jitter * 0.3f)));
            Blend(w, Stone, 0.35f * (1f - Smooth(7f, 13f, Vector2.Distance(p, Den) + jitter * 0.3f)));
            Blend(w, Dirt, 0.7f * (1f - Smooth(3.5f, 6f, Vector2.Distance(p, Spring))));
            Blend(w, Dirt, 0.6f * (1f - Smooth(4f, 8f, Vector2.Distance(p, Hut) + jitter * 0.2f)));
            Blend(w, Dirt, 0.6f * (1f - Smooth(5f, 8f, Vector2.Distance(p, Gate))));
            Blend(w, Dirt, 1f - Smooth(-0.6f, 1f, RoadEdge(x, z) + (Perlin(x / 3f, z / 3f) - 0.5f) * 1.2f));

            var river = RiverDistance(x, z);
            Blend(w, Dirt, 0.5f * (1f - Smooth(RiverHalfWidth + 1.5f, RiverHalfWidth + 4f, river)));
            Blend(w, Stone, 0.75f * (1f - Smooth(RiverHalfWidth + 0.5f, RiverHalfWidth + 2f, river)));

            Blend(w, Stone, Smooth(26f, 38f, steepness));
            Blend(w, Stone, 0.85f * Smooth(2f, 16f, Outside(x, z) + (Perlin(x / 9f, z / 9f) - 0.5f) * 10f));

            var sum = w[0] + w[1] + w[2] + w[3];
            for (var i = 0; i < 4; i++) w[i] /= sum;
        }

        static void Blend(float[] w, int layer, float amount)
        {
            if (amount <= 0f) return;
            for (var i = 0; i < w.Length; i++) w[i] = Mathf.Lerp(w[i], i == layer ? 1f : 0f, amount);
        }

        // ---------------------------------------------------------------- build

        [MenuItem("Wuxia/World/Build Oak Valley")]
        public static string Build()
        {
            if (EditorApplication.isPlaying) return "Stop Play mode first.";
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.path.EndsWith("OakValley.unity")) return $"Open OakValley.unity first (active: {scene.path}).";

            EnsureLayer(ObstacleLayer, "Obstacle");
            foreach (var dir in new[] { ArtRoot, $"{ArtRoot}/Terrain", $"{ArtRoot}/Materials", $"{ArtRoot}/Meshes" })
                if (!AssetDatabase.IsValidFolder(dir))
                    AssetDatabase.CreateFolder(Path.GetDirectoryName(dir).Replace('\\', '/'), Path.GetFileName(dir));

            var environment = GameObject.Find("Environment").transform;
            var gameplay = GameObject.Find("Gameplay").transform;
            foreach (var old in new[] { "Ground", "Path", "PathToEastDen", "PathToWestDen", "Cliffs", "Trees", "Rocks", "Generated" })
            {
                var t = environment.Find(old);
                if (t != null) Object.DestroyImmediate(t.gameObject);
            }
            var generated = new GameObject("Generated").transform;
            generated.SetParent(environment, false);

            paletteMeshes.Clear();
            var materials = new Materials();
            var terrain = BuildTerrain(generated);
            var ctx = new Context { Root = generated, Terrain = terrain, M = materials, Rng = new Random(1207) };

            BuildWater(ctx);
            BuildBridge(ctx);
            BuildGate(ctx);
            BuildFold(ctx);
            BuildSpring(ctx);
            BuildCamp(ctx);
            BuildDen(ctx);
            BuildNorthExit(ctx);
            Scatter(ctx);
            BuildBounds(ctx);
            BuildLedges(ctx, environment.Find("LeapLedges"));
            PlaceAnchors(ctx, environment, gameplay);
            BuildAreas(ctx, gameplay);
            SetUpLighting(generated);

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            var count = generated.GetComponentsInChildren<Transform>(true).Length;
            return $"Oak Valley built: {count} generated objects ({ctx.Trees} trees, {ctx.Rocks} rocks). Rebake the NavMesh and save the scene.";
        }

        sealed class Context
        {
            public Transform Root;
            public Terrain Terrain;
            public Materials M;
            public Random Rng;
            public int Trees, Rocks;

            public float Ground(float x, float z) => Terrain.SampleHeight(new Vector3(x, 0f, z)) + Terrain.transform.position.y;
            public Vector3 OnGround(Vector2 p, float lift = 0f) => new Vector3(p.x, Ground(p.x, p.y) + lift, p.y);
            public float Range(float min, float max) => min + (float)Rng.NextDouble() * (max - min);

            public Transform Group(string name, Transform parent = null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent != null ? parent : Root, false);
                return go.transform;
            }
        }

        // ---------------------------------------------------------------- terrain

        static Terrain BuildTerrain(Transform parent)
        {
            var data = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
            if (data == null)
            {
                data = new TerrainData();
                AssetDatabase.CreateAsset(data, TerrainDataPath);
            }
            data.heightmapResolution = HeightResolution;
            data.size = TerrainSize;

            var heights = new float[HeightResolution, HeightResolution];
            for (var j = 0; j < HeightResolution; j++)
            for (var i = 0; i < HeightResolution; i++)
            {
                var x = TerrainOrigin.x + i / (float)(HeightResolution - 1) * TerrainSize.x;
                var z = TerrainOrigin.z + j / (float)(HeightResolution - 1) * TerrainSize.z;
                heights[j, i] = Mathf.Clamp01((Height(x, z) - TerrainOrigin.y) / TerrainSize.y);
            }
            data.SetHeights(0, 0, heights);

            data.terrainLayers = new[]
            {
                Layer("Meadow", GrassTexture, 6f),
                Layer("ForestFloor", ForestTexture, 7f),
                Layer("Dirt", DirtTexture, 5f),
                Layer("Rock", RockTexture, 10f),
            };
            data.alphamapResolution = AlphaResolution;
            data.baseMapResolution = 512;
            var alphas = new float[AlphaResolution, AlphaResolution, 4];
            var w = new float[4];
            for (var j = 0; j < AlphaResolution; j++)
            for (var i = 0; i < AlphaResolution; i++)
            {
                var u = (i + 0.5f) / AlphaResolution;
                var v = (j + 0.5f) / AlphaResolution;
                Paint(TerrainOrigin.x + u * TerrainSize.x, TerrainOrigin.z + v * TerrainSize.z, data.GetSteepness(u, v), w);
                for (var l = 0; l < 4; l++) alphas[j, i, l] = w[l];
            }
            data.SetAlphamaps(0, 0, alphas);
            EditorUtility.SetDirty(data);

            var go = Terrain.CreateTerrainGameObject(data);
            go.name = "Terrain";
            go.transform.SetParent(parent, false);
            go.transform.position = TerrainOrigin;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            var terrain = go.GetComponent<Terrain>();
            terrain.materialTemplate = TerrainMaterial();
            terrain.drawInstanced = true;
            terrain.heightmapPixelError = 12f;
            terrain.basemapDistance = 220f;
            terrain.shadowCastingMode = ShadowCastingMode.Off;
            terrain.drawTreesAndFoliage = false;
            terrain.Flush();
            return terrain;
        }

        static Material TerrainMaterial()
        {
            var path = $"{ArtRoot}/Terrain/OakValleyTerrain.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit")) { enableInstancing = true };
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static TerrainLayer Layer(string name, Func<int, Color32[]> texture, float tile)
        {
            var path = $"{ArtRoot}/Terrain/{name}.terrainlayer";
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (layer == null)
            {
                layer = new TerrainLayer();
                AssetDatabase.CreateAsset(layer, path);
            }
            layer.diffuseTexture = SaveTexture($"{ArtRoot}/Terrain/{name}.png", texture(TextureSize));
            layer.tileSize = new Vector2(tile, tile);
            layer.smoothness = 0f;
            layer.metallic = 0f;
            EditorUtility.SetDirty(layer);
            return layer;
        }

        /// <summary>Writes a PNG and imports it compressed for mobile (ASTC 6×6 on Android/iOS), like the model textures.</summary>
        static Texture2D SaveTexture(string path, Color32[] pixels)
        {
            var tex = new Texture2D(TextureSize, TextureSize, TextureFormat.RGB24, false);
            tex.SetPixels32(pixels);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 2;
            importer.maxTextureSize = TextureSize;
            importer.textureCompression = TextureImporterCompression.Compressed;
            foreach (var platform in new[] { "Android", "iPhone" })
                importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                {
                    name = platform, overridden = true, maxTextureSize = TextureSize, format = TextureImporterFormat.ASTC_6x6,
                });
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // ---------------------------------------------------------------- ground textures (tileable)

        static Color32[] GrassTexture(int size)
        {
            var px = Fill(size, (u, v) =>
            {
                var c = Color.Lerp(new Color(0.29f, 0.40f, 0.15f), new Color(0.50f, 0.60f, 0.25f), Fbm(u, v, 4, 4, 1));
                c = Color.Lerp(c, new Color(0.62f, 0.60f, 0.28f), Smooth(0.55f, 0.8f, Fbm(u, v, 3, 2, 3)) * 0.5f);
                return c * (0.86f + 0.26f * Fbm(u, v, 48, 2, 2));
            });
            // Wildflowers: white, yellow, pink, violet, orange specks.
            var rng = new Random(7);
            var colours = new[]
            {
                new Color(0.95f, 0.93f, 0.85f), new Color(0.95f, 0.93f, 0.85f), new Color(0.98f, 0.82f, 0.25f), new Color(0.98f, 0.82f, 0.25f),
                new Color(0.92f, 0.50f, 0.62f), new Color(0.62f, 0.50f, 0.92f), new Color(0.95f, 0.55f, 0.20f),
            };
            for (var f = 0; f < 520; f++)
                Dot(px, size, rng.Next(size), rng.Next(size), 1.7f, colours[rng.Next(colours.Length)], 1f);
            return px;
        }

        static Color32[] ForestTexture(int size)
        {
            return Fill(size, (u, v) =>
            {
                var c = Color.Lerp(new Color(0.17f, 0.24f, 0.10f), new Color(0.31f, 0.37f, 0.16f), Fbm(u, v, 4, 4, 5));
                c = Color.Lerp(c, new Color(0.36f, 0.27f, 0.15f), Smooth(0.55f, 0.68f, Fbm(u, v, 6, 3, 7)) * 0.8f);
                return c * (0.84f + 0.3f * Fbm(u, v, 48, 2, 8));
            });
        }

        static Color32[] DirtTexture(int size)
        {
            var px = Fill(size, (u, v) =>
            {
                var c = Color.Lerp(new Color(0.47f, 0.37f, 0.25f), new Color(0.64f, 0.52f, 0.36f), Fbm(u, v, 4, 4, 9));
                return c * (0.9f + 0.2f * Fbm(u, v, 64, 2, 10));
            });
            var rng = new Random(11);
            for (var p = 0; p < 420; p++)
            {
                var light = rng.NextDouble() < 0.6;
                Dot(px, size, rng.Next(size), rng.Next(size), 1.5f + (float)rng.NextDouble() * 2.5f,
                    light ? new Color(0.70f, 0.64f, 0.55f) : new Color(0.38f, 0.32f, 0.25f), 0.9f);
            }
            return px;
        }

        static Color32[] RockTexture(int size)
        {
            return Fill(size, (u, v) =>
            {
                var c = Color.Lerp(new Color(0.36f, 0.36f, 0.35f), new Color(0.60f, 0.60f, 0.58f), Fbm(u, v, 4, 5, 11));
                if (Mathf.Abs(Fbm(u, v, 8, 3, 12) - 0.5f) < 0.02f) c *= 0.62f;
                c = Color.Lerp(c, new Color(0.42f, 0.45f, 0.30f), Smooth(0.6f, 0.75f, Fbm(u, v, 5, 3, 14)) * 0.35f);
                return c * (0.9f + 0.18f * Fbm(u, v, 64, 1, 13));
            });
        }

        static Color32[] Fill(int size, Func<float, float, Color> colour)
        {
            var px = new Color32[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                px[y * size + x] = colour(x / (float)size, y / (float)size);
            return px;
        }

        /// <summary>A soft round speck, wrapping at the edges so the texture still tiles.</summary>
        static void Dot(Color32[] px, int size, int cx, int cy, float radius, Color colour, float strength)
        {
            var r = Mathf.CeilToInt(radius);
            for (var dy = -r; dy <= r; dy++)
            for (var dx = -r; dx <= r; dx++)
            {
                var d = Mathf.Sqrt(dx * dx + dy * dy) / radius;
                if (d > 1f) continue;
                var i = ((cy + dy + size) % size) * size + (cx + dx + size) % size;
                var shade = 1.08f - 0.25f * Mathf.Clamp01((dx + dy) / (2f * radius) + 0.5f);
                px[i] = Color.Lerp(px[i], colour * shade, strength * (1f - d * d * 0.6f));
            }
        }

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                var h = (uint)(x * 374761393 + y * 668265263 + seed * 144269);
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 0xffffff) / 16777215f;
            }
        }

        /// <summary>Value noise over a <paramref name="period"/>×<paramref name="period"/> lattice that wraps, so u,v in [0,1) tile.</summary>
        static float TileNoise(float u, float v, int period, int seed)
        {
            var x = u * period;
            var y = v * period;
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            int Wrap(int i) => (i % period + period) % period;
            var a = Hash(Wrap(x0), Wrap(y0), seed);
            var b = Hash(Wrap(x0 + 1), Wrap(y0), seed);
            var c = Hash(Wrap(x0), Wrap(y0 + 1), seed);
            var d = Hash(Wrap(x0 + 1), Wrap(y0 + 1), seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        static float Fbm(float u, float v, int period, int octaves, int seed)
        {
            float sum = 0f, total = 0f, amp = 0.5f;
            for (var o = 0; o < octaves; o++)
            {
                sum += amp * TileNoise(u, v, period << o, seed + o * 31);
                total += amp;
                amp *= 0.5f;
            }
            return sum / total;
        }

        // ---------------------------------------------------------------- materials and meshes

        /// <summary>Colour slots of the shared foliage material (see <see cref="PaletteMesh"/>).</summary>
        enum Palette { Bark, Leaves, LeavesDeep, Bush, Gorse, Rock, MossRock }

        static readonly Color[] PaletteColours =
        {
            new Color(0.33f, 0.25f, 0.18f), new Color(0.38f, 0.48f, 0.19f), new Color(0.21f, 0.32f, 0.13f), new Color(0.27f, 0.37f, 0.14f),
            new Color(0.55f, 0.52f, 0.14f), new Color(0.56f, 0.54f, 0.50f), new Color(0.37f, 0.43f, 0.27f),
        };

        static readonly Dictionary<string, Mesh> paletteMeshes = new Dictionary<string, Mesh>();

        /// <summary>
        /// One-submesh copy of <paramref name="source"/> whose submesh i takes its colour from palette slot
        /// <paramref name="slots"/>[i] (through the UVs), so it draws with the shared foliage material and
        /// merges with every other tree, bush and rock in its cell.
        /// </summary>
        static Mesh PaletteMesh(Mesh source, params Palette[] slots)
        {
            var name = source.name + "_" + string.Join("_", slots);
            if (paletteMeshes.TryGetValue(name, out var cached) && cached != null) return cached;
            var uvs = new Vector2[source.vertexCount];
            var triangles = new List<int>();
            for (var sub = 0; sub < source.subMeshCount; sub++)
            {
                var uv = new Vector2(((int)slots[Mathf.Min(sub, slots.Length - 1)] + 0.5f) / PaletteColours.Length, 0.5f);
                var tris = source.GetTriangles(sub);
                foreach (var i in tris) uvs[i] = uv;
                triangles.AddRange(tris);
            }
            var mesh = new Mesh { name = name, indexFormat = source.indexFormat };
            mesh.SetVertices(source.vertices);
            mesh.SetNormals(source.normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return paletteMeshes[name] = SaveMesh(mesh);
        }

        sealed class Materials
        {
            /// <summary>Trees, bushes and scattered rocks share this one material; their colours come from <see cref="Palette"/> through the UVs.</summary>
            public readonly Material Foliage = PaletteMaterial();
            public readonly Material Gorse = Mat("Gorse", PaletteColours[(int)Palette.Gorse]);
            public readonly Material Rock = Mat("Rock", new Color(0.56f, 0.54f, 0.50f));
            public readonly Material MossRock = Mat("MossRock", new Color(0.37f, 0.43f, 0.27f));
            public readonly Material Water = Mat("Water", new Color(0.12f, 0.25f, 0.27f), 0.75f);
            public readonly Material BridgeStone = Mat("BridgeStone", new Color(0.55f, 0.55f, 0.47f));
            public readonly Material TowerStone = Mat("TowerStone", new Color(0.60f, 0.58f, 0.53f));
            public readonly Material SpringStone = Mat("SpringStone", new Color(0.76f, 0.73f, 0.63f));
            public readonly Material DryEarth = Mat("DryEarth", new Color(0.62f, 0.52f, 0.38f));
            public readonly Material DeadPlant = Mat("DeadPlant", new Color(0.62f, 0.50f, 0.25f));
            public readonly Material Wood = Mat("Wood", new Color(0.45f, 0.32f, 0.20f));
            public readonly Material DarkWood = Mat("DarkWood", new Color(0.27f, 0.19f, 0.12f));
            public readonly Material Canvas = Mat("Canvas", new Color(0.78f, 0.72f, 0.58f));
            public readonly Material Thatch = Mat("Thatch", new Color(0.62f, 0.52f, 0.30f));
            public readonly Material Wool = Mat("Wool", new Color(0.90f, 0.87f, 0.80f));
            public readonly Material SheepFace = Mat("SheepFace", new Color(0.20f, 0.18f, 0.16f));
            public readonly Material Bone = Mat("Bone", new Color(0.86f, 0.83f, 0.73f));
            public readonly Material CaveDark = Mat("CaveDark", new Color(0.03f, 0.03f, 0.03f), 0f);
            public readonly Material Flame = Mat("Flame", new Color(1f, 0.55f, 0.15f), 0f, new Color(1f, 0.5f, 0.12f) * 3f);
            public readonly Material ShepherdCloth = Mat("ShepherdCloth", new Color(0.46f, 0.36f, 0.25f));
            public readonly Material HanStone = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Greybox/Materials/HanStone.mat");
            public readonly Material HanRoof = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Greybox/Materials/HanRoof.mat");
            public readonly Material LeapMarker = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Greybox/Materials/LeapMarker.mat");

            static Material PaletteMaterial()
            {
                const int block = 8;
                var count = PaletteColours.Length;
                var pixels = new Color32[count * block * block];
                for (var y = 0; y < block; y++)
                for (var x = 0; x < count * block; x++)
                    pixels[y * count * block + x] = PaletteColours[x / block];
                var path = $"{ArtRoot}/Materials/FoliagePalette.png";
                var tex = new Texture2D(count * block, block, TextureFormat.RGB24, false);
                tex.SetPixels32(pixels);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();

                // Wuxia/Foliage is URP Lit plus the camera occluder fade (CameraOccluderFade).
                var m = Mat("Foliage", Color.white, shader: "Wuxia/Foliage");
                m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(path));
                return m;
            }

            static Material Mat(string name, Color colour, float smoothness = 0.08f, Color? emission = null, string shader = "Universal Render Pipeline/Lit")
            {
                var path = $"{ArtRoot}/Materials/{name}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null)
                {
                    m = new Material(Shader.Find(shader));
                    AssetDatabase.CreateAsset(m, path);
                }
                else if (m.shader.name != shader) m.shader = Shader.Find(shader);
                m.SetColor("_BaseColor", colour);
                m.SetFloat("_Smoothness", smoothness);
                m.SetFloat("_Metallic", 0f);
                m.enableInstancing = true;
                if (emission.HasValue)
                {
                    var emissive = new LocalKeyword(m.shader, "_EMISSION");
                    m.SetKeyword(emissive, true);
                    m.SetColor("_EmissionColor", emission.Value);
                    // URP's material validation (inspector, reimport, URP upgrades) turns _EMISSION off
                    // unless the GI flags are emissive; there are no lightmaps, so this only keeps the glow.
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
                EditorUtility.SetDirty(m);
                return m;
            }
        }

        /// <summary>Flat-shaded low-poly mesh assembly; every face is wound so its normal points along an outward hint.</summary>
        sealed class MeshBuilder
        {
            readonly List<Vector3> vertices = new List<Vector3>();
            readonly List<Vector3> normals = new List<Vector3>();
            readonly List<Vector2> uvs = new List<Vector2>();
            readonly List<int>[] submeshes;

            public MeshBuilder(int submeshCount)
            {
                submeshes = new List<int>[submeshCount];
                for (var i = 0; i < submeshCount; i++) submeshes[i] = new List<int>();
            }

            public void Tri(int sub, Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
            {
                var cross = Vector3.Cross(b - a, c - a);
                if (cross.sqrMagnitude < 1e-10f) return;
                if (Vector3.Dot(cross, outward) < 0f)
                {
                    (b, c) = (c, b);
                    cross = -cross;
                }
                var n = cross.normalized;
                var i = vertices.Count;
                foreach (var p in new[] { a, b, c })
                {
                    vertices.Add(p);
                    normals.Add(n);
                    uvs.Add(Mathf.Abs(n.y) > 0.6f ? new Vector2(p.x, p.z) : Mathf.Abs(n.x) > Mathf.Abs(n.z) ? new Vector2(p.z, p.y) : new Vector2(p.x, p.y));
                }
                submeshes[sub].Add(i);
                submeshes[sub].Add(i + 1);
                submeshes[sub].Add(i + 2);
            }

            public void Quad(int sub, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward)
            {
                Tri(sub, a, b, c, outward);
                Tri(sub, a, c, d, outward);
            }

            public void Box(int sub, Vector3 centre, Vector3 size, Quaternion rotation)
            {
                var h = size * 0.5f;
                Vector3 P(float x, float y, float z) => centre + rotation * new Vector3(x * h.x, y * h.y, z * h.z);
                for (var axis = 0; axis < 3; axis++)
                for (var sign = -1; sign <= 1; sign += 2)
                {
                    var n = Vector3.zero;
                    n[axis] = sign;
                    var u = Vector3.zero;
                    u[(axis + 1) % 3] = 1f;
                    var v = Vector3.zero;
                    v[(axis + 2) % 3] = 1f;
                    Quad(sub, P(n.x - u.x - v.x, n.y - u.y - v.y, n.z - u.z - v.z), P(n.x + u.x - v.x, n.y + u.y - v.y, n.z + u.z - v.z),
                        P(n.x + u.x + v.x, n.y + u.y + v.y, n.z + u.z + v.z), P(n.x - u.x + v.x, n.y - u.y + v.y, n.z - u.z + v.z), rotation * n);
                }
            }

            public void Box(int sub, Vector3 centre, Vector3 size) => Box(sub, centre, size, Quaternion.identity);

            /// <summary>Tapered cylinder from <paramref name="from"/> to <paramref name="to"/>; a zero end radius makes a cone.</summary>
            public void Cylinder(int sub, Vector3 from, Vector3 to, float r0, float r1, int sides, bool capTop = true)
            {
                var axis = (to - from).normalized;
                var side = Vector3.Cross(axis, Mathf.Abs(axis.y) < 0.99f ? Vector3.up : Vector3.right).normalized;
                var fwd = Vector3.Cross(axis, side);
                Vector3 Dir(int i) => side * Mathf.Cos(i * Mathf.PI * 2f / sides) + fwd * Mathf.Sin(i * Mathf.PI * 2f / sides);
                for (var i = 0; i < sides; i++)
                {
                    var hint = Dir(i) + Dir(i + 1);
                    Quad(sub, from + Dir(i) * r0, from + Dir(i + 1) * r0, to + Dir(i + 1) * r1, to + Dir(i) * r1, hint);
                    if (capTop && r1 > 0f) Tri(sub, to, to + Dir(i) * r1, to + Dir(i + 1) * r1, axis);
                }
            }

            /// <summary>A lumpy flattened icosphere: canopy clusters, bushes and rocks. Coarse ones (20 faces instead of 80) are for distant LODs.</summary>
            public void Blob(int sub, Vector3 centre, Vector3 radii, float lumpiness, int seed, Quaternion rotation, bool coarse = false)
            {
                Vector3 P(Vector3 dir)
                {
                    var n = Mathf.PerlinNoise(dir.x * 1.7f + seed * 0.37f + 50f, dir.y * 1.7f + dir.z * 0.9f + 11f);
                    var f = 1f + lumpiness * (n - 0.5f) * 2f;
                    return centre + rotation * Vector3.Scale(radii, dir * f);
                }
                foreach (var t in coarse ? UnitIcosahedron : UnitIcosphere)
                {
                    Vector3 a = P(t[0]), b = P(t[1]), c = P(t[2]);
                    Tri(sub, a, b, c, (a + b + c) / 3f - centre);
                }
            }

            public void Blob(int sub, Vector3 centre, Vector3 radii, float lumpiness, int seed, bool coarse = false) =>
                Blob(sub, centre, radii, lumpiness, seed, Quaternion.identity, coarse);

            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name };
                if (vertices.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetUVs(0, uvs);
                mesh.subMeshCount = submeshes.Length;
                for (var i = 0; i < submeshes.Length; i++) mesh.SetTriangles(submeshes[i], i);
                mesh.RecalculateBounds();
                return SaveMesh(mesh);
            }
        }

        static List<Vector3[]> unitIcosphere, unitIcosahedron;

        /// <summary>Icosahedron subdivided once (80 faces), unit radius.</summary>
        static List<Vector3[]> UnitIcosphere
        {
            get
            {
                BuildIcospheres();
                return unitIcosphere;
            }
        }

        /// <summary>Plain icosahedron (20 faces), unit radius.</summary>
        static List<Vector3[]> UnitIcosahedron
        {
            get
            {
                BuildIcospheres();
                return unitIcosahedron;
            }
        }

        static void BuildIcospheres()
        {
            {
                if (unitIcosphere != null) return;
                var t = (1f + Mathf.Sqrt(5f)) / 2f;
                var v = new[]
                {
                    new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                    new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                    new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1),
                };
                for (var i = 0; i < v.Length; i++) v[i] = v[i].normalized;
                int[] f =
                {
                    0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                    3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
                };
                unitIcosphere = new List<Vector3[]>();
                unitIcosahedron = new List<Vector3[]>();
                for (var i = 0; i < f.Length; i += 3)
                {
                    Vector3 a = v[f[i]], b = v[f[i + 1]], c = v[f[i + 2]];
                    unitIcosahedron.Add(new[] { a, b, c });
                    Vector3 ab = ((a + b) / 2f).normalized, bc = ((b + c) / 2f).normalized, ca = ((c + a) / 2f).normalized;
                    unitIcosphere.Add(new[] { a, ab, ca });
                    unitIcosphere.Add(new[] { b, bc, ab });
                    unitIcosphere.Add(new[] { c, ca, bc });
                    unitIcosphere.Add(new[] { ab, bc, ca });
                }
            }
        }

        /// <summary>Saves into Meshes/, reusing an existing asset so scene references survive a rebuild.</summary>
        static Mesh SaveMesh(Mesh mesh)
        {
            var path = $"{ArtRoot}/Meshes/{mesh.name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }
            EditorUtility.CopySerialized(mesh, existing);
            Object.DestroyImmediate(mesh);
            return existing;
        }

        static GameObject Place(Transform parent, string name, Mesh mesh, Material[] materials, Vector3 position, Quaternion rotation,
            float scale = 1f, bool castShadows = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.localScale = Vector3.one * scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            return go;
        }

        static void Collide(GameObject go, bool convex)
        {
            var collider = go.AddComponent<MeshCollider>();
            collider.sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
            collider.convex = convex;
        }

        /// <summary>Hides a small prop once it's below <paramref name="screenHeight"/> of the screen (bushes, pebbles, crates).</summary>
        static void CullWhenSmall(GameObject go, float screenHeight = 0.025f)
        {
            var lod = go.AddComponent<LODGroup>();
            lod.SetLODs(new[] { new LOD(screenHeight, new[] { go.GetComponent<Renderer>() }) });
            lod.RecalculateBounds();
        }

        static BoxCollider Solid(GameObject go, Vector3 centre, Vector3 size, int layer)
        {
            var holder = go;
            if (layer != go.layer)
            {
                holder = new GameObject("Collider") { layer = layer };
                holder.transform.SetParent(go.transform, false);
            }
            var box = holder.AddComponent<BoxCollider>();
            box.center = centre;
            box.size = size;
            return box;
        }

        /// <summary>
        /// A low-poly oak: stout trunk, a few branches and a broad crown of lumpy clusters that hangs low
        /// around the trunk; plus its distant LOD (same crown in coarse blobs, no branches, ~1/4 the triangles).
        /// </summary>
        /// <param name="hang">How far the lowest crown clusters droop below the crown base (share of the crown height). Forest oaks keep theirs high so the follow camera (~5 m up) stays under the canopy.</param>
        static Mesh[] OakMesh(string name, int seed, float trunkHeight, float trunkRadius, float crownWidth, float crownHeight, float lean, int clusters, float hang = 0.12f)
        {
            var meshes = new Mesh[2];
            for (var lod = 0; lod < 2; lod++)
            {
                var coarse = lod > 0;
                var rng = new Random(seed);
                float Rnd(float min, float max) => min + (float)rng.NextDouble() * (max - min);
                var mb = new MeshBuilder(2);
                var top = new Vector3(lean, trunkHeight, lean * 0.4f);
                mb.Cylinder(0, new Vector3(0f, -0.6f, 0f), top, trunkRadius, trunkRadius * 0.62f, coarse ? 5 : 7);
                if (!coarse) mb.Cylinder(0, new Vector3(0f, -0.4f, 0f), new Vector3(lean * 0.15f, 0.9f, 0f), trunkRadius * 1.55f, trunkRadius * 0.85f, 7, false);
                var crown = top + Vector3.up * 0.6f;
                mb.Blob(1, crown + Vector3.up * crownHeight * 0.35f, new Vector3(crownWidth * 0.36f, crownHeight * 0.42f, crownWidth * 0.36f), 0.16f, seed, coarse);
                mb.Blob(1, crown + Vector3.up * crownHeight * 0.68f, new Vector3(crownWidth * 0.24f, crownHeight * 0.26f, crownWidth * 0.24f), 0.2f, seed + 3, coarse);
                var from = Vector3.Lerp(new Vector3(0f, trunkHeight * 0.6f, 0f), top, 0.5f);
                for (var i = 0; i < clusters; i++)
                {
                    var a = i / (float)clusters * Mathf.PI * 2f + Rnd(0f, 0.7f);
                    var dist = crownWidth * Rnd(0.24f, 0.32f);
                    var c = crown + new Vector3(Mathf.Cos(a) * dist, crownHeight * Rnd(-hang, 0.22f), Mathf.Sin(a) * dist);
                    var r = crownWidth * Rnd(0.2f, 0.27f);
                    mb.Blob(1, c, new Vector3(r, r * 0.88f, r) * (coarse ? 1.08f : 1f), 0.2f, seed + i * 7, coarse);
                    if (!coarse) mb.Cylinder(0, from, c - (c - from).normalized * r * 0.7f, trunkRadius * 0.42f, trunkRadius * 0.2f, 5, false);
                }
                meshes[lod] = mb.ToMesh(coarse ? name + "_LOD1" : name);
            }
            return meshes;
        }

        static Mesh BlobMesh(string name, int seed, Vector3 radii, float lumpiness, int lumps = 1, float spread = 0f)
        {
            var rng = new Random(seed);
            var mb = new MeshBuilder(1);
            for (var i = 0; i < lumps; i++)
            {
                var offset = i == 0 ? Vector3.zero : new Vector3(((float)rng.NextDouble() - 0.5f) * spread, 0f, ((float)rng.NextDouble() - 0.5f) * spread);
                var s = i == 0 ? 1f : 0.6f + 0.3f * (float)rng.NextDouble();
                mb.Blob(0, offset + Vector3.up * radii.y * 0.4f * s, radii * s, lumpiness, seed + i * 13);
            }
            return mb.ToMesh(name);
        }

        // ---------------------------------------------------------------- river, bridge, gate

        static void BuildWater(Context ctx)
        {
            var mb = new MeshBuilder(1);
            const float step = 2f, half = RiverHalfWidth + 1.4f;
            for (var x = TerrainOrigin.x - step; x < TerrainOrigin.x + TerrainSize.x + step; x += step)
            {
                Vector3 Edge(float px, float side)
                {
                    var s = RiverSlope(px);
                    var n = new Vector3(-s, 0f, 1f).normalized;
                    return new Vector3(px, WaterY(px), RiverZ(px)) + n * side;
                }
                mb.Quad(0, Edge(x, -half), Edge(x + step, -half), Edge(x + step, half), Edge(x, half), Vector3.up);
            }
            var go = Place(ctx.Root, "River", mb.ToMesh("River"), new[] { ctx.M.Water }, Vector3.zero, Quaternion.identity, 1f, false);
            go.layer = WaterLayer;
        }

        /// <summary>
        /// A humpback stone bridge with one arch, square to the river. Built along local +Z: the deck rises
        /// <see cref="BridgeRise"/> over its length, the arch opens over the channel, parapets keep you on it.
        /// </summary>
        static void BuildBridge(Context ctx)
        {
            var b = BridgeCentre;
            var water = WaterY(BridgeX);
            const float halfLength = BridgeLength * 0.5f, halfWidth = BridgeWidth * 0.5f;
            float Deck(float t) => b.y + BridgeRise * (0.5f + 0.5f * Mathf.Cos(Mathf.PI * t / halfLength));
            float Under(float t) => Mathf.Abs(t) <= ArchRadius
                ? water - 0.5f + 2f * Mathf.Sqrt(Mathf.Max(0f, 1f - t / ArchRadius * (t / ArchRadius)))
                : water - 2.5f;

            var mb = new MeshBuilder(1);
            void Solid(float x0, float x1, Func<float, float> topOf, Func<float, float> bottomOf)
            {
                const float step = 0.5f;
                for (var t = -halfLength; t < halfLength - 0.001f; t += step)
                {
                    var t1 = t + step;
                    float top0 = topOf(t), top1 = topOf(t1), bot0 = bottomOf(t), bot1 = bottomOf(t1);
                    mb.Quad(0, new Vector3(x0, bot0, t), new Vector3(x0, top0, t), new Vector3(x0, top1, t1), new Vector3(x0, bot1, t1), Vector3.left);
                    mb.Quad(0, new Vector3(x1, bot0, t), new Vector3(x1, top0, t), new Vector3(x1, top1, t1), new Vector3(x1, bot1, t1), Vector3.right);
                    mb.Quad(0, new Vector3(x0, top0, t), new Vector3(x1, top0, t), new Vector3(x1, top1, t1), new Vector3(x0, top1, t1), Vector3.up);
                    mb.Quad(0, new Vector3(x0, bot0, t), new Vector3(x1, bot0, t), new Vector3(x1, bot1, t1), new Vector3(x0, bot1, t1), Vector3.down);
                }
                foreach (var end in new[] { -halfLength, halfLength })
                    mb.Quad(0, new Vector3(x0, bottomOf(end), end), new Vector3(x1, bottomOf(end), end), new Vector3(x1, topOf(end), end),
                        new Vector3(x0, topOf(end), end), new Vector3(0f, 0f, Mathf.Sign(end)));
            }
            Solid(-halfWidth, halfWidth, Deck, Under);
            Solid(-halfWidth, -halfWidth + 0.4f, t => Deck(t) + 0.8f, t => Deck(t) - 0.05f);
            Solid(halfWidth - 0.4f, halfWidth, t => Deck(t) + 0.8f, t => Deck(t) - 0.05f);
            foreach (var x in new[] { -halfWidth + 0.2f, halfWidth - 0.2f })
            foreach (var t in new[] { -halfLength + 0.3f, halfLength - 0.3f })
                mb.Box(0, new Vector3(x, Deck(t) + 0.55f, t), new Vector3(0.65f, 1.1f, 0.65f));

            var go = Place(ctx.Root, "Stone Bridge", mb.ToMesh("StoneBridge"), new[] { ctx.M.BridgeStone },
                new Vector3(b.x, 0f, b.z), Quaternion.LookRotation(BridgeAxis));
            Collide(go, false);
            BuildRiverWalls(ctx);
        }

        /// <summary>
        /// Invisible walls along both banks, open only where the bridge crosses, so the river is crossed
        /// by the bridge alone (the monsters' NavMesh is cut the same way).
        /// </summary>
        static void BuildRiverWalls(Context ctx)
        {
            var group = ctx.Group("River Walls");
            var b = BridgeCentre;
            var across = new Vector3(BridgeAxis.z, 0f, -BridgeAxis.x);
            const float step = 0.25f;
            foreach (var side in new[] { -1f, 1f })
            {
                var run = new List<Vector3>();
                void Flush()
                {
                    for (var i = 0; i + 1 < run.Count; i += 16)
                    {
                        var a = run[i];
                        var e = run[Mathf.Min(i + 16, run.Count - 1)];
                        Wall(group, a, e, 6f, ctx);
                    }
                    run.Clear();
                }
                for (var x = TerrainOrigin.x - 2f; x <= TerrainOrigin.x + TerrainSize.x + 2f; x += step)
                {
                    var s = RiverSlope(x);
                    var n = new Vector3(-s, 0f, 1f).normalized;
                    var p = new Vector3(x, 0f, RiverZ(x)) + n * side * (RiverHalfWidth + 1.8f);
                    var off = p - new Vector3(b.x, 0f, b.z);
                    var onBridge = Mathf.Abs(Vector3.Dot(off, across)) < BridgeWidth * 0.5f && Mathf.Abs(Vector3.Dot(off, BridgeAxis)) < BridgeLength * 0.5f;
                    if (onBridge) Flush();
                    else run.Add(p);
                }
                Flush();
            }
        }

        /// <summary>An invisible box wall from <paramref name="a"/> to <paramref name="b"/> on the ground, on the Obstacle layer.</summary>
        static void Wall(Transform parent, Vector3 a, Vector3 b, float height, Context ctx)
        {
            var dir = b - a;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;
            var mid = (a + b) * 0.5f;
            var go = new GameObject("Wall") { layer = ObstacleLayer };
            go.transform.SetParent(parent, false);
            var ground = Mathf.Min(ctx.Ground(a.x, a.z), ctx.Ground(b.x, b.z), ctx.Ground(mid.x, mid.z));
            go.transform.SetPositionAndRotation(new Vector3(mid.x, ground + height * 0.5f - 1.5f, mid.z), Quaternion.LookRotation(dir));
            go.AddComponent<BoxCollider>().size = new Vector3(0.8f, height + 3f, dir.magnitude + 0.3f);
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        }

        /// <summary>The south gate: the road home to Dardana, closed for now.</summary>
        static void BuildGate(Context ctx)
        {
            var mb = new MeshBuilder(3);
            foreach (var s in new[] { -1f, 1f })
            {
                mb.Box(0, new Vector3(s * 6.6f, 3f, 0f), new Vector3(1.8f, 8f, 1.8f));
                mb.Box(0, new Vector3(s * 15f, 1.4f, 0.3f), new Vector3(15f, 6.8f, 1.6f));
                mb.Box(1, new Vector3(s * 15f, 4.95f, 0.3f), new Vector3(15.4f, 0.3f, 2.1f));
            }
            mb.Box(0, new Vector3(0f, 6.6f, 0f), new Vector3(15.4f, 1.2f, 2f));
            mb.Box(1, new Vector3(0f, 7.5f, 0f), new Vector3(16.6f, 0.7f, 3f));
            mb.Box(1, new Vector3(0f, 8.1f, 0f), new Vector3(12f, 0.5f, 1.6f));
            mb.Box(2, new Vector3(0f, 2.8f, -0.25f), new Vector3(11.4f, 5.6f, 0.4f));
            mb.Box(2, new Vector3(0f, 2.8f, 0f), new Vector3(0.15f, 5.6f, 0.2f));
            var go = Place(ctx.Root, "South Gate", mb.ToMesh("SouthGate"), new[] { ctx.M.HanStone, ctx.M.HanRoof, ctx.M.DarkWood },
                ctx.OnGround(Gate, -0.1f), Quaternion.identity);
            Collide(go, false);
        }

        // ---------------------------------------------------------------- landmarks

        /// <summary>The shepherd's hut, a small pen and the flock (lv 1–4 meadow).</summary>
        static void BuildFold(Context ctx)
        {
            var root = ctx.Group("Shepherd's Fold");

            var hut = new MeshBuilder(3);
            hut.Box(0, new Vector3(0f, 1.1f, 0f), new Vector3(5f, 3f, 4f));
            Prism(hut, 1, new Vector3(0f, 2.6f, 0f), 5.8f, 4.8f, 1.9f);
            hut.Box(2, new Vector3(0f, 1f, 2.02f), new Vector3(1.1f, 2f, 0.08f));
            var hutGo = Place(root, "Hut", hut.ToMesh("ShepherdHut"), new[] { ctx.M.HanStone, ctx.M.Thatch, ctx.M.DarkWood },
                ctx.OnGround(Hut, -0.3f), Quaternion.LookRotation(Vector3.left));
            Solid(hutGo, new Vector3(0f, 1.3f, 0f), new Vector3(5f, 3.4f, 4f), 0);

            // Pen south of the hut, open on its west side.
            var pen = new Vector2(Hut.x, Hut.y - 9f);
            var fence = new MeshBuilder(1);
            var penGo = new GameObject("Pen");
            penGo.transform.SetParent(root, false);
            penGo.transform.position = ctx.OnGround(pen);
            const float hx = 4.5f, hz = 3.5f;
            var corners = new[] { new Vector3(-hx, 0f, -hz), new Vector3(hx, 0f, -hz), new Vector3(hx, 0f, hz), new Vector3(-hx, 0f, hz), new Vector3(-hx, 0f, -hz) };
            for (var i = 0; i < 4; i++)
            {
                var a = corners[i];
                var e = corners[i + 1];
                var west = i == 3;
                var len = Vector3.Distance(a, e);
                var posts = Mathf.CeilToInt(len / 1.8f);
                for (var k = 0; k <= posts; k++)
                {
                    var p = Vector3.Lerp(a, e, k / (float)posts);
                    if (west && Mathf.Abs(p.z) < 1.2f) continue;
                    fence.Box(0, p + Vector3.up * 0.55f, new Vector3(0.14f, 1.3f, 0.14f));
                }
                var dir = (e - a).normalized;
                foreach (var y in new[] { 0.45f, 0.95f })
                {
                    if (west)
                    {
                        fence.Box(0, Vector3.Lerp(a, e, 0.2f) + Vector3.up * y, new Vector3(0.08f, 0.1f, len * 0.36f), Quaternion.LookRotation(dir));
                        fence.Box(0, Vector3.Lerp(a, e, 0.8f) + Vector3.up * y, new Vector3(0.08f, 0.1f, len * 0.36f), Quaternion.LookRotation(dir));
                    }
                    else fence.Box(0, (a + e) * 0.5f + Vector3.up * y, new Vector3(0.08f, 0.1f, len), Quaternion.LookRotation(dir));
                }
                if (west)
                {
                    Solid(penGo, Vector3.Lerp(a, e, 0.2f) + Vector3.up * 0.6f, Abs(Quaternion.LookRotation(dir) * new Vector3(0.3f, 1.4f, len * 0.36f)), ObstacleLayer);
                    Solid(penGo, Vector3.Lerp(a, e, 0.8f) + Vector3.up * 0.6f, Abs(Quaternion.LookRotation(dir) * new Vector3(0.3f, 1.4f, len * 0.36f)), ObstacleLayer);
                }
                else Solid(penGo, (a + e) * 0.5f + Vector3.up * 0.6f, Abs(Quaternion.LookRotation(dir) * new Vector3(0.3f, 1.4f, len)), ObstacleLayer);
            }
            penGo.AddComponent<MeshFilter>().sharedMesh = fence.ToMesh("PenFence");
            penGo.AddComponent<MeshRenderer>().sharedMaterial = ctx.M.Wood;
            GameObjectUtility.SetStaticEditorFlags(penGo, StaticEditorFlags.BatchingStatic);

            var sheep = SheepMesh();
            var spots = new List<Vector2>();
            for (var i = 0; i < 4; i++) spots.Add(pen + new Vector2(ctx.Range(-3f, 3f), ctx.Range(-2.4f, 2.4f)));
            for (var tries = 0; spots.Count < 13 && tries < 200; tries++)
            {
                var p = new Vector2(44f, -130f) + new Vector2(ctx.Range(-14f, 14f), ctx.Range(-12f, 12f));
                if (RoadEdge(p.x, p.y) < 2.5f || Vector2.Distance(p, Hut) < 5f || Mathf.Abs(p.x - pen.x) < hx + 1.5f && Mathf.Abs(p.y - pen.y) < hz + 1.5f) continue;
                if (spots.Exists(o => Vector2.Distance(o, p) < 2f)) continue;
                spots.Add(p);
            }
            for (var i = 0; i < spots.Count; i++)
            {
                var go = Place(root, $"Sheep {i + 1}", sheep, new[] { ctx.M.Wool, ctx.M.SheepFace }, ctx.OnGround(spots[i]),
                    Quaternion.Euler(0f, ctx.Range(0f, 360f), 0f), ctx.Range(0.9f, 1.1f));
                Solid(go, new Vector3(0f, 0.55f, 0f), new Vector3(0.7f, 0.9f, 1.3f), ObstacleLayer);
                CullWhenSmall(go, 0.015f);
            }
        }

        static Mesh SheepMesh()
        {
            var mb = new MeshBuilder(2);
            mb.Blob(0, new Vector3(0f, 0.62f, 0f), new Vector3(0.42f, 0.38f, 0.62f), 0.22f, 3);
            mb.Blob(1, new Vector3(0f, 0.78f, 0.62f), new Vector3(0.15f, 0.17f, 0.22f), 0.1f, 5);
            mb.Blob(0, new Vector3(0f, 0.92f, 0.52f), new Vector3(0.17f, 0.1f, 0.14f), 0.2f, 6);
            foreach (var x in new[] { -0.2f, 0.2f })
            foreach (var z in new[] { -0.38f, 0.36f })
                mb.Box(1, new Vector3(x, 0.2f, z), new Vector3(0.09f, 0.42f, 0.09f));
            return mb.ToMesh("Sheep");
        }

        /// <summary>A roof or tent: triangular prism along local X.</summary>
        static void Prism(MeshBuilder mb, int sub, Vector3 baseCentre, float length, float width, float height)
        {
            float hl = length * 0.5f, hw = width * 0.5f;
            var c = baseCentre;
            Vector3 L0 = c + new Vector3(-hl, 0f, -hw), L1 = c + new Vector3(-hl, 0f, hw), LT = c + new Vector3(-hl, height, 0f);
            Vector3 R0 = c + new Vector3(hl, 0f, -hw), R1 = c + new Vector3(hl, 0f, hw), RT = c + new Vector3(hl, height, 0f);
            mb.Tri(sub, L0, L1, LT, Vector3.left);
            mb.Tri(sub, R0, R1, RT, Vector3.right);
            mb.Quad(sub, L0, R0, RT, LT, new Vector3(0f, hw, -height));
            mb.Quad(sub, L1, R1, RT, LT, new Vector3(0f, hw, height));
        }

        /// <summary>The dried spring: a carved stone basin, cracked dry inside, in a forest clearing (lv 5–8).</summary>
        static void BuildSpring(Context ctx)
        {
            var root = ctx.Group("Dried Spring");
            var mb = new MeshBuilder(2);
            const int segments = 24;
            const float outer = 2.9f, inner = 2.4f, height = 0.75f;
            for (var i = 0; i < segments; i++)
            {
                float a0 = i * Mathf.PI * 2f / segments, a1 = (i + 1) * Mathf.PI * 2f / segments;
                Vector3 D(float a) => new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                var mid = D((a0 + a1) * 0.5f);
                Vector3 o0 = D(a0) * outer, o1 = D(a1) * outer, i0 = D(a0) * inner, i1 = D(a1) * inner;
                var up = Vector3.up * height;
                var down = Vector3.down * 0.3f;
                mb.Quad(0, o0 + down, o1 + down, o1 + up, o0 + up, mid);
                mb.Quad(0, i0 + down, i1 + down, i1 + up, i0 + up, -mid);
                mb.Quad(0, o0 + up, o1 + up, i1 + up, i0 + up, Vector3.up);
                // Carved band: a slightly proud ring of every other stone.
                if (i % 2 == 0) mb.Quad(0, o0 * 1.03f + Vector3.up * 0.25f, o1 * 1.03f + Vector3.up * 0.25f, o1 * 1.03f + Vector3.up * 0.45f, o0 * 1.03f + Vector3.up * 0.45f, mid);
            }
            mb.Cylinder(1, Vector3.down * 0.2f, Vector3.up * 0.18f, inner, inner, segments);
            var basin = Place(root, "Basin", mb.ToMesh("SpringBasin"), new[] { ctx.M.SpringStone, ctx.M.DryEarth }, ctx.OnGround(Spring), Quaternion.identity);
            Collide(basin, false);

            var stalks = new MeshBuilder(1);
            for (var i = 0; i < 9; i++)
            {
                var a = ctx.Range(0f, Mathf.PI * 2f);
                var r = ctx.Range(3.1f, 4.1f);
                var p = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                var tip = p + new Vector3(ctx.Range(-0.2f, 0.2f), ctx.Range(0.6f, 1.1f), ctx.Range(-0.2f, 0.2f));
                stalks.Cylinder(0, p, tip, 0.03f, 0.015f, 4, false);
                stalks.Blob(0, tip, new Vector3(0.09f, 0.06f, 0.09f), 0.3f, i);
            }
            Place(root, "Dead Plants", stalks.ToMesh("DeadPlants"), new[] { ctx.M.DeadPlant }, ctx.OnGround(Spring), Quaternion.identity, 1f, false);

            var boulder = BlobMesh("MossBoulder", 21, new Vector3(1.4f, 1.1f, 1.7f), 0.3f, 2, 1.6f);
            for (var i = 0; i < 5; i++)
            {
                var a = (i * 72f + ctx.Range(-15f, 15f)) * Mathf.Deg2Rad;
                var p = Spring + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * ctx.Range(6.5f, 8.5f);
                if (RoadEdge(p.x, p.y) < 2f) continue;
                var go = Place(root, "Boulder", boulder, new[] { ctx.M.MossRock }, ctx.OnGround(p, -0.4f), Quaternion.Euler(0f, ctx.Range(0f, 360f), 0f), ctx.Range(0.9f, 1.5f));
                go.layer = ObstacleLayer;
                Collide(go, true);
            }
            var gorse = BlobMesh("GorseBush", 31, new Vector3(0.8f, 0.6f, 0.8f), 0.35f, 3, 1.4f);
            for (var i = 0; i < 8; i++)
            {
                var a = ctx.Range(0f, Mathf.PI * 2f);
                var p = Spring + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * ctx.Range(4.5f, 7.5f);
                if (RoadEdge(p.x, p.y) < 1.5f) continue;
                CullWhenSmall(Place(root, "Gorse", gorse, new[] { ctx.M.Gorse }, ctx.OnGround(p, -0.1f), Quaternion.Euler(0f, ctx.Range(0f, 360f), 0f), ctx.Range(0.8f, 1.2f), false));
            }
        }

        /// <summary>
        /// The bandit camp (lv 8–11) on a hill against the west slopes: a ruined tower with scaffolding,
        /// a palisade open towards the road, tents, crates and a campfire.
        /// </summary>
        static void BuildCamp(Context ctx)
        {
            var root = ctx.Group("Bandit Camp");
            var centre = ctx.OnGround(Camp);

            // Tower: door faces the camp centre.
            var toCamp = (new Vector3(Camp.x, 0f, Camp.y) - new Vector3(Tower.x, 0f, Tower.y)).normalized;
            var towerRot = Quaternion.LookRotation(Vector3.Cross(toCamp, Vector3.up));
            var tower = new MeshBuilder(3);
            tower.Box(0, new Vector3(0f, 4.3f, 0f), new Vector3(6f, 9.6f, 6f));
            // Ragged crown: merlons along the top, some fallen, a taller broken wall on one corner.
            var crown = new[] { 1.1f, 0f, 1.3f, 0.9f, 1.2f, 0.7f, 0f, 1f, 0.5f, 1.2f, 0f, 0f, 1.4f, 0.8f, 1.1f, 0f };
            for (var side = 0; side < 4; side++)
            for (var k = 0; k < 4; k++)
            {
                var h = crown[side * 4 + k];
                if (h <= 0f) continue;
                var along = -2.25f + k * 1.5f;
                var rot = Quaternion.Euler(0f, side * 90f, 0f);
                tower.Box(0, rot * new Vector3(along, 9.1f + h * 0.5f, 2.7f), Abs(rot * new Vector3(1.1f, h, 0.6f)));
            }
            tower.Box(0, new Vector3(-1.6f, 11f, 2.7f), new Vector3(2.8f, 3.2f, 0.6f));
            tower.Box(0, new Vector3(-2.7f, 10.6f, 1.2f), new Vector3(0.6f, 2.4f, 3f));
            tower.Box(2, new Vector3(0f, 6.6f, 3.01f), new Vector3(0.8f, 1.3f, 0.05f));
            tower.Box(2, new Vector3(-3.01f, 7.2f, 0.6f), new Vector3(0.05f, 1.2f, 0.7f));
            tower.Box(2, new Vector3(0.8f, 3.6f, -3.01f), new Vector3(0.7f, 1.1f, 0.05f));
            tower.Box(2, new Vector3(3.01f, 1.1f, 0f), new Vector3(0.05f, 2.2f, 1.3f));
            tower.Box(1, new Vector3(3.6f, 2.5f, 0f), new Vector3(1.4f, 0.08f, 2.2f), Quaternion.Euler(0f, 0f, -15f));
            var towerPos = ctx.OnGround(Tower, -0.4f);
            var towerGo = Place(root, "Ruined Tower", tower.ToMesh("RuinedTower"), new[] { ctx.M.TowerStone, ctx.M.Canvas, ctx.M.CaveDark }, towerPos, towerRot);
            Solid(towerGo, new Vector3(0f, 4.3f, 0f), new Vector3(6f, 9.6f, 6f), 0);

            // Scaffolding against the south face; its platform is a leap ledge (see BuildLedges).
            var scaffold = new MeshBuilder(1);
            foreach (var x in new[] { -2.1f, 2.1f })
            foreach (var z in new[] { -3.2f, -5.4f })
                scaffold.Box(0, new Vector3(x, 2.9f, z), new Vector3(0.18f, 6.2f, 0.18f));
            scaffold.Box(0, new Vector3(0f, 5.65f, -5.4f), new Vector3(4.4f, 0.12f, 0.12f));
            foreach (var x in new[] { -2.1f, 2.1f })
                scaffold.Box(0, new Vector3(x, 2.4f, -4.3f), new Vector3(0.1f, 0.12f, 4.8f), Quaternion.Euler(-60f, 0f, 0f));
            scaffold.Box(0, new Vector3(0f, 2.3f, -5.4f), new Vector3(0.1f, 0.12f, 5.2f), Quaternion.Euler(0f, 90f, 55f));
            Place(root, "Scaffolding", scaffold.ToMesh("TowerScaffolding"), new[] { ctx.M.DarkWood }, towerPos, towerRot);

            // Palisade of sharpened stakes, open towards the road (east).
            var stakes = new MeshBuilder(1);
            var palisade = new GameObject("Palisade");
            palisade.transform.SetParent(root, false);
            palisade.transform.position = new Vector3(Camp.x, 0f, Camp.y);
            const float gap = 24f;
            var chunkStart = Vector3.zero;
            var chunkCount = 0;
            for (var deg = gap; deg <= 360f - gap; deg += 1.8f)
            {
                var a = deg * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                var p = dir * (PalisadeRadius + ctx.Range(-0.15f, 0.15f));
                var y = ctx.Ground(Camp.x + p.x, Camp.y + p.z) - 0.4f;
                var height = ctx.Range(2.6f, 3.3f);
                var top = p + dir * 0.25f + Vector3.up * (y + height);
                stakes.Cylinder(0, p + Vector3.up * y, top, 0.17f, 0.15f, 6, false);
                stakes.Cylinder(0, top, top + Vector3.up * 0.4f + dir * 0.04f, 0.15f, 0f, 6, false);
                if (chunkCount == 0) chunkStart = new Vector3(p.x, y, p.z);
                if (++chunkCount == 6 || deg + 1.8f > 360f - gap)
                {
                    var end = new Vector3(p.x, y, p.z);
                    var mid = (chunkStart + end) * 0.5f;
                    var wall = new GameObject("Wall") { layer = ObstacleLayer };
                    wall.transform.SetParent(palisade.transform, false);
                    wall.transform.localPosition = mid + Vector3.up * 1.6f;
                    wall.transform.localRotation = Quaternion.LookRotation(end - chunkStart == Vector3.zero ? Vector3.forward : Flat3(end - chunkStart));
                    wall.AddComponent<BoxCollider>().size = new Vector3(0.6f, 3.6f, Vector3.Distance(chunkStart, end) + 0.6f);
                    chunkCount = 0;
                }
            }
            palisade.AddComponent<MeshFilter>().sharedMesh = stakes.ToMesh("Palisade");
            palisade.AddComponent<MeshRenderer>().sharedMaterial = ctx.M.Wood;
            GameObjectUtility.SetStaticEditorFlags(palisade, StaticEditorFlags.BatchingStatic);

            // Barricades either side of the entrance.
            var barricade = new MeshBuilder(1);
            for (var k = -1; k <= 1; k++)
            {
                barricade.Cylinder(0, new Vector3(k * 0.9f, -0.2f, -1.1f), new Vector3(k * 0.9f, 1.5f, 1.1f), 0.1f, 0f, 5, false);
                barricade.Cylinder(0, new Vector3(k * 0.9f, -0.2f, 1.1f), new Vector3(k * 0.9f, 1.5f, -1.1f), 0.1f, 0f, 5, false);
            }
            barricade.Cylinder(0, new Vector3(-1.6f, 0.6f, 0f), new Vector3(1.6f, 0.6f, 0f), 0.12f, 0.12f, 6);
            var barricadeMesh = barricade.ToMesh("Barricade");
            foreach (var deg in new[] { gap + 4f, -gap - 4f })
            {
                var a = deg * Mathf.Deg2Rad;
                var p = Camp + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (PalisadeRadius + 2.2f);
                var go = Place(root, "Barricade", barricadeMesh, new[] { ctx.M.Wood }, ctx.OnGround(p), Quaternion.Euler(0f, -deg, 0f));
                Solid(go, new Vector3(0f, 0.7f, 0f), new Vector3(3.4f, 1.4f, 1.6f), ObstacleLayer);
            }

            // Tents, crates, barrels.
            var tent = new MeshBuilder(2);
            Prism(tent, 0, Vector3.zero, 3.6f, 2.8f, 2.1f);
            tent.Cylinder(1, new Vector3(-1.85f, 0f, 0f), new Vector3(-1.85f, 2.25f, 0f), 0.06f, 0.05f, 5);
            tent.Cylinder(1, new Vector3(1.85f, 0f, 0f), new Vector3(1.85f, 2.25f, 0f), 0.06f, 0.05f, 5);
            var tentMesh = tent.ToMesh("Tent");
            foreach (var (offset, yaw) in new[] { (new Vector2(7f, 6.5f), 200f), (new Vector2(4f, -8.5f), -25f), (new Vector2(-3f, -11f), 40f) })
            {
                var go = Place(root, "Tent", tentMesh, new[] { ctx.M.Canvas, ctx.M.DarkWood }, ctx.OnGround(Camp + offset, -0.05f), Quaternion.Euler(0f, yaw, 0f));
                Solid(go, new Vector3(0f, 0.9f, 0f), new Vector3(3.6f, 1.8f, 2.6f), ObstacleLayer);
            }
            var crate = new MeshBuilder(1);
            crate.Box(0, new Vector3(0f, 0.45f, 0f), new Vector3(0.9f, 0.9f, 0.9f));
            var crateMesh = crate.ToMesh("Crate");
            var barrel = new MeshBuilder(1);
            barrel.Cylinder(0, Vector3.zero, new Vector3(0f, 0.5f, 0f), 0.36f, 0.42f, 10, false);
            barrel.Cylinder(0, new Vector3(0f, 0.5f, 0f), new Vector3(0f, 1f, 0f), 0.42f, 0.36f, 10);
            var barrelMesh = barrel.ToMesh("Barrel");
            var props = new[]
            {
                (new Vector2(-4f, 1f), true), (new Vector2(-4.2f, 2.1f), true), (new Vector2(-3.1f, 1.4f), false), (new Vector2(9f, 2f), false),
                (new Vector2(9.7f, 2.9f), false), (new Vector2(1f, -10.5f), true), (new Vector2(-6.5f, -6f), true), (new Vector2(-6f, -7.1f), false),
            };
            foreach (var (offset, isCrate) in props)
            {
                var p = Camp + offset;
                var go = Place(root, isCrate ? "Crate" : "Barrel", isCrate ? crateMesh : barrelMesh, new[] { isCrate ? ctx.M.DarkWood : ctx.M.Wood },
                    ctx.OnGround(p, -0.03f), Quaternion.Euler(0f, ctx.Range(0f, 90f), 0f), ctx.Range(0.9f, 1.1f));
                Solid(go, new Vector3(0f, 0.5f, 0f), new Vector3(0.9f, 1f, 0.9f), ObstacleLayer);
                CullWhenSmall(go, 0.015f);
            }

            // Campfire with a warm light.
            var fire = new MeshBuilder(3);
            for (var i = 0; i < 9; i++)
            {
                var a = i * Mathf.PI * 2f / 9f;
                fire.Blob(0, new Vector3(Mathf.Cos(a) * 0.85f, 0.08f, Mathf.Sin(a) * 0.85f), new Vector3(0.24f, 0.16f, 0.22f), 0.3f, 40 + i);
            }
            for (var i = 0; i < 3; i++)
            {
                var a = i * 60f * Mathf.Deg2Rad;
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.6f;
                fire.Cylinder(1, -d + Vector3.up * 0.1f, d + Vector3.up * 0.2f, 0.1f, 0.09f, 6);
            }
            fire.Blob(2, new Vector3(0f, 0.55f, 0f), new Vector3(0.32f, 0.55f, 0.32f), 0.35f, 60);
            fire.Blob(2, new Vector3(0.12f, 0.8f, -0.05f), new Vector3(0.16f, 0.4f, 0.16f), 0.3f, 61);
            var fireGo = Place(root, "Campfire", fire.ToMesh("Campfire"), new[] { ctx.M.Rock, ctx.M.DarkWood, ctx.M.Flame }, centre, Quaternion.identity, 1f, false);
            var lightGo = new GameObject("Firelight");
            lightGo.transform.SetParent(fireGo.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.3f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.6f, 0.28f);
            // The camp is at dusk (Atmosphere/Dusk): the fire carries the scene, as in the key art.
            light.range = 15f;
            light.intensity = 5f;
            light.shadows = LightShadows.None;
        }

        /// <summary>The wolf den (lv 11–12): a hollow ringed by boulders below the east slopes, a cave mouth, bones.</summary>
        static void BuildDen(Context ctx)
        {
            var root = ctx.Group("Wolf Den");
            var big = new[] { BlobMesh("DenRockA", 51, new Vector3(1.6f, 1.3f, 1.9f), 0.3f), BlobMesh("DenRockB", 52, new Vector3(1.8f, 1.6f, 1.5f), 0.35f, 2, 1.5f) };
            for (var deg = 40f; deg <= 300f; deg += 32f)
            {
                var a = (deg + ctx.Range(-8f, 8f)) * Mathf.Deg2Rad;
                var p = Den + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * ctx.Range(13f, 15.5f);
                if (RoadEdge(p.x, p.y) < 3f) continue;
                var go = Place(root, "Den Rock", big[ctx.Rng.Next(big.Length)], new[] { ctx.M.Rock }, ctx.OnGround(p, -0.6f),
                    Quaternion.Euler(ctx.Range(-8f, 8f), ctx.Range(0f, 360f), 0f), ctx.Range(1.3f, 2.1f));
                Collide(go, true);
            }

            // Cave mouth in the east slope, facing the den.
            var cave = new MeshBuilder(2);
            cave.Blob(0, new Vector3(4f, 2.5f, 0f), new Vector3(4.5f, 5f, 7.5f), 0.25f, 70);
            cave.Blob(0, new Vector3(0.5f, 1.8f, -3.3f), new Vector3(1.8f, 2.8f, 1.6f), 0.3f, 71);
            cave.Blob(0, new Vector3(0.5f, 1.8f, 3.3f), new Vector3(1.8f, 2.8f, 1.6f), 0.3f, 72);
            cave.Blob(0, new Vector3(0.3f, 4.6f, 0f), new Vector3(2f, 1.3f, 4.6f), 0.3f, 73);
            cave.Box(1, new Vector3(-0.35f, 1.7f, 0f), new Vector3(0.2f, 3.6f, 4.6f));
            var caveGo = Place(root, "Cave Mouth", cave.ToMesh("CaveMouth"), new[] { ctx.M.Rock, ctx.M.CaveDark }, ctx.OnGround(Cave, -0.6f), Quaternion.identity, 1.5f);
            Collide(caveGo, false);

            var bones = new MeshBuilder(1);
            for (var i = 0; i < 14; i++)
            {
                var p = new Vector3(ctx.Range(-8f, 8f), 0.05f, ctx.Range(-8f, 8f));
                var d = Quaternion.Euler(0f, ctx.Range(0f, 360f), 0f) * Vector3.forward * ctx.Range(0.25f, 0.45f);
                bones.Cylinder(0, p - d, p + d, 0.045f, 0.04f, 5);
                if (i % 5 == 0) bones.Blob(0, p + Vector3.up * 0.08f, new Vector3(0.13f, 0.11f, 0.17f), 0.2f, 80 + i);
            }
            Place(root, "Bones", bones.ToMesh("Bones"), new[] { ctx.M.Bone }, ctx.OnGround(Den, 0f), Quaternion.identity, 1f, false);
        }

        /// <summary>The north pass to Serpent Marsh (lv 12–25): standing stones and a signpost; the road beyond isn't open yet.</summary>
        static void BuildNorthExit(Context ctx)
        {
            var root = ctx.Group("North Exit");
            var stone = BlobMesh("StandingStone", 91, new Vector3(1.2f, 3.4f, 1f), 0.15f);
            foreach (var x in new[] { -8.5f, 8.5f })
            {
                var go = Place(root, "Standing Stone", stone, new[] { ctx.M.Rock }, ctx.OnGround(new Vector2(x, NorthExit.y + 2f), -0.8f),
                    Quaternion.Euler(ctx.Range(-4f, 4f), ctx.Range(0f, 360f), ctx.Range(-4f, 4f)));
                Collide(go, true);
            }
            var sign = new MeshBuilder(1);
            sign.Box(0, new Vector3(0f, 1.4f, 0f), new Vector3(0.18f, 2.8f, 0.18f));
            sign.Box(0, new Vector3(0.55f, 2.4f, 0f), new Vector3(1.5f, 0.42f, 0.07f), Quaternion.Euler(0f, 0f, 4f));
            sign.Box(0, new Vector3(-0.45f, 1.9f, 0f), new Vector3(1.2f, 0.36f, 0.07f), Quaternion.Euler(0f, 0f, -3f));
            Place(root, "Signpost", sign.ToMesh("Signpost"), new[] { ctx.M.Wood }, ctx.OnGround(SignpostSpot, -0.1f), Quaternion.Euler(0f, -70f, 0f));
        }

        static readonly Vector2 SignpostSpot = new Vector2(6.5f, 197f);

        // ---------------------------------------------------------------- scatter

        /// <summary>Trees, bushes and rocks: sparse oaks in the meadow, dense forest up to the river, clumps north of it, foothill trees along the edges.</summary>
        static void Scatter(Context ctx)
        {
            var trees = ctx.Group("Trees");
            var rocks = ctx.Group("Rocks");
            var bushes = ctx.Group("Bushes");
            var placed = new List<(Vector2 p, float r)>();

            var oakMeadow = new Mesh[][] { OakMesh("OakMeadowA", 101, 2.6f, 0.55f, 10.5f, 6f, 0.4f, 6), OakMesh("OakMeadowB", 102, 3f, 0.6f, 9.5f, 5.6f, 1.1f, 6) };
            var oakForest = new Mesh[][]
            {
                OakMesh("OakForestA", 201, 6f, 0.8f, 9.5f, 6f, 0.25f, 5, -0.05f), OakMesh("OakForestB", 202, 5.6f, 0.9f, 10f, 5.6f, 0.8f, 6, -0.05f),
                OakMesh("OakForestC", 203, 6.4f, 0.75f, 8.5f, 6f, 0.5f, 5, -0.05f),
            };
            var rockMeshes = new[]
            {
                BlobMesh("RockA", 301, new Vector3(1f, 0.7f, 1.25f), 0.3f), BlobMesh("RockB", 302, new Vector3(1.1f, 0.85f, 0.9f), 0.35f),
                BlobMesh("RockC", 303, new Vector3(0.9f, 0.55f, 1.2f), 0.25f, 2, 1.2f),
            };
            var bush = BlobMesh("Bush", 401, new Vector3(0.75f, 0.6f, 0.75f), 0.3f, 3, 1.3f);
            var gorse = BlobMesh("GorseBush", 31, new Vector3(0.8f, 0.6f, 0.8f), 0.35f, 3, 1.4f);

            bool Clear(Vector2 p, float radius, float road, float edge)
            {
                if (Outside(p.x, p.y) > edge) return false;
                if (RoadEdge(p.x, p.y) < road) return false;
                if (RiverDistance(p.x, p.y) < RiverHalfWidth + 2.5f + radius) return false;
                if (p.x < TerrainOrigin.x + 3f || p.x > TerrainOrigin.x + TerrainSize.x - 3f || p.y < TerrainOrigin.z + 3f || p.y > TerrainOrigin.z + TerrainSize.z - 3f) return false;
                foreach (var (c, r) in Keepouts)
                    if (Vector2.Distance(p, c) < r + radius) return false;
                foreach (var (q, r) in placed)
                    if ((p - q).sqrMagnitude < (r + radius) * (r + radius)) return false;
                return true;
            }

            void Tree(Vector2 p, Mesh[][] variants, Palette leaves, float minScale, float maxScale)
            {
                var scale = ctx.Range(minScale, maxScale);
                var shape = variants[ctx.Rng.Next(variants.Length)];
                var lods = new[] { PaletteMesh(shape[0], Palette.Bark, leaves), PaletteMesh(shape[1], Palette.Bark, leaves) };
                var go = new GameObject("Oak") { layer = ObstacleLayer };
                go.transform.SetParent(trees, false);
                go.transform.SetPositionAndRotation(ctx.OnGround(p, -0.1f), Quaternion.Euler(0f, ctx.Range(0f, 360f), 0f));
                go.transform.localScale = Vector3.one * scale;
                var near = Place(go.transform, "LOD0", lods[0], new[] { ctx.M.Foliage }, go.transform.position, go.transform.rotation);
                var far = Place(go.transform, "LOD1", lods[1], new[] { ctx.M.Foliage }, go.transform.position, go.transform.rotation);
                near.transform.localScale = far.transform.localScale = Vector3.one;
                var lodGroup = go.AddComponent<LODGroup>();
                lodGroup.SetLODs(new[] { new LOD(0.12f, new Renderer[] { near.GetComponent<Renderer>() }), new LOD(0f, new Renderer[] { far.GetComponent<Renderer>() }) });
                lodGroup.RecalculateBounds();
                var trunk = go.AddComponent<CapsuleCollider>();
                trunk.center = new Vector3(0f, 2.5f, 0f);
                trunk.radius = 0.5f;
                trunk.height = 5f;
                ctx.Trees++;
            }

            void Spread(Vector2 min, Vector2 max, int attempts, Func<Vector2, bool> accept, float radius, float road, float edge, Action<Vector2> place)
            {
                for (var i = 0; i < attempts; i++)
                {
                    var p = new Vector2(ctx.Range(min.x, max.x), ctx.Range(min.y, max.y));
                    if (!accept(p) || !Clear(p, radius, road, edge)) continue;
                    placed.Add((p, radius));
                    place(p);
                }
            }

            bool InForest(Vector2 p) => p.y > ForestEdge(p.x) && p.y < RiverZ(p.x) - 4f;
            bool InFringe(Vector2 p) => p.y < ForestEdge(p.x) && p.y > ForestEdge(p.x) - 24f;
            var wide = new Vector2(TerrainOrigin.x, TerrainOrigin.z);
            var far = new Vector2(TerrainOrigin.x + TerrainSize.x, TerrainOrigin.z + TerrainSize.z);

            // Landmark boulders and the forest's mossy rocks first, so trees grow around them.
            Spread(new Vector2(wide.x, -95f), new Vector2(far.x, 50f), 110, InForest, 2.2f, 3f, -3f, p =>
            {
                var go = Place(rocks, "Mossy Rock", PaletteMesh(rockMeshes[ctx.Rng.Next(rockMeshes.Length)], Palette.MossRock), new[] { ctx.M.Foliage }, ctx.OnGround(p, -0.4f),
                    Quaternion.Euler(0f, ctx.Range(0f, 360f), 0f), ctx.Range(1.2f, 2.6f));
                go.layer = ObstacleLayer;
                Collide(go, true);
                ctx.Rocks++;
            });

            Spread(new Vector2(wide.x, -95f), new Vector2(far.x, 70f), 9000, InForest, 4.4f, 4.5f, -2f, p => Tree(p, oakForest, Palette.LeavesDeep, 0.95f, 1.3f));
            // The forest thins out into the meadow rather than stopping at a line.
            Spread(new Vector2(wide.x, -120f), new Vector2(far.x, -40f), 1500, p => InFringe(p) && Perlin(p.x / 18f + 70f, p.y / 18f) > 0.45f, 5.5f, 5f, -2f,
                p => Tree(p, oakForest, ctx.Rng.NextDouble() < 0.5 ? Palette.LeavesDeep : Palette.Leaves, 0.9f, 1.2f));
            Spread(new Vector2(wide.x, -200f), new Vector2(far.x, -40f), 900, p => p.y < ForestEdge(p.x) - 4f && Perlin(p.x / 40f, p.y / 40f) > 0.42f, 9f, 6f, -2f,
                p => Tree(p, oakMeadow, Palette.Leaves, 0.95f, 1.4f));
            Spread(new Vector2(wide.x, 60f), new Vector2(far.x, 200f), 2500, p => p.y > RiverZ(p.x) + 6f && Perlin(p.x / 35f + 9f, p.y / 35f) > 0.48f, 5f, 5f, -2f,
                p => Tree(p, ctx.Rng.NextDouble() < 0.5 ? oakForest : oakMeadow, ctx.Rng.NextDouble() < 0.5 ? Palette.LeavesDeep : Palette.Leaves, 0.9f, 1.3f));
            // Foothills beyond the walkable edge frame the valley.
            Spread(wide, far, 3000, p => Outside(p.x, p.y) > 1f && Outside(p.x, p.y) < 18f, 7f, 0f, 18f,
                p => Tree(p, ctx.Rng.NextDouble() < 0.6 ? oakForest : oakMeadow, Palette.LeavesDeep, 0.9f, 1.4f));

            Spread(wide, far, 450, p => Outside(p.x, p.y) < -4f, 1.2f, 2.5f, -4f, p =>
            {
                var forest = InForest(p);
                var go = Place(rocks, "Rock", PaletteMesh(rockMeshes[ctx.Rng.Next(rockMeshes.Length)], forest ? Palette.MossRock : Palette.Rock), new[] { ctx.M.Foliage },
                    ctx.OnGround(p, -0.25f), Quaternion.Euler(ctx.Range(-10f, 10f), ctx.Range(0f, 360f), 0f), ctx.Range(0.35f, 1.3f));
                if (go.transform.localScale.x > 0.7f)
                {
                    go.layer = ObstacleLayer;
                    Collide(go, true);
                }
                else CullWhenSmall(go);
                ctx.Rocks++;
            });
            // Big rocks on the slopes for a broken skyline.
            Spread(wide, far, 500, p => Outside(p.x, p.y) > 3f, 4f, 0f, 60f, p =>
            {
                Place(rocks, "Crag", PaletteMesh(rockMeshes[ctx.Rng.Next(rockMeshes.Length)], Palette.Rock), new[] { ctx.M.Foliage }, ctx.OnGround(p, -1.5f),
                    Quaternion.Euler(ctx.Range(-15f, 15f), ctx.Range(0f, 360f), ctx.Range(-15f, 15f)), ctx.Range(2.5f, 5f), false);
                ctx.Rocks++;
            });
            // Pebbles on the river banks.
            for (var i = 0; i < 160; i++)
            {
                var x = ctx.Range(TerrainOrigin.x, TerrainOrigin.x + TerrainSize.x);
                var side = ctx.Rng.NextDouble() < 0.5 ? -1f : 1f;
                var p = new Vector2(x, RiverZ(x) + side * ctx.Range(RiverHalfWidth - 0.5f, RiverHalfWidth + 2.5f));
                if (Vector2.Distance(p, Flat(BridgeCentre)) < 4f || Outside(p.x, p.y) > 2f) continue;
                CullWhenSmall(Place(rocks, "Bank Rock", PaletteMesh(rockMeshes[ctx.Rng.Next(rockMeshes.Length)], Palette.Rock), new[] { ctx.M.Foliage }, ctx.OnGround(p, -0.2f),
                    Quaternion.Euler(0f, ctx.Range(0f, 360f), 0f), ctx.Range(0.3f, 0.8f), false));
                ctx.Rocks++;
            }

            Spread(wide, far, 700, p => Outside(p.x, p.y) < -3f && (InForest(p) || ctx.Rng.NextDouble() < 0.45), 1f, 1.8f, -3f, p =>
            {
                var yellow = InForest(p) && ctx.Rng.NextDouble() < 0.3;
                CullWhenSmall(Place(bushes, yellow ? "Gorse" : "Bush", PaletteMesh(yellow ? gorse : bush, yellow ? Palette.Gorse : Palette.Bush), new[] { ctx.M.Foliage }, ctx.OnGround(p, -0.15f),
                    Quaternion.Euler(0f, ctx.Range(0f, 360f), 0f), ctx.Range(0.7f, 1.3f), false));
            });

            // Hundreds of separate trees would cost hundreds of draw calls on a phone: merge them per cell at load instead.
            Batch(trees, 40f, 0f, 0.9f);
            Batch(rocks, 80f, 0f);
            Batch(bushes, 24f, 0.12f);
        }

        static void Batch(Transform group, float cellSize, float cullScreenHeight, float detailScreenHeight = 0f)
        {
            foreach (var t in group.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
            var batcher = new SerializedObject(group.gameObject.AddComponent<ScatterBatcher>());
            batcher.FindProperty("cellSize").floatValue = cellSize;
            batcher.FindProperty("cullScreenHeight").floatValue = cullScreenHeight;
            batcher.FindProperty("detailScreenHeight").floatValue = detailScreenHeight;
            batcher.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- bounds, ledges, anchors, areas

        /// <summary>Invisible walls just inside the foot of the slopes, across the south gate and the north pass.</summary>
        static void BuildBounds(Context ctx)
        {
            var group = ctx.Group("Bounds");
            const float south = SouthWallZ - 2f, north = NorthWallZ + 5f, step = 4f;
            foreach (var side in new[] { -1f, 1f })
            {
                Vector3 Edge(float z) => new Vector3(CentreX(z) + side * (HalfWidth(z, side) + BoundsInset), 0f, z);
                for (var z = south; z < north; z += step)
                    Wall(group, Edge(z), Edge(Mathf.Min(z + step, north)), 10f, ctx);
            }
            foreach (var z in new[] { south, north })
            {
                float west = -HalfWidth(z, -1f) - BoundsInset, east = HalfWidth(z, 1f) + BoundsInset;
                for (var x = west; x < east; x += 8f)
                    Wall(group, new Vector3(CentreX(z) + x, 0f, z), new Vector3(CentreX(z) + Mathf.Min(x + 8f, east), 0f, z), 10f, ctx);
            }
        }

        static void BuildLedges(Context ctx, Transform hanLedges)
        {
            // The Han's practice stones and kulla roof keep their spots relative to the Han.
            Move(hanLedges, "LeapStone Low", HanRoot + new Vector3(-9f, 1.5f, 10f));
            Move(hanLedges, "LeapStone High", HanRoot + new Vector3(-16f, 3f, 4f));
            Move(hanLedges, "Kulla Roof", HanRoot + new Vector3(7.5f, 9.35f, -0.5f));
            foreach (var old in new[] { "LeapStone West Den", "LeapStone North Den" })
            {
                var t = hanLedges.Find(old);
                if (t != null) Object.DestroyImmediate(t.gameObject);
            }

            var group = ctx.Group("Ledges");
            StoneLedge(ctx, group, "LeapStone Meadow West", MeadowWestPack + new Vector2(10f, 7f), new Vector3(3.5f, 2.6f, 3.5f), 20f);
            StoneLedge(ctx, group, "LeapStone Meadow East", MeadowEastPack + new Vector2(-9f, -6f), new Vector3(4f, 3.4f, 4f), -10f);
            StoneLedge(ctx, group, "LeapStone Den Crag", Den + new Vector2(-4f, 12f), new Vector3(4f, 4.2f, 4f), 35f);

            var toCamp = (new Vector3(Camp.x, 0f, Camp.y) - new Vector3(Tower.x, 0f, Tower.y)).normalized;
            var towerRot = Quaternion.LookRotation(Vector3.Cross(toCamp, Vector3.up));
            var towerBase = ctx.OnGround(Tower, -0.4f);
            Ledge(ctx, group, "Tower Top", towerBase + towerRot * new Vector3(0f, 9.3f, 0f), new Vector3(4.6f, 0.4f, 4.6f), towerRot, ctx.M.DarkWood);
            Ledge(ctx, group, "Tower Scaffold", towerBase + towerRot * new Vector3(0f, 5.45f, -4.3f), new Vector3(4.4f, 0.3f, 2.4f), towerRot, ctx.M.Wood);
        }

        static void StoneLedge(Context ctx, Transform parent, string name, Vector2 at, Vector3 size, float yaw)
        {
            var ground = Mathf.Min(ctx.Ground(at.x - size.x * 0.5f, at.y - size.z * 0.5f), ctx.Ground(at.x + size.x * 0.5f, at.y + size.z * 0.5f),
                ctx.Ground(at.x - size.x * 0.5f, at.y + size.z * 0.5f), ctx.Ground(at.x + size.x * 0.5f, at.y - size.z * 0.5f));
            Ledge(ctx, parent, name, new Vector3(at.x, ground + size.y * 0.5f - 0.3f, at.y), size, Quaternion.Euler(0f, yaw, 0f), ctx.M.Rock);
        }

        /// <summary>A box collider whose top is a Drangue Leap landing, with its visible block and the glowing cyan rim.</summary>
        static void Ledge(Context ctx, Transform parent, string name, Vector3 centre, Vector3 size, Quaternion rotation, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(centre, rotation);
            go.AddComponent<BoxCollider>().size = size;
            go.AddComponent<LeapLedge>();
            var cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            void Child(string childName, Vector3 position, Vector3 scale, Material m)
            {
                var c = new GameObject(childName);
                c.transform.SetParent(go.transform, false);
                c.transform.localPosition = position;
                c.transform.localScale = scale;
                c.AddComponent<MeshFilter>().sharedMesh = cube;
                c.AddComponent<MeshRenderer>().sharedMaterial = m;
                GameObjectUtility.SetStaticEditorFlags(c, StaticEditorFlags.BatchingStatic);
            }
            Child("Mesh", Vector3.zero, size, material);
            var top = size.y * 0.5f + 0.01f;
            Child("RimN", new Vector3(0f, top, size.z * 0.5f - 0.06f), new Vector3(size.x, 0.05f, 0.12f), ctx.M.LeapMarker);
            Child("RimS", new Vector3(0f, top, -size.z * 0.5f + 0.06f), new Vector3(size.x, 0.05f, 0.12f), ctx.M.LeapMarker);
            Child("RimE", new Vector3(size.x * 0.5f - 0.06f, top, 0f), new Vector3(0.12f, 0.05f, size.z), ctx.M.LeapMarker);
            Child("RimW", new Vector3(-size.x * 0.5f + 0.06f, top, 0f), new Vector3(0.12f, 0.05f, size.z), ctx.M.LeapMarker);
        }

        static void PlaceAnchors(Context ctx, Transform environment, Transform gameplay)
        {
            environment.Find("Han").position = HanRoot;
            var respawn = gameplay.Find("RespawnPoint");
            respawn.SetPositionAndRotation(HanRoot + new Vector3(0f, 0f, 14f), Quaternion.identity);
            gameplay.Find("HanKeeper").position = HanRoot + new Vector3(-3.2f, 0f, 5.4f);

            var player = GameObject.Find("Player").transform;
            player.SetPositionAndRotation(respawn.position, respawn.rotation);
            var camera = GameObject.Find("Main Camera").transform;
            camera.SetPositionAndRotation(respawn.position + new Vector3(0f, 5.4f, -7f), Quaternion.Euler(28f, 0f, 0f));

            // Monsters by area: wolves in the meadow (lv 1–4), boars in the oak forest (5–8), bandits at
            // the camp (8–11), den wolves at the den (11–12).
            Pack(ctx, gameplay, new[] { "MeadowWestPack", "WestDen" }, "MeadowWestPack", MeadowWestPack, "Wolf", 5, 6f);
            Pack(ctx, gameplay, new[] { "MeadowEastPack", "EastDen" }, "MeadowEastPack", MeadowEastPack, "Wolf", 5, 6f);
            Pack(ctx, gameplay, new[] { "BoarSpringHerd" }, "BoarSpringHerd", BoarSpringHerd, "Boar", 4, 6f);
            Pack(ctx, gameplay, new[] { "BoarEastHerd" }, "BoarEastHerd", BoarEastHerd, "Boar", 4, 7f);
            Pack(ctx, gameplay, new[] { "BanditCampGang" }, "BanditCampGang", BanditCampGang, "Bandit", 5, 7f);
            Pack(ctx, gameplay, new[] { "BanditLookouts" }, "BanditLookouts", BanditLookouts, "Bandit", 3, 3.5f);
            Pack(ctx, gameplay, new[] { "WolfDenPack", "NorthDen" }, "WolfDenPack", DenPack, "DenWolf", 4, 7f);

            var shepherd = gameplay.Find("Shepherd");
            if (shepherd == null) shepherd = CreateShepherd(ctx, gameplay);
            shepherd.SetPositionAndRotation(ctx.OnGround(ShepherdSpot), Quaternion.LookRotation(new Vector3(-1f, 0f, 0.3f)));

            var signs = gameplay.Find("Signs");
            if (signs == null)
            {
                signs = new GameObject("Signs").transform;
                signs.SetParent(gameplay, false);
            }
            Anchor(signs, "ToDardana", ctx.OnGround(Gate, 5.5f) + Vector3.forward * 1.5f);
            Anchor(signs, "ToSerpentMarsh", ctx.OnGround(SignpostSpot, 1.2f));
        }

        /// <summary>
        /// Moves the spawner (found under any of <paramref name="names"/>, created if none) to its spot
        /// and sets which monster prefab it keeps alive, how many and how far they spread.
        /// </summary>
        static void Pack(Context ctx, Transform gameplay, string[] names, string name, Vector2 at, string prefab, int count, float radius)
        {
            Transform t = null;
            foreach (var n in names)
                if ((t = gameplay.Find(n)) != null) break;
            if (t == null)
            {
                t = new GameObject(name, typeof(MonsterSpawner)).transform;
                t.SetParent(gameplay, false);
            }
            t.name = name;
            t.position = ctx.OnGround(at);

            var path = $"Assets/Prefabs/{prefab}.prefab";
            var monster = AssetDatabase.LoadAssetAtPath<MonsterAI>(path);
            if (monster == null) Debug.LogWarning($"[OakValley] No monster prefab at {path} for {name}");
            var spawner = new SerializedObject(t.GetComponent<MonsterSpawner>());
            spawner.FindProperty("prefab").objectReferenceValue = monster;
            spawner.FindProperty("count").intValue = count;
            spawner.FindProperty("radius").floatValue = radius;
            spawner.ApplyModifiedProperties();
        }

        static void Anchor(Transform parent, string name, Vector3 position)
        {
            var t = parent.Find(name);
            if (t == null)
            {
                t = new GameObject(name).transform;
                t.SetParent(parent, false);
            }
            t.position = position;
        }

        /// <summary>The shepherd: an NPC on the Npc layer (no errands until quests exist), built like the Han Keeper.</summary>
        static Transform CreateShepherd(Context ctx, Transform gameplay)
        {
            var keeper = gameplay.Find("HanKeeper");
            var go = new GameObject("Shepherd") { layer = NpcLayer };
            go.transform.SetParent(gameplay, false);
            var keeperCapsule = keeper.GetComponent<CapsuleCollider>();
            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.center = keeperCapsule.center;
            capsule.radius = keeperCapsule.radius;
            capsule.height = keeperCapsule.height;

            var visual = Object.Instantiate(keeper.Find("Visual").gameObject, go.transform);
            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.GetComponent<MeshRenderer>().sharedMaterial = ctx.M.ShepherdCloth;
            var crook = new MeshBuilder(1);
            crook.Cylinder(0, new Vector3(0.45f, 0f, 0.15f), new Vector3(0.45f, 1.9f, 0.15f), 0.035f, 0.03f, 6);
            crook.Cylinder(0, new Vector3(0.45f, 1.9f, 0.15f), new Vector3(0.3f, 2.05f, 0.15f), 0.03f, 0.03f, 6);
            crook.Cylinder(0, new Vector3(0.3f, 2.05f, 0.15f), new Vector3(0.2f, 1.9f, 0.15f), 0.03f, 0.025f, 6);
            var crookGo = new GameObject("Crook");
            crookGo.transform.SetParent(visual.transform, false);
            crookGo.AddComponent<MeshFilter>().sharedMesh = crook.ToMesh("ShepherdCrook");
            crookGo.AddComponent<MeshRenderer>().sharedMaterial = ctx.M.Wood;
            return go.transform;
        }

        static void BuildAreas(Context ctx, Transform gameplay)
        {
            var old = gameplay.Find("Areas");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var root = ctx.Group("Areas", gameplay);
            void Area(string name, int min, int max, Vector2 centre, Vector2 size, int priority)
            {
                var go = new GameObject(name);
                go.transform.SetParent(root, false);
                go.transform.position = new Vector3(centre.x, 0f, centre.y);
                go.AddComponent<ZoneArea>().Configure(name, min, max, size, priority);
            }
            Area("The Han", 0, 0, new Vector2(-10f, -192f), new Vector2(46f, 36f), 2);
            Area("Meadow", 1, 4, new Vector2(0f, -137f), new Vector2(300f, 142f), 0);
            Area("Shepherd's Fold", 1, 4, new Vector2(52f, -131f), new Vector2(34f, 30f), 1);
            Area("Oak Forest", 5, 8, new Vector2(0f, -4f), new Vector2(300f, 124f), 0);
            Area("Dried Spring", 5, 8, Spring, new Vector2(26f, 26f), 1);
            Area("Upper Valley", 8, 12, new Vector2(0f, 134f), new Vector2(300f, 152f), 0);
            Area("Bandit Camp", 8, 11, Camp, new Vector2(56f, 56f), 1);
            Area("Wolf Den", 11, 12, new Vector2(89f, 170f), new Vector2(50f, 44f), 1);
            Area("Road to Serpent Marsh", 12, 25, new Vector2(0f, 207f), new Vector2(40f, 14f), 2);
        }

        const string AtmosphereRoot = ArtRoot + "/Atmosphere";

        /// <summary>
        /// Each area's mood, after its key art in docs/art/oak-valley/: golden afternoon at the Han and the
        /// meadow, low gold in the oak forest, sunset over the upper valley, dusk at the bandit camp, overcast
        /// twilight at the wolf den, misty dusk on the road north. The first one is the scene's start.
        /// </summary>
        static readonly (string profile, string[] areas)[] Moods =
        {
            ("Afternoon", new[] { "The Han", "Meadow", "Shepherd's Fold" }),
            ("Forest", new[] { "Oak Forest", "Dried Spring" }),
            ("Sunset", new[] { "Upper Valley" }),
            ("Dusk", new[] { "Bandit Camp" }),
            ("Overcast", new[] { "Wolf Den" }),
            ("MarshMist", new[] { "Road to Serpent Marsh" }),
        };

        /// <summary>
        /// Sun, sky (<c>Wuxia/Sky</c>), fog and post-processing. <c>Generated/Atmosphere</c> holds a
        /// <see cref="ZoneAtmosphere"/> that eases between the <see cref="Moods"/> as the player changes area,
        /// a base volume (tonemapping, bloom, vignette) and one grading volume per mood. The moods are assets in
        /// <see cref="AtmosphereRoot"/> (<c>&lt;Mood&gt;.asset</c> and its <c>&lt;Mood&gt;_Post.asset</c>): tune them there.
        /// </summary>
        static void SetUpLighting(Transform generated)
        {
            var sun = GameObject.Find("Sun").GetComponent<Light>();
            RenderSettings.sun = sun;

            var skyPath = $"{ArtRoot}/Materials/OakValleySky.mat";
            var skyShader = Shader.Find("Wuxia/Sky");
            var sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if (sky == null)
            {
                sky = new Material(skyShader);
                AssetDatabase.CreateAsset(sky, skyPath);
            }
            else if (sky.shader != skyShader) sky.shader = skyShader;
            RenderSettings.skybox = sky;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;

            // Fog ends inside the far plane, so the far slopes melt into the sky instead of being cut off.
            var camera = Camera.main;
            camera.farClipPlane = 240f;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            var root = new GameObject("Atmosphere").transform;
            root.SetParent(generated, false);
            AddVolume(root, "Base", LoadAtmosphere<VolumeProfile>("Base_Post"), 0f, 1f);
            var areaMoods = new List<ZoneAtmosphere.AreaMood>();
            AtmosphereProfile start = null;
            foreach (var (name, areas) in Moods)
            {
                var profile = LoadAtmosphere<AtmosphereProfile>(name);
                if (start == null) start = profile;
                if (profile.volume != null) AddVolume(root, name, profile.volume, 1f, 0f);
                foreach (var area in areas) areaMoods.Add(new ZoneAtmosphere.AreaMood { area = area, profile = profile });
            }
            var atmosphere = root.gameObject.AddComponent<ZoneAtmosphere>();
            atmosphere.Configure(start, areaMoods, sun, sky, camera);
            atmosphere.ApplyInstant(start);
            EditorUtility.SetDirty(sky);
        }

        static T LoadAtmosphere<T>(string name) where T : Object
        {
            var path = $"{AtmosphereRoot}/{name}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException($"Missing atmosphere asset {path}");
            return asset;
        }

        static void AddVolume(Transform parent, string name, VolumeProfile profile, float priority, float weight)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = priority;
            volume.weight = weight;
            volume.sharedProfile = profile;
        }

        // ---------------------------------------------------------------- helpers

        static void Move(Transform parent, string name, Vector3 position)
        {
            var t = parent.Find(name);
            if (t != null) t.position = position;
        }

        static void EnsureLayer(int index, string name)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layer = tagManager.FindProperty("layers").GetArrayElementAtIndex(index);
            if (layer.stringValue == name) return;
            if (!string.IsNullOrEmpty(layer.stringValue)) throw new InvalidOperationException($"Layer {index} is already '{layer.stringValue}'");
            layer.stringValue = name;
            tagManager.ApplyModifiedProperties();
        }

        static Vector2 Flat(Vector3 v) => new Vector2(v.x, v.z);
        static Vector3 Flat3(Vector3 v) => new Vector3(v.x, 0f, v.z);
        static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        static float Perlin(float x, float z) => Mathf.PerlinNoise(x + 1000.5f, z + 1000.5f);

        static float Smooth(float a, float b, float x)
        {
            var t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        static float SoftMax(float a, float b, float k)
        {
            var h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / k);
            return Mathf.Lerp(a, b, h) + k * h * (1f - h);
        }

        static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }
    }
}
