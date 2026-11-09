using System.Numerics;

namespace MatchZy
{
    // Which highlighted spawn (.showspawns) a player is looking at. This file must not depend on CounterStrikeSharp so that it can be unit tested (see tests/).
    public static class SpawnPickLogic
    {
        // Height of the beam .showspawns draws above each spawn.
        public const float BeamHeight = 100f;
        // How far from the beam the view ray may pass, a bit more than a player's width so the beam is easy to hit.
        public const float AimRadius = 24f;
        // How far away a spawn can be picked, in units (about 8 m at 1 unit = 1 inch), so use only reaches nearby spawns.
        public const float MaxDistance = 320f;

        // Forward vector for Source angles in degrees (pitch looks down when positive).
        public static Vector3 Forward(float pitch, float yaw)
        {
            float p = pitch * MathF.PI / 180f;
            float y = yaw * MathF.PI / 180f;
            return new Vector3(MathF.Cos(p) * MathF.Cos(y), MathF.Cos(p) * MathF.Sin(y), -MathF.Sin(p));
        }

        // Returns the index of the spawn whose beam the view ray from eye along forward passes within radius of, or -1.
        // When several beams are that close, the nearest one along the view wins, so a beam behind another isn't picked.
        // A beam the player is standing in (e.g. right after teleporting to that spawn) is skipped, otherwise it would always win.
        public static int FindAimedSpawn(Vector3 eye, Vector3 forward, IReadOnlyList<Vector3> spawns,
            float beamHeight = BeamHeight, float radius = AimRadius, float maxDistance = MaxDistance)
        {
            if (forward.LengthSquared() < 1e-6f) return -1;
            Vector3 dir = Vector3.Normalize(forward);
            Vector3 up = new(0, 0, beamHeight);

            int best = -1;
            float bestAlong = float.MaxValue;
            for (int i = 0; i < spawns.Count; i++)
            {
                float dx = eye.X - spawns[i].X, dy = eye.Y - spawns[i].Y;
                if (dx * dx + dy * dy <= radius * radius) continue;
                var (distance, along) = RayToSegment(eye, dir, maxDistance, spawns[i], spawns[i] + up);
                if (distance <= radius && along < bestAlong)
                {
                    best = i;
                    bestAlong = along;
                }
            }
            return best;
        }

        // Closest distance between the ray segment (origin, origin + dir * length) and the segment (a, b), and how far along the
        // ray that closest point is. dir must be normalized.
        private static (float Distance, float Along) RayToSegment(Vector3 origin, Vector3 dir, float length, Vector3 a, Vector3 b)
        {
            Vector3 seg = b - a;
            Vector3 w = origin - a;
            float segLenSq = Vector3.Dot(seg, seg);
            float dirSeg = Vector3.Dot(dir, seg);
            float dirW = Vector3.Dot(dir, w);
            float segW = Vector3.Dot(seg, w);
            float denom = segLenSq - dirSeg * dirSeg; // |dir|^2 * |seg|^2 - (dir.seg)^2 with |dir| = 1

            float t; // along the ray
            float s; // along the segment, 0..1
            if (denom < 1e-6f)
            {
                // Looking straight along the beam: take the end of the beam nearest the eye.
                t = 0;
                s = Math.Clamp(segW / segLenSq, 0f, 1f);
            }
            else
            {
                t = Math.Clamp((dirSeg * segW - segLenSq * dirW) / denom, 0f, length);
                s = Math.Clamp((segW + dirSeg * t) / segLenSq, 0f, 1f);
            }
            // Re-project the ray onto the clamped segment point.
            t = Math.Clamp(Vector3.Dot(a + seg * s - origin, dir), 0f, length);
            s = Math.Clamp(Vector3.Dot(origin + dir * t - a, seg) / segLenSq, 0f, 1f);

            Vector3 closestRay = origin + dir * t;
            Vector3 closestSeg = a + seg * s;
            return (Vector3.Distance(closestRay, closestSeg), t);
        }
    }
}
