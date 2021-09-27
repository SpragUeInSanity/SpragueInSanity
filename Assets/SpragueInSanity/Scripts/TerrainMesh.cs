using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SpragueInSanity
{
    //Builds a mesh that mirrors the actual terrain for a give section.
    //REFERENCE: https://stackoverflow.com/questions/62442429/best-way-to-get-shape-of-terrain-at-1x1-size-for-road-system BY derHugo.
    public class TerrainMesh
    {
        private Mesh mesh;
        public Mesh GetAreaMesh(Terrain terrain, Vector3 startPosition, float width, float depth)
        {
            float stepX = 1;
            float stepZ = 1;
            // iterate the size in X and Z direction to get the target vertices
            // This crates a vertex grid with given terrain heights on according positions
            Vector3[] vertices = new Vector3[(int)((width + 1) * (depth + 1))];
            Vector2[] uv = new Vector2[vertices.Length];
            Vector4[] tangents = new Vector4[vertices.Length];
            Vector4 tangent = new Vector4(0f, 1f, 0f, -1f);

            for (int i = 0, z = 0; z <= depth; z++)
            {
                for (int x = 0; x <= width; x++, i++)
                {
                    Vector3 position = startPosition + Vector3.forward * stepZ * z + Vector3.right * stepX * x;
                    position.y = terrain.SampleHeight(position);
                    vertices[i] = new Vector3(x, position.y, z);

                    uv[i] = new Vector2((float)x / width, (float)z / depth);
                    tangents[i] = tangent;
                }
            }

            // Procedural grid generation taken from https://catlikecoding.com/unity/tutorials/procedural-grid
            // This generates the triangles for the given vertex grid
            int[] triangles = new int[(int)(depth * width * 6)];
            for (int ti = 0, vi = 0, y = 0; y < depth; y++, vi++)
            {
                for (int x = 0; x < width; x++, ti += 6, vi++)
                {
                    triangles[ti] = vi;
                    triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                    triangles[ti + 4] = triangles[ti + 1] = (int)(vi + width + 1);
                    triangles[ti + 5] = (int)(vi + width + 2);
                }
            }

            // Finally create the mesh and fill in the data
            mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.tangents = tangents;
            mesh.RecalculateNormals();

            return mesh;
        }
    }
}
