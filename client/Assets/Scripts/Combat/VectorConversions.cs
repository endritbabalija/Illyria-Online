namespace Wuxia.Combat
{
    /// <summary>Bridges Unity vectors to the System.Numerics ones the pure rule classes use.</summary>
    public static class VectorConversions
    {
        public static System.Numerics.Vector3 ToNumerics(this UnityEngine.Vector3 v) => new System.Numerics.Vector3(v.x, v.y, v.z);

        /// <summary>The ground-plane part (X, Z) of a world direction.</summary>
        public static System.Numerics.Vector2 ToFlat(this UnityEngine.Vector3 v) => new System.Numerics.Vector2(v.x, v.z);
    }
}
