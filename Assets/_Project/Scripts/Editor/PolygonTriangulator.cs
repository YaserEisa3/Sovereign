using System.Collections.Generic;
using UnityEngine;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// Ear clipping, so coastline rings become filled landmasses. Simplifying the
    /// source data to half a degree can leave a ring slightly self-intersecting, so
    /// this bails out rather than looping forever - a landmass losing a sliver is
    /// better than the Editor hanging.
    /// </summary>
    public static class PolygonTriangulator
    {
        public static void Triangulate(List<Vector2> ring, List<int> triangles, int vertexOffset)
        {
            int count = ring.Count;
            if (count < 3) return;

            List<int> remaining = new List<int>(count);
            if (SignedArea(ring) < 0f)
                for (int i = count - 1; i >= 0; i--) remaining.Add(i);
            else
                for (int i = 0; i < count; i++) remaining.Add(i);

            int guard = count * count;
            while (remaining.Count > 3 && guard-- > 0)
            {
                bool clipped = false;
                for (int i = 0; i < remaining.Count; i++)
                {
                    int previous = remaining[(i + remaining.Count - 1) % remaining.Count];
                    int current = remaining[i];
                    int next = remaining[(i + 1) % remaining.Count];

                    Vector2 a = ring[previous], b = ring[current], c = ring[next];
                    if (Cross(a, b, c) <= 0f) continue;

                    bool encloses = false;
                    for (int k = 0; k < remaining.Count && !encloses; k++)
                    {
                        int other = remaining[k];
                        if (other == previous || other == current || other == next) continue;
                        if (Contains(ring[other], a, b, c)) encloses = true;
                    }
                    if (encloses) continue;

                    triangles.Add(vertexOffset + previous);
                    triangles.Add(vertexOffset + current);
                    triangles.Add(vertexOffset + next);
                    remaining.RemoveAt(i);
                    clipped = true;
                    break;
                }
                if (!clipped) break;
            }

            if (remaining.Count == 3)
            {
                triangles.Add(vertexOffset + remaining[0]);
                triangles.Add(vertexOffset + remaining[1]);
                triangles.Add(vertexOffset + remaining[2]);
            }
        }

        static float SignedArea(List<Vector2> ring)
        {
            float area = 0f;
            for (int i = 0; i < ring.Count; i++)
            {
                Vector2 a = ring[i], b = ring[(i + 1) % ring.Count];
                area += a.x * b.y - b.x * a.y;
            }
            return area * 0.5f;
        }

        static float Cross(Vector2 a, Vector2 b, Vector2 c)
        {
            return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
        }

        static bool Contains(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Cross(a, b, point);
            float d2 = Cross(b, c, point);
            float d3 = Cross(c, a, point);
            bool negative = d1 < 0f || d2 < 0f || d3 < 0f;
            bool positive = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(negative && positive);
        }
    }
}
