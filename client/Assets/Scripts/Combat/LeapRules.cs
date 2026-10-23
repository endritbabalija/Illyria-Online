using System.Numerics;
using Wuxia.Data;

namespace Wuxia.Combat
{
    public enum LeapCheck { Ok, NotALedge, TooFar, TooHigh, TooLow }

    /// <summary>
    /// Whether a Drangue Leap may land at a spot: only on a marked ledge, within reach and within the
    /// rise and drop limits. Pure C# so the server can check every landing in M4 and the leap can't be
    /// used to teleport. Positions are world points with Y up.
    /// </summary>
    public static class LeapRules
    {
        const float Epsilon = 1e-4f;

        public static LeapCheck Check(LeapData rules, Vector3 from, Vector3 to, bool onMarkedLedge)
        {
            if (!onMarkedLedge) return LeapCheck.NotALedge;
            if (SkillShapes.FlatDistance(from, to) > rules.maxDistance + Epsilon) return LeapCheck.TooFar;
            var rise = to.Y - from.Y;
            if (rise > rules.maxRise + Epsilon) return LeapCheck.TooHigh;
            if (-rise > rules.maxDrop + Epsilon) return LeapCheck.TooLow;
            return LeapCheck.Ok;
        }
    }
}
