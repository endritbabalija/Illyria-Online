using UnityEngine;

namespace Wuxia.Player
{
    /// <summary>
    /// Turns a held weapon model from its carry pose (the prefab root's local pose under the weapon
    /// pivot) to a second grip while the player attacks. The bow needs it: carried upright at the side,
    /// but the drawing hand turns the fist, so on the draw it gets a grip that stands it upright with
    /// its back to the target. Away from a character (item icons, ground drops) it does nothing. Works under other players too (<see cref="IAttackPose"/>).
    /// </summary>
    public class WeaponActionGrip : MonoBehaviour
    {
        [Tooltip("Local position under the weapon pivot while attacking.")]
        [SerializeField] Vector3 actionPosition;
        [Tooltip("Local rotation under the weapon pivot while attacking.")]
        [SerializeField] Vector3 actionEuler;

        IAttackPose player;
        Vector3 carryPosition;
        Quaternion carryRotation;
        Quaternion actionRotation;

        void Start()
        {
            player = GetComponentInParent<IAttackPose>();
            carryPosition = transform.localPosition;
            carryRotation = transform.localRotation;
            actionRotation = Quaternion.Euler(actionEuler);
        }

        void LateUpdate()
        {
            if (player == null) return;
            var w = player.AttackWeight;
            transform.SetLocalPositionAndRotation(
                Vector3.Lerp(carryPosition, actionPosition, w),
                Quaternion.Slerp(carryRotation, actionRotation, w));
        }
    }
}
