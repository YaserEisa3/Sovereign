using System.Collections.Generic;
using UnityEngine;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// The three meshes the map is made of: filled land, coastline outlines and the
    /// graticule. Each is ONE combined mesh asset rather than ninety-four objects,
    /// so the hierarchy stays readable.
    /// </summary>
    public static partial class ProjectBuilder
    {
        static void BuildLand(List<List<Vector2>> rings, Transform mapRoot)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            foreach (List<Vector2> ring in rings)
            {
                int offset = vertices.Count;
                foreach (Vector2 point in ring) vertices.Add(new Vector3(point.x, point.y, 0f));
                PolygonTriangulator.Triangulate(ring, triangles, offset);
            }

            Mesh mesh = new Mesh { name = "Mesh_Land" };
            mesh.indexFormat = vertices.Count > 65000
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();

            Mesh saved = SaveMesh(mesh, "Mesh_Land");
            MeshObject("Land", saved, MatSprite("Mat_MapLand", new Color32(0x1c, 0x27, 0x36, 0xff)), mapRoot, LandZ);
        }

        static void BuildCoastlines(List<List<Vector2>> rings, Transform mapRoot)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> indices = new List<int>();

            foreach (List<Vector2> ring in rings)
            {
                int offset = vertices.Count;
                foreach (Vector2 point in ring) vertices.Add(new Vector3(point.x, point.y, 0f));
                for (int i = 0; i < ring.Count; i++)
                {
                    indices.Add(offset + i);
                    indices.Add(offset + (i + 1) % ring.Count);
                }
            }

            Mesh mesh = new Mesh { name = "Mesh_Coastlines" };
            mesh.indexFormat = vertices.Count > 65000
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            mesh.SetIndices(indices, MeshTopology.Lines, 0);
            mesh.RecalculateBounds();

            Mesh saved = SaveMesh(mesh, "Mesh_Coastlines");
            MeshObject("Coastlines", saved, MatSprite("Mat_MapCoast", new Color32(0x4a, 0x6b, 0x8f, 0xff)), mapRoot, CoastZ);
        }

        /// <summary>Latitude and longitude lines. In Mercator both families are straight,
        /// which is the whole reason sailors liked it.</summary>
        static void BuildGraticule(Transform mapRoot)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> indices = new List<int>();

            for (int lon = -180; lon <= 180; lon += 30)
            {
                Vector2 bottom = MapProjection.Project(lon, -MapProjection.LatitudeLimit);
                Vector2 top = MapProjection.Project(lon, MapProjection.LatitudeLimit);
                AddSegment(vertices, indices, bottom, top);
            }

            for (int lat = -80; lat <= 80; lat += 20)
            {
                Vector2 left = MapProjection.Project(-180f, lat);
                Vector2 right = MapProjection.Project(180f, lat);
                AddSegment(vertices, indices, left, right);
            }

            // The equator, drawn as its own slightly brighter line, is a useful anchor.
            Mesh mesh = new Mesh { name = "Mesh_Graticule" };
            mesh.SetVertices(vertices);
            mesh.SetIndices(indices, MeshTopology.Lines, 0);
            mesh.RecalculateBounds();

            Mesh saved = SaveMesh(mesh, "Mesh_Graticule");
            MeshObject("Graticule", saved, MatSprite("Mat_MapGrid", new Color32(0x16, 0x1f, 0x2b, 0xff)), mapRoot, GraticuleZ);
        }

        static void AddSegment(List<Vector3> vertices, List<int> indices, Vector2 from, Vector2 to)
        {
            indices.Add(vertices.Count);
            vertices.Add(new Vector3(from.x, from.y, 0f));
            indices.Add(vertices.Count);
            vertices.Add(new Vector3(to.x, to.y, 0f));
        }
    }
}
