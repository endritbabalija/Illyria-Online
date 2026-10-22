using UnityEngine;
using Wuxia.Player;

namespace Wuxia.UI
{
    /// <summary>Spinning ring on the ground under the locked target.</summary>
    public class TargetMarker : MonoBehaviour
    {
        [SerializeField] PlayerCombat player;
        [SerializeField] GameObject ring;
        [SerializeField] float spinSpeed = 90f;

        void LateUpdate()
        {
            var target = player.Target;
            if (ring.activeSelf != (target != null)) ring.SetActive(target != null);
            if (target == null) return;
            transform.position = target.transform.position + Vector3.up * 0.03f;
            transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        }
    }
}
