using UnityEngine;

namespace Sovereign.Presentation
{
    /// <summary>
    /// Keeping the buildings out of the sea. A town spreads outward from its city
    /// site, so even a site that is safely inland can throw a plot across a coastline
    /// - which reads as a bug however right the coordinates were. Each plot is tested
    /// against the coastline mesh and walked back toward its own town until it is
    /// standing on something.
    /// </summary>
    public partial class CityscapeView
    {
        Vector3[] _landVertices;
        int[] _landTriangles;

        /// <summary>Diagnostic: is this WORLD point on land?</summary>
        public bool TestLand(Vector3 worldPoint) { return IsLand(worldPoint); }

        public bool HasLandMesh { get { return _landVertices != null && _landTriangles != null; } }

        /// <summary>Buildings standing in the sea right now. The smoke test demands zero.</summary>
        public int BuildingsAtSea
        {
            get
            {
                if (!HasLandMesh) return -1;
                int wet = 0;
                foreach (Transform site in transform)
                {
                    Transform cityscape = site.name.StartsWith("CitySite_") ? site.Find("Cityscape") : null;
                    if (cityscape == null && site.name == "Cityscape") cityscape = site;
                    if (cityscape == null) continue;
                    foreach (Transform building in cityscape)
                        if (building.gameObject.activeSelf && !IsLand(building.position)) wet++;
                }
                return wet;
            }
        }

        void CacheLand()
        {
            if (landMesh == null) return;
            _landVertices = landMesh.vertices;
            _landTriangles = landMesh.triangles;
        }

        /// <summary>Finds dry ground for one plot: first by pulling it back toward its
        /// own town, and failing that by looking outward for the nearest land.</summary>
        Vector3 OnDryLand(Transform site, Vector3 local)
        {
            if (_landVertices == null || _landTriangles == null) return local;
            if (IsLand(site.TransformPoint(local))) return local;

            // Keep the town together if that is enough.
            for (int step = 1; step <= 6; step++)
            {
                Vector3 candidate = Vector3.Lerp(local, Vector3.zero, step / 6f);
                if (IsLand(site.TransformPoint(candidate))) return candidate;
            }

            // The town centre itself can be offshore - an island, a river mouth, a
            // coastline the 110m outline drew tighter than the real one. Pulling inward
            // cannot help there, so look outward for the nearest ground.
            for (float radius = 0.15f; radius <= 1.5f; radius += 0.15f)
                for (int step = 0; step < 12; step++)
                {
                    float angle = step * Mathf.PI * 2f / 12f;
                    Vector3 candidate = local + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
                    if (IsLand(site.TransformPoint(candidate))) return candidate;
                }

            return local;
        }

        bool IsLand(Vector3 worldPoint)
        {
            for (int t = 0; t + 2 < _landTriangles.Length; t += 3)
            {
                Vector3 a = _landVertices[_landTriangles[t]];
                Vector3 b = _landVertices[_landTriangles[t + 1]];
                Vector3 c = _landVertices[_landTriangles[t + 2]];

                // Ear clipping leaves zero-area slivers, and every point on earth is
                // "inside" one of those - skipping them is what makes this test mean
                // anything at all.
                float area = Side(a, b, c);
                if (area > -1e-5f && area < 1e-5f) continue;

                float d1 = Side(worldPoint, a, b), d2 = Side(worldPoint, b, c), d3 = Side(worldPoint, c, a);
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
