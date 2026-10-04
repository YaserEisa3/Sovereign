using UnityEditor;
using UnityEngine;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// GDD 3.3 and 18. Where each country's towns stand. Placed by hand in real
    /// coordinates rather than scattered at runtime: a country's cities belong on its
    /// coasts, rivers and plains, and no sampling function knows that.
    ///
    /// The home nation gets five, because it is the map the player stares at; the
    /// rest get three each.
    /// </summary>
    public static partial class ProjectBuilder
    {
        // Longitude, latitude. Indexed to match NationObjectNames.
        static readonly Vector2[][] CitySiteCoordinates =
        {
            new[] {                                   // Home - North America
                new Vector2(-120f, 38f),              // west coast
                new Vector2(-97f, 33f),               // gulf south
                new Vector2(-88f, 42f),               // the industrial lakes
                new Vector2(-78f, 41f),               // east coast
                new Vector2(-105f, 45f)               // northern plains
            },
            new[] {                                   // Euroland
                new Vector2(2f, 48f), new Vector2(13f, 52f), new Vector2(-4f, 40f)
            },
            new[] {                                   // Sino-Pacific
                new Vector2(121f, 31f), new Vector2(113f, 23f), new Vector2(126f, 37f)
            },
            new[] {                                   // Petro-Gulf
                new Vector2(47f, 29f), new Vector2(55f, 25f), new Vector2(44f, 21f)
            },
            new[] {                                   // Emerging South
                new Vector2(13f, -4f), new Vector2(30f, 1f), new Vector2(18f, 9f)
            },
            new[] {                                   // Island Finance
                new Vector2(-77f, 25f), new Vector2(-66f, 18f), new Vector2(-80f, 22f)
            },
            new[] {                                   // Northern Alliance
                new Vector2(18f, 59f), new Vector2(11f, 63f), new Vector2(25f, 66f)
            }
        };

        /// <summary>Hangs the city sites off each nation, in the nation's own space, and
        /// pulls any that missed onto dry land.</summary>
        static void BuildCitySites(Transform[] nations)
        {
            Mesh land = AssetDatabase.LoadAssetAtPath<Mesh>(MapDataPath + "Mesh_Land.asset");
            Vector3[] vertices = land == null ? null : land.vertices;
            int[] triangles = land == null ? null : land.triangles;

            for (int i = 0; i < nations.Length && i < CitySiteCoordinates.Length; i++)
            {
                Vector2 nationPoint = MapProjection.Project(NationCoordinates[i].x, NationCoordinates[i].y);

                for (int site = 0; site < CitySiteCoordinates[i].Length; site++)
                {
                    Vector2 coordinate = CitySiteCoordinates[i][site];
                    Vector2 projected = SnapToLand(MapProjection.Project(coordinate.x, coordinate.y), vertices, triangles);

                    GameObject marker = Child("CitySite_" + site, nations[i]);
                    marker.transform.localPosition = new Vector3(projected.x - nationPoint.x, projected.y - nationPoint.y, 0f);
                }
            }
        }

        /// <summary>
        /// A city in the sea reads as a bug even when the coordinates were right: the
        /// coastline mesh is a simplified 110m outline, so a real coastal city can sit
        /// a degree offshore of it. Any site that misses the land is walked inland in a
        /// widening spiral until it is standing on something.
        /// </summary>
        static Vector2 SnapToLand(Vector2 point, Vector3[] vertices, int[] triangles)
        {
            if (vertices == null || triangles == null || OnLand(point, vertices, triangles)) return point;

            for (float radius = 0.2f; radius <= 3f; radius += 0.2f)
                for (int step = 0; step < 16; step++)
                {
                    float angle = step * Mathf.PI * 2f / 16f;
                    Vector2 candidate = point + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    if (OnLand(candidate, vertices, triangles)) return candidate;
                }

            Debug.LogWarning("Sovereign: city site at " + point + " is nowhere near land; left where it was.");
            return point;
        }

        static bool OnLand(Vector2 point, Vector3[] vertices, int[] triangles)
        {
            for (int t = 0; t + 2 < triangles.Length; t += 3)
            {
                Vector3 a = vertices[triangles[t]], b = vertices[triangles[t + 1]], c = vertices[triangles[t + 2]];
                // Ear clipping leaves slivers of no area behind, and every point is
                // "inside" one of those - which made the whole ocean read as land.
                float area = Side(a, b, c);
                if (area > -1e-5f && area < 1e-5f) continue;

                float d1 = Side(point, a, b), d2 = Side(point, b, c), d3 = Side(point, c, a);
                bool negative = d1 < 0f || d2 < 0f || d3 < 0f;
                bool positive = d1 > 0f || d2 > 0f || d3 > 0f;
                if (!(negative && positive)) return true;
            }
            return false;
        }

        static float Side(Vector3 p, Vector3 a, Vector3 b)
        {
            return (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
        }
    }
}
