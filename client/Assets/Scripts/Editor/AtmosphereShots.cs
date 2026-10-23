using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Wuxia.World;

namespace Wuxia.EditorTools
{
    /// <summary>
    /// Renders Oak Valley from the game camera's point of view at fixed spots, each in its area's mood, to
    /// compare against the key art in docs/art/oak-valley/. Edit mode only (the Editor's Mobile quality
    /// level, post-processing on, no occluder fade); the player is moved for each shot and put back.
    /// Writes <c>Temp/Screenshots/&lt;tag&gt;-&lt;spot&gt;.jpg</c> at 1560×720 (a 2340×1080 phone at two thirds).
    /// Menu: Wuxia → World → Capture Atmosphere Shots; MCP: <c>return Wuxia.EditorTools.AtmosphereShots.Capture("v2");</c>
    /// </summary>
    public static class AtmosphereShots
    {
        // Spot, ground x/z, camera yaw and pitch (the game's default pitch is 28°, 8 m behind a 1.6 m pivot), area.
        static readonly (string name, float x, float z, float yaw, float pitch, string area)[] Spots =
        {
            ("han", -4f, -176f, 10f, 12f, "The Han"),
            ("meadow", 6f, -128f, 20f, 14f, "Meadow"),
            ("meadow-sun", 4f, -150f, 292f, 2f, "Meadow"),
            ("spring", -48f, -30f, -35f, 28f, "Dried Spring"),
            ("forest", 4f, -40f, 0f, 14f, "Oak Forest"),
            ("valley", 10f, 90f, 10f, 14f, "Upper Valley"),
            ("camp", -56f, 106f, -95f, 18f, "Bandit Camp"),
            ("campfire", -64f, 98f, -60f, 24f, "Bandit Camp"),
            ("den", 66f, 158f, 70f, 16f, "Wolf Den"),
            ("north", 0f, 186f, 0f, 12f, "Road to Serpent Marsh"),
        };

        [MenuItem("Wuxia/World/Capture Atmosphere Shots")]
        static void CaptureMenu() => Debug.Log(Capture("shot"));

        public static string Capture(string tag, params string[] only)
        {
            if (EditorApplication.isPlaying) return "Stop Play mode first.";
            var atmosphere = Object.FindFirstObjectByType<ZoneAtmosphere>();
            var player = GameObject.Find("Player");
            if (atmosphere == null || player == null || Camera.main == null) return "Open OakValley.unity first.";

            var dir = Path.GetFullPath("Temp/Screenshots");
            Directory.CreateDirectory(dir);
            var keep = (player.transform.position, player.transform.rotation);
            var go = new GameObject("AtmosphereShotCamera") { hideFlags = HideFlags.HideAndDontSave };
            var cam = go.AddComponent<Camera>();
            cam.CopyFrom(Camera.main);
            go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            var rt = new RenderTexture(1560, 720, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            var mask = ~((1 << 8) | (1 << 9) | (1 << OakValleyBuilder.ObstacleLayer));
            var log = new StringBuilder();
            try
            {
                foreach (var spot in Spots)
                {
                    if (only.Length > 0 && System.Array.IndexOf(only, spot.name) < 0) continue;
                    atmosphere.ApplyInstant(atmosphere.ProfileFor(spot.area));
                    var ground = new Vector3(spot.x, 200f, spot.z);
                    if (Physics.Raycast(ground, Vector3.down, out var hit, 400f, mask)) ground = hit.point;
                    player.transform.SetPositionAndRotation(ground, Quaternion.Euler(0f, spot.yaw, 0f));
                    var rotation = Quaternion.Euler(spot.pitch, spot.yaw, 0f);
                    go.transform.SetPositionAndRotation(ground + Vector3.up * 1.6f + rotation * Vector3.back * 8f, rotation);

                    cam.targetTexture = rt;
                    cam.Render();
                    cam.Render(); // the second frame has the volume stack and bloom history settled
                    RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                    RenderTexture.active = null;
                    var path = Path.Combine(dir, $"{tag}-{spot.name}.jpg");
                    File.WriteAllBytes(path, tex.EncodeToJPG(90));
                    log.AppendLine(path);
                }
            }
            finally
            {
                player.transform.SetPositionAndRotation(keep.position, keep.rotation);
                atmosphere.ApplyInstant(atmosphere.DefaultProfile);
                cam.targetTexture = null;
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(tex);
                rt.Release();
                Object.DestroyImmediate(rt);
            }
            return log.ToString();
        }
    }
}
