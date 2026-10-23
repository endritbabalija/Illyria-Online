using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Wuxia.Data;
using Wuxia.Player;
using Wuxia.World;
using Object = UnityEngine.Object;

namespace Wuxia.EditorTools
{
    /// <summary>
    /// Renders one icon per item in items.json (plus the gold pile and pouch) into a single atlas,
    /// Assets/Items/Icons/ItemIcons.png, and stores it with each icon's UV rect in ItemModels.asset.
    /// Items with a model in the registry are rendered from it; worn weapons from the greybox weapon
    /// of their type on the Player prefab; everything else from a greybox stand-in built here out of
    /// primitives (a bottle in the potion's colour, a hood, a vest, boots, a ring, an amulet).
    /// Menu: Wuxia → Items → Generate Item Icons. From MCP: Wuxia.EditorTools.ItemIconGenerator.Generate().
    /// </summary>
    public static class ItemIconGenerator
    {
        public const string RegistryPath = "Assets/Items/ItemModels.asset";
        public const string AtlasPath = "Assets/Items/Icons/ItemIcons.png";
        const string ItemsJsonPath = "Packages/com.wuxia.gamedata/items.json";
        const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";

        /// <summary>Icon size in the atlas, in pixels.</summary>
        public const int IconSize = 128;
        // Rendered this many times larger, then averaged down (anti-aliasing).
        const int Supersample = 4;
        // Transparent margin around each icon, in final pixels, so bilinear filtering never bleeds between cells.
        const int Padding = 5;
        // Dark rim around each icon so it reads on any tile colour, in final pixels.
        const float OutlineWidth = 1.5f;
        static readonly Color OutlineColor = new Color(0.04f, 0.035f, 0.03f, 0.8f);

        enum Pose { Upright, Weapon }

        struct Job
        {
            public string Key;
            public Func<Scene, GameObject> Build;
            public Pose Pose;
        }

        [MenuItem("Wuxia/Items/Generate Item Icons")]
        static void GenerateFromMenu() => Debug.Log(Generate());

        /// <summary>Renders every icon, writes the atlas and updates the registry. Returns a summary line.</summary>
        public static string Generate()
        {
            var registry = AssetDatabase.LoadAssetAtPath<ItemModelRegistry>(RegistryPath);
            if (registry == null) throw new InvalidOperationException("No item model registry at " + RegistryPath);
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(ItemsJsonPath);
            if (json == null) throw new InvalidOperationException("No items.json at " + ItemsJsonPath);
            var data = ItemsData.FromJson(json.text);

            var materials = new List<Material>();
            var meshes = new List<Mesh>();
            var jobs = CollectJobs(registry, data, materials, meshes);

            var scene = EditorSceneManager.NewPreviewScene();
            var pixels = new List<Color[]>();
            // Shaders compile synchronously, or a variant seen for the first time renders as nothing.
            var asyncShaders = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;
            try
            {
                var camera = SetUpStage(scene);
                foreach (var job in jobs) pixels.Add(Render(scene, camera, job));
            }
            finally
            {
                ShaderUtil.allowAsyncCompilation = asyncShaders;
                EditorSceneManager.ClosePreviewScene(scene);
                foreach (var m in materials) Object.DestroyImmediate(m);
                foreach (var m in meshes) Object.DestroyImmediate(m);
            }

            var entries = WriteAtlas(jobs, pixels, out var atlas);
            registry.SetIcons(atlas, entries);
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            return $"[Icons] Rendered {jobs.Count} item icons into {AtlasPath} ({atlas.width}×{atlas.height})";
        }

        // ---- What to render ------------------------------------------------------------------

        static List<Job> CollectJobs(ItemModelRegistry registry, ItemsData data, List<Material> materials, List<Mesh> meshes)
        {
            var jobs = new List<Job>();
            var keys = new HashSet<string>();
            var greyboxWeapons = LoadGreyboxWeapons();

            foreach (var def in data.items)
            {
                var key = ItemModelRegistry.IconKey(def);
                if (!keys.Add(key)) continue;
                var prefab = registry.FindPrefab(def.modelKey);
                if (prefab != null)
                {
                    jobs.Add(new Job { Key = key, Build = s => InstantiateIn(prefab, s), Pose = def.IsWeapon ? Pose.Weapon : Pose.Upright });
                }
                else if (def.IsWeapon && greyboxWeapons.TryGetValue(def.weaponType, out var model))
                {
                    jobs.Add(new Job { Key = key, Build = s => OneHanded(InstantiateIn(model, s)), Pose = Pose.Weapon });
                }
                else
                {
                    var item = def;
                    var pose = Pose.Upright;
                    jobs.Add(new Job { Key = key, Build = s => BuildStandIn(item, s, materials, meshes, registry), Pose = pose });
                }
            }

            AddEntryJob(jobs, keys, registry.Gold, "Gold");
            AddEntryJob(jobs, keys, registry.Pouch, "Pouch");
            return jobs;
        }

        static void AddEntryJob(List<Job> jobs, HashSet<string> keys, ItemModelRegistry.Entry entry, string fallbackKey)
        {
            if (entry == null || entry.prefab == null) return;
            var key = string.IsNullOrEmpty(entry.modelKey) ? fallbackKey : entry.modelKey;
            if (!keys.Add(key)) return;
            var prefab = entry.prefab;
            jobs.Add(new Job { Key = key, Build = s => InstantiateIn(prefab, s), Pose = Pose.Upright });
        }

        /// <summary>weapon type → the greybox weapon model under the Player prefab's WeaponPivot.</summary>
        static Dictionary<string, GameObject> LoadGreyboxWeapons()
        {
            var result = new Dictionary<string, GameObject>();
            var player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            var appearance = player != null ? player.GetComponentInChildren<PlayerAppearance>(true) : null;
            if (appearance == null) return result;
            var weapons = new SerializedObject(appearance).FindProperty("weapons");
            for (var i = 0; i < weapons.arraySize; i++)
            {
                var element = weapons.GetArrayElementAtIndex(i);
                var model = element.FindPropertyRelative("model").objectReferenceValue as GameObject;
                if (model != null) result[element.FindPropertyRelative("weaponId").stringValue] = model;
            }
            return result;
        }

        /// <summary>Paired greybox weapons (the sicas: SicaRight + SicaLeft) show just the right-hand one.</summary>
        static GameObject OneHanded(GameObject go)
        {
            foreach (Transform child in go.transform)
            {
                if (child.name.IndexOf("Left", StringComparison.Ordinal) < 0) continue;
                if (go.transform.Find(child.name.Replace("Left", "Right")) != null) child.gameObject.SetActive(false);
            }
            return go;
        }

        static GameObject InstantiateIn(GameObject source, Scene scene)
        {
            var go = Object.Instantiate(source);
            go.name = source.name;
            go.SetActive(true);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            go.transform.localScale = Vector3.one;
            SceneManager.MoveGameObjectToScene(go, scene);
            return go;
        }

        // ---- Greybox stand-ins -----------------------------------------------------------------

        static GameObject BuildStandIn(ItemDef def, Scene scene, List<Material> materials, List<Mesh> meshes, ItemModelRegistry registry)
        {
            var root = new GameObject(def.id);
            SceneManager.MoveGameObjectToScene(root, scene);
            var t = root.transform;
            var main = Mat(materials, MainColor(def), 0.35f);
            var dark = Mat(materials, MainColor(def) * 0.55f, 0.2f);

            if (def.IsUsable)
            {
                var glass = Mat(materials, PotionColor(def), 0.85f);
                var cork = Mat(materials, new Color(0.55f, 0.38f, 0.22f), 0.2f);
                var shine = Mat(materials, new Color(1f, 1f, 1f, 1f), 0.9f, new Color(0.6f, 0.6f, 0.6f));
                Part(t, PrimitiveType.Sphere, new Vector3(0f, 0.42f, 0f), new Vector3(0.84f, 0.8f, 0.84f), Vector3.zero, glass);
                Part(t, PrimitiveType.Cylinder, new Vector3(0f, 0.9f, 0f), new Vector3(0.3f, 0.14f, 0.3f), Vector3.zero, glass);
                Part(t, PrimitiveType.Cylinder, new Vector3(0f, 1.05f, 0f), new Vector3(0.4f, 0.04f, 0.4f), Vector3.zero, glass);
                Part(t, PrimitiveType.Cylinder, new Vector3(0f, 1.14f, 0f), new Vector3(0.24f, 0.08f, 0.24f), Vector3.zero, cork);
                Part(t, PrimitiveType.Sphere, new Vector3(-0.2f, 0.56f, -0.3f), new Vector3(0.14f, 0.2f, 0.1f), Vector3.zero, shine);
                return root;
            }

            switch (def.Slot)
            {
                case EquipSlot.Helm:
                    Part(t, PrimitiveType.Sphere, new Vector3(0f, 0.5f, 0f), new Vector3(0.9f, 0.95f, 0.92f), Vector3.zero, main);
                    Part(t, PrimitiveType.Sphere, new Vector3(0f, 0.42f, -0.33f), new Vector3(0.52f, 0.56f, 0.32f), Vector3.zero, dark);
                    Part(t, PrimitiveType.Cylinder, new Vector3(0f, 0.06f, 0f), new Vector3(1.0f, 0.05f, 1.0f), Vector3.zero, main);
                    break;
                case EquipSlot.Armor:
                    Part(t, PrimitiveType.Cube, new Vector3(0f, 0.5f, 0f), new Vector3(0.86f, 1.0f, 0.42f), Vector3.zero, main);
                    Part(t, PrimitiveType.Cube, new Vector3(-0.5f, 0.9f, 0f), new Vector3(0.3f, 0.22f, 0.46f), new Vector3(0f, 0f, 18f), main);
                    Part(t, PrimitiveType.Cube, new Vector3(0.5f, 0.9f, 0f), new Vector3(0.3f, 0.22f, 0.46f), new Vector3(0f, 0f, -18f), main);
                    Part(t, PrimitiveType.Cube, new Vector3(0f, 0.97f, -0.02f), new Vector3(0.34f, 0.1f, 0.46f), Vector3.zero, dark);
                    Part(t, PrimitiveType.Cube, new Vector3(0f, 0.22f, 0f), new Vector3(0.9f, 0.12f, 0.46f), Vector3.zero, dark);
                    Part(t, PrimitiveType.Cube, new Vector3(0f, 0.6f, -0.215f), new Vector3(0.05f, 0.7f, 0.02f), Vector3.zero, dark);
                    break;
                case EquipSlot.Boots:
                    foreach (var x in new[] { -0.24f, 0.24f })
                    {
                        Part(t, PrimitiveType.Cube, new Vector3(x, 0.5f, 0.06f), new Vector3(0.32f, 0.62f, 0.36f), Vector3.zero, main);
                        Part(t, PrimitiveType.Cube, new Vector3(x, 0.14f, -0.1f), new Vector3(0.34f, 0.24f, 0.66f), Vector3.zero, main);
                        Part(t, PrimitiveType.Cube, new Vector3(x, 0.01f, -0.1f), new Vector3(0.36f, 0.06f, 0.7f), Vector3.zero, dark);
                        Part(t, PrimitiveType.Cube, new Vector3(x, 0.8f, 0.06f), new Vector3(0.35f, 0.08f, 0.39f), Vector3.zero, dark);
                    }
                    break;
                case EquipSlot.Ring:
                {
                    var torus = Torus(meshes, 0.42f, 0.09f);
                    MeshPart(t, torus, new Vector3(0f, 0.5f, 0f), new Vector3(90f, 0f, 0f), main);
                    Part(t, PrimitiveType.Sphere, new Vector3(0f, 0.98f, 0f), new Vector3(0.24f, 0.24f, 0.24f), Vector3.zero,
                        Mat(materials, new Color(0.3f, 0.85f, 0.75f), 0.9f, new Color(0.05f, 0.2f, 0.18f)));
                    break;
                }
                case EquipSlot.Amulet:
                {
                    var cord = Mat(materials, new Color(0.32f, 0.22f, 0.14f), 0.2f);
                    MeshPart(t, Torus(meshes, 0.42f, 0.025f), new Vector3(0f, 0.75f, 0f), new Vector3(90f, 0f, 0f), cord);
                    if (IsNamed(def, "fang"))
                    {
                        var bone = Mat(materials, new Color(0.93f, 0.89f, 0.78f), 0.4f);
                        Part(t, PrimitiveType.Capsule, new Vector3(0f, 0.12f, 0f), new Vector3(0.16f, 0.26f, 0.16f), new Vector3(0f, 0f, 12f), bone);
                        Part(t, PrimitiveType.Cylinder, new Vector3(0f, 0.36f, 0f), new Vector3(0.12f, 0.03f, 0.12f), Vector3.zero, cord);
                    }
                    else
                    {
                        Part(t, PrimitiveType.Cylinder, new Vector3(0f, 0.22f, 0f), new Vector3(0.42f, 0.04f, 0.42f), new Vector3(90f, 0f, 0f), main);
                        Part(t, PrimitiveType.Sphere, new Vector3(0f, 0.22f, -0.05f), new Vector3(0.18f, 0.18f, 0.12f), Vector3.zero,
                            Mat(materials, new Color(0.35f, 0.75f, 0.95f), 0.9f, new Color(0.05f, 0.15f, 0.2f)));
                    }
                    break;
                }
                default:
                    if (registry.Pouch != null && registry.Pouch.prefab != null)
                    {
                        Object.DestroyImmediate(root);
                        return InstantiateIn(registry.Pouch.prefab, scene);
                    }
                    Part(t, PrimitiveType.Cube, new Vector3(0f, 0.5f, 0f), Vector3.one, Vector3.zero, main);
                    break;
            }
            return root;
        }

        static bool IsNamed(ItemDef def, string word) =>
            (def.name ?? "").IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0
            || (def.id ?? "").IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0;

        /// <summary>Material colour from the item's name (copper, iron, leather…), else by slot.</summary>
        static Color MainColor(ItemDef def)
        {
            if (IsNamed(def, "copper")) return new Color(0.80f, 0.47f, 0.26f);
            if (IsNamed(def, "iron") || IsNamed(def, "steel")) return new Color(0.62f, 0.64f, 0.68f);
            if (IsNamed(def, "gold")) return new Color(0.95f, 0.75f, 0.25f);
            if (IsNamed(def, "leather")) return new Color(0.50f, 0.31f, 0.17f);
            if (IsNamed(def, "padded")) return new Color(0.66f, 0.54f, 0.36f);
            if (IsNamed(def, "cloth")) return new Color(0.80f, 0.73f, 0.58f);
            if (IsNamed(def, "shepherd")) return new Color(0.60f, 0.45f, 0.26f);
            switch (def.Slot)
            {
                case EquipSlot.Ring:
                case EquipSlot.Amulet: return new Color(0.75f, 0.70f, 0.55f);
                default: return new Color(0.55f, 0.42f, 0.28f);
            }
        }

        /// <summary>Red for HP, blue for Spirit, violet for both.</summary>
        static Color PotionColor(ItemDef def)
        {
            var hp = def.use.hp > 0;
            var spirit = def.use.spirit > 0;
            if (hp && spirit) return new Color(0.62f, 0.25f, 0.85f);
            return hp ? new Color(0.88f, 0.16f, 0.13f) : new Color(0.18f, 0.48f, 0.98f);
        }

        static Material Mat(List<Material> materials, Color color, float smoothness, Color emission = default)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var m = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", smoothness);
            if (emission.maxColorComponent > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission);
            }
            materials.Add(m);
            return m;
        }

        static void Part(Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Vector3 euler, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            Place(go, parent, position, scale, euler, material);
        }

        static void MeshPart(Transform parent, Mesh mesh, Vector3 position, Vector3 euler, Material material)
        {
            var go = new GameObject(mesh.name, typeof(MeshFilter), typeof(MeshRenderer));
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            Place(go, parent, position, Vector3.one, euler, material);
        }

        static void Place(GameObject go, Transform parent, Vector3 position, Vector3 scale, Vector3 euler, Material material)
        {
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localEulerAngles = euler;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
        }

        /// <summary>A torus in the XZ plane (ring radius <paramref name="major"/>, tube radius <paramref name="minor"/>).</summary>
        static Mesh Torus(List<Mesh> meshes, float major, float minor, int rings = 40, int sides = 14)
        {
            var vertices = new Vector3[(rings + 1) * (sides + 1)];
            var normals = new Vector3[vertices.Length];
            var triangles = new int[rings * sides * 6];
            for (var r = 0; r <= rings; r++)
            {
                var u = r / (float)rings * Mathf.PI * 2f;
                var centre = new Vector3(Mathf.Cos(u), 0f, Mathf.Sin(u));
                for (var s = 0; s <= sides; s++)
                {
                    var v = s / (float)sides * Mathf.PI * 2f;
                    var normal = centre * Mathf.Cos(v) + Vector3.up * Mathf.Sin(v);
                    var i = r * (sides + 1) + s;
                    vertices[i] = centre * major + normal * minor;
                    normals[i] = normal;
                }
            }
            var k = 0;
            for (var r = 0; r < rings; r++)
                for (var s = 0; s < sides; s++)
                {
                    var a = r * (sides + 1) + s;
                    var b = a + sides + 1;
                    triangles[k++] = a; triangles[k++] = a + 1; triangles[k++] = b;
                    triangles[k++] = b; triangles[k++] = a + 1; triangles[k++] = b + 1;
                }
            var mesh = new Mesh { name = "Torus", hideFlags = HideFlags.HideAndDontSave, vertices = vertices, normals = normals, triangles = triangles };
            mesh.RecalculateBounds();
            meshes.Add(mesh);
            return mesh;
        }

        // ---- Rendering -------------------------------------------------------------------------

        static Camera SetUpStage(Scene scene)
        {
            var lightGo = new GameObject("IconKeyLight");
            SceneManager.MoveGameObjectToScene(lightGo, scene);
            var key = lightGo.AddComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 1.25f;
            key.shadows = LightShadows.None;
            // From the upper left, slightly behind the camera.
            lightGo.transform.rotation = Quaternion.Euler(38f, 32f, 0f);

            var cameraGo = new GameObject("IconCamera");
            SceneManager.MoveGameObjectToScene(cameraGo, scene);
            var camera = cameraGo.AddComponent<Camera>();
            camera.scene = scene;
            camera.cameraType = CameraType.Preview;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            camera.orthographic = true;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.transform.rotation = Quaternion.identity; // looks along +Z
            return camera;
        }

        static Color[] Render(Scene scene, Camera camera, Job job)
        {
            var go = job.Build(scene);
            try
            {
                go.transform.SetPositionAndRotation(Vector3.zero, Presentation(go, job.Pose));
                Frame(camera, go);

                var size = IconSize * Supersample;
                var rt = RenderTexture.GetTemporary(size, size, 24, RenderTextureFormat.ARGB32);
                var previous = RenderTexture.active;
                try
                {
                    camera.targetTexture = rt;
                    camera.Render();
                    RenderTexture.active = rt;
                    var read = new Texture2D(size, size, TextureFormat.RGBA32, false);
                    read.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                    read.Apply();
                    var hi = read.GetPixels();
                    Object.DestroyImmediate(read);
                    return Downsample(AddOutline(hi, size), size);
                }
                finally
                {
                    camera.targetTexture = null;
                    RenderTexture.active = previous;
                    RenderTexture.ReleaseTemporary(rt);
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// Upright things turn 35° and tip 25° towards the camera (a 3/4 view; flat things like the
        /// gold pile tip further). Weapons lie on the screen diagonal, long axis bottom-left to
        /// top-right, broad side facing the camera, flat blades rolled a little so they read as solid.
        /// </summary>
        static Quaternion Presentation(GameObject go, Pose pose)
        {
            var bounds = LocalBounds(go);
            var size = bounds.size;
            if (pose == Pose.Upright)
            {
                var flat = size.y < 0.4f * Mathf.Max(size.x, size.z);
                return Quaternion.Euler(flat ? -45f : -22f, 0f, 0f) * Quaternion.Euler(0f, 35f, 0f);
            }

            var axes = new[] { Vector3.right, Vector3.up, Vector3.forward };
            int longest = 0, shortest = 0;
            for (var i = 1; i < 3; i++)
            {
                if (size[i] > size[longest]) longest = i;
                if (size[i] < size[shortest]) shortest = i;
            }
            if (shortest == longest) shortest = (longest + 1) % 3;
            // Model space: long axis → +Z, thin axis → +Y; then +Z → screen diagonal, +Y → towards the camera.
            var toCanonical = Quaternion.Inverse(Quaternion.LookRotation(axes[longest], axes[shortest]));
            var diagonal = new Vector3(1f, 1f, 0f).normalized;
            var onScreen = Quaternion.LookRotation(diagonal, Vector3.back);
            // Only flat blades roll (so their edge shows); a bow or staff reads best square-on.
            var middle = 3 - longest - shortest;
            var roll = size[shortest] < 0.3f * size[middle] ? 20f : 0f;
            return Quaternion.AngleAxis(roll, diagonal) * onScreen * toCanonical;
        }

        /// <summary>Bounds of every renderer in the object's own space (object at the origin, unrotated).</summary>
        static Bounds LocalBounds(GameObject go)
        {
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var first = true;
            var b = new Bounds();
            foreach (var p in Points(go))
            {
                if (first) { b = new Bounds(p, Vector3.zero); first = false; }
                else b.Encapsulate(p);
            }
            return b;
        }

        /// <summary>Fits the orthographic camera tightly around the object's vertices.</summary>
        static void Frame(Camera camera, GameObject go)
        {
            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            foreach (var p in Points(go))
            {
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }
            var half = Mathf.Max(max.x - min.x, max.y - min.y) * 0.5f;
            var usable = (IconSize - 2f * Padding - 2f * OutlineWidth) / IconSize;
            camera.orthographicSize = Mathf.Max(0.001f, half / usable);
            var depth = max.z - min.z;
            camera.transform.position = new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, min.z - 1f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = depth + 2f;
        }

        /// <summary>World-space vertices of every mesh (bounds corners when a mesh isn't readable).</summary>
        static IEnumerable<Vector3> Points(GameObject go)
        {
            foreach (var renderer in go.GetComponentsInChildren<Renderer>())
            {
                var m = renderer.transform.localToWorldMatrix;
                Mesh mesh = null;
                if (renderer is SkinnedMeshRenderer skinned) mesh = skinned.sharedMesh;
                else
                {
                    var filter = renderer.GetComponent<MeshFilter>();
                    if (filter != null) mesh = filter.sharedMesh;
                }
                if (mesh == null) continue;
                if (mesh.isReadable)
                {
                    var vertices = mesh.vertices;
                    var step = Math.Max(1, vertices.Length / 20000);
                    for (var i = 0; i < vertices.Length; i += step) yield return m.MultiplyPoint3x4(vertices[i]);
                }
                else
                {
                    var b = mesh.bounds;
                    for (var i = 0; i < 8; i++)
                        yield return m.MultiplyPoint3x4(b.center + Vector3.Scale(b.extents,
                            new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                }
            }
        }

        /// <summary>Puts a dark rim under the icon: alpha dilated by the outline width (separable max filter).</summary>
        static Color[] AddOutline(Color[] src, int size)
        {
            var radius = Mathf.RoundToInt(OutlineWidth * Supersample);
            var alpha = new float[src.Length];
            for (var i = 0; i < src.Length; i++) alpha[i] = src[i].a;
            var tmp = new float[src.Length];
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var a = 0f;
                    for (var dx = Math.Max(0, x - radius); dx <= Math.Min(size - 1, x + radius); dx++) a = Math.Max(a, alpha[y * size + dx]);
                    tmp[y * size + x] = a;
                }
            var result = new Color[src.Length];
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var a = 0f;
                    for (var dy = Math.Max(0, y - radius); dy <= Math.Min(size - 1, y + radius); dy++) a = Math.Max(a, tmp[dy * size + x]);
                    var i = y * size + x;
                    var under = OutlineColor.a * a;
                    var top = src[i];
                    // Icon over rim.
                    var outA = top.a + under * (1f - top.a);
                    if (outA <= 0f) { result[i] = new Color(0f, 0f, 0f, 0f); continue; }
                    var rgb = ((Vector4)top * top.a + (Vector4)OutlineColor * under * (1f - top.a)) / outA;
                    result[i] = new Color(rgb.x, rgb.y, rgb.z, outA);
                }
            return result;
        }

        /// <summary>Box-filters the supersampled render down to the icon size, weighting colour by alpha.</summary>
        static Color[] Downsample(Color[] hi, int hiSize)
        {
            var result = new Color[IconSize * IconSize];
            var n = Supersample * Supersample;
            for (var y = 0; y < IconSize; y++)
                for (var x = 0; x < IconSize; x++)
                {
                    float r = 0f, g = 0f, b = 0f, a = 0f;
                    for (var sy = 0; sy < Supersample; sy++)
                        for (var sx = 0; sx < Supersample; sx++)
                        {
                            var c = hi[(y * Supersample + sy) * hiSize + x * Supersample + sx];
                            r += c.r * c.a; g += c.g * c.a; b += c.b * c.a; a += c.a;
                        }
                    result[y * IconSize + x] = a > 0f ? new Color(r / a, g / a, b / a, a / n) : new Color(0f, 0f, 0f, 0f);
                }
            return result;
        }

        // ---- Atlas -----------------------------------------------------------------------------

        static ItemModelRegistry.IconEntry[] WriteAtlas(List<Job> jobs, List<Color[]> pixels, out Texture2D atlas)
        {
            var columns = Mathf.NextPowerOfTwo(Mathf.CeilToInt(Mathf.Sqrt(jobs.Count)));
            var rows = Mathf.NextPowerOfTwo(Mathf.CeilToInt(jobs.Count / (float)columns));
            var width = columns * IconSize;
            var height = rows * IconSize;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var clear = new Color[width * height];
            texture.SetPixels(clear);

            var entries = new ItemModelRegistry.IconEntry[jobs.Count];
            for (var i = 0; i < jobs.Count; i++)
            {
                var x = i % columns * IconSize;
                var y = (rows - 1 - i / columns) * IconSize; // first icon top-left
                texture.SetPixels(x, y, IconSize, IconSize, pixels[i]);
                entries[i] = new ItemModelRegistry.IconEntry
                {
                    key = jobs[i].Key,
                    uv = new Rect(x / (float)width, y / (float)height, IconSize / (float)width, IconSize / (float)height),
                };
            }
            texture.Apply();

            var fullPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), AtlasPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllBytes(fullPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(AtlasPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(AtlasPath);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = Mathf.Max(width, height);
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.isReadable = false;
            importer.SaveAndReimport();

            atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
            return entries;
        }
    }
}
