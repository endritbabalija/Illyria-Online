using System;
using Nakama;
using UnityEngine;

namespace Wuxia.Net
{
    /// <summary>
    /// M0 smoke test: logs into Nakama with device auth when Play starts.
    /// Drop it on any GameObject in the scene; check the Console for "[Nakama]".
    /// </summary>
    public class NakamaLoginTest : MonoBehaviour
    {
        const string DeviceIdKey = "nakama.device_id";

        [SerializeField] string scheme = "http";
        // 127.0.0.1 works in the Editor. On a phone, use the dev PC's LAN IP.
        [SerializeField] string host = "127.0.0.1";
        [SerializeField] int port = 7350;
        [SerializeField] string serverKey = "defaultkey";

        async void Start()
        {
            var client = new Client(scheme, host, port, serverKey, UnityWebRequestAdapter.Instance);
            var deviceId = GetDeviceId();

            try
            {
                var session = await client.AuthenticateDeviceAsync(deviceId);
                var account = await client.GetAccountAsync(session);
                Debug.Log($"[Nakama] Logged in as '{account.User.Username}' (user {session.UserId}, new account: {session.Created}) at {scheme}://{host}:{port}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Nakama] Login failed at {scheme}://{host}:{port}: {e.Message}");
            }
        }

        // Nakama needs a stable ID of 10–128 chars; fall back to a stored GUID where the platform has none.
        static string GetDeviceId()
        {
            var id = PlayerPrefs.GetString(DeviceIdKey, string.Empty);
            if (!string.IsNullOrEmpty(id)) return id;

            id = SystemInfo.deviceUniqueIdentifier;
            if (id == SystemInfo.unsupportedIdentifier || id.Length < 10) id = Guid.NewGuid().ToString();

            PlayerPrefs.SetString(DeviceIdKey, id);
            PlayerPrefs.Save();
            return id;
        }
    }
}
