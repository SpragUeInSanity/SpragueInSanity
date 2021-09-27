using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SpragueInSanity
{
	/// <summary>
	/// DIVIDES INPUT MESH TO CREATE MORE DETAIL OR VERTICES BY ADDING TRIANGLES INSIDE EXISTING TRIANGLES.
	/// THIS GIVE A MESH MORE GRANULARITY IF NEEDED FOR FINE GRAINED SMOOTHING OR NOISE ALGORITHMS.
	/// REFERENCE: https://wiki.unity3d.com/index.php/MeshSubdivision: By realm_1
	/// </summary>

	public class MeshSubdivide
    {
		private Mesh newMesh;
		private Vector3[] origVertices;
		private int[] origTriangles;
		private Vector2[] origUvs;
		private Vector3[] origNormals;

		private List<Vector3> newVertices;
		private List<int> newTriangles;
		private List<Vector2> newUvs;
		private List<Vector3> newNormals;

		/// <summary>
		/// CALLED BY EXTERNAL CLASS TO DIVIDE MESH INTO FINER GRANULARITY TO SUPPORT FINE GRAINED SMOOTHING
		/// OR NOISE ALGORITHMS.  PASS IN MESH, NUMBER OF PASSES TO SUBDIVIDE. 
		/// </summary>
		/// <param name="origMesh"></param>
		/// <param name="subdivisions"></param>
		/// <returns></returns>
		public Mesh Subdivide(Mesh origMesh, int subdivisions)
		{
			origTriangles = origMesh.triangles;
			origVertices = origMesh.vertices;
			origNormals = origMesh.normals;
			origUvs = origMesh.uv;


			//COPY ORIGINAL MESH INTO NEW STRUCTURES EXCEPT FOR TRIANGLES.  WE NEED TO PRESERVE AND REBUILD THE ORDER
			//WHEN WE SUBDIVIDE
			newVertices = new List<Vector3>(origVertices);
			newUvs = new List<Vector2>(origUvs);
			newNormals = new List<Vector3>(origNormals);

			if (subdivisions < 0)
			{
				subdivisions = 0;
				Debug.LogWarning("Subdivisions increased to minimum, which is 0.");
			}
			else if (subdivisions > 6)
			{
				subdivisions = 6;
				Debug.LogWarning("Subdivisions decreased to maximum, which is 6.");
			}
			for (int pass = 0; pass < subdivisions; pass++)
			{
				Subdivide();
				origTriangles = newTriangles.ToArray();
				origVertices = newVertices.ToArray();
				origNormals = newNormals.ToArray();
				origUvs = newUvs.ToArray();
			}
			if (subdivisions == 0)
			{
				return origMesh;
			}
			newMesh = new Mesh();
			newMesh.vertices = newVertices.ToArray();
			newMesh.triangles = newTriangles.ToArray();
			newMesh.normals = newNormals.ToArray();
			newMesh.uv = newUvs.ToArray();
			newMesh.RecalculateNormals();


			return newMesh;
		}
		//THIS JUST ADDS A NEW TRIANGLE IN THE MIDDLE OF EXISTING TRIANGLES.  THIS BASICALLY CREATE
		//THREE NEW VERTICES AND FOUR NEW TRIANGLES.
		private void Subdivide()
		{
			newTriangles = new List<int>();  //INITIALIZE TO EMPTY SO WE CAN RECONSTRUCT IN ORDER
											 // CYCLE THROUGH EACH ORIGINAL TRIANGLE, CREATE A NEW
											 // INTERNAL TRIANGLE
			for (int x = 0; x < origTriangles.Length; x += 3)
			{
				int vertexIndex0 = origTriangles[x];
				int vertexIndex1 = origTriangles[x + 1];
				int vertexIndex2 = origTriangles[x + 2];

				Vector3 vertex0 = origVertices[vertexIndex0];
				Vector3 vertex1 = origVertices[vertexIndex1];
				Vector3 vertex2 = origVertices[vertexIndex2];

				Vector2 uv0 = origUvs[vertexIndex0];
				Vector2 uv1 = origUvs[vertexIndex1];
				Vector2 uv2 = origUvs[vertexIndex2];

				Vector3 normal0 = origNormals[vertexIndex0];
				Vector3 normal1 = origNormals[vertexIndex1];
				Vector3 normal2 = origNormals[vertexIndex2];

				//CREATE NEW POINTS 1/2 WAY BETWEEN ORIGINAL POINTS FOR NEW TRIANGLE.  ADD THEM TO VERTEX STRUCTURE
				Vector3 newVertex0 = (vertex0 + vertex1) / 2f;
				Vector3 newVertex1 = (vertex1 + vertex2) / 2f;
				Vector3 newVertex2 = (vertex2 + vertex0) / 2f;

				//THIS FUNCTIONALITY CURRENTLY NOT USED.  ORIGINAL INTENT WAS FOR KEEP EDGES IDENTIFIED
				//FOR CUSTOM SMOOTHER OR DEFORMATION ROUTINE
				//UpdateEdgeExtrudedLookup(vertex0, vertex1, newVertex0);
				//UpdateEdgeExtrudedLookup(vertex1, vertex2, newVertex1);
				//UpdateEdgeExtrudedLookup(vertex2, vertex0, newVertex2);

				int newTriangleVertexIndex = newVertices.Count; // CAPTURE SIZE/INDEX BEFORE ADDING NEW VERTICES SO WE CAN ASSIGN NEW TRIANGLES
				int newVertexIndex0 = newTriangleVertexIndex;
				int newVertexIndex1 = newTriangleVertexIndex + 1;
				int newVertexIndex2 = newTriangleVertexIndex + 2;

				newVertices.Add(newVertex0);
				newVertices.Add(newVertex1);
				newVertices.Add(newVertex2);
				//COPY AND ADJUST NORMALS
				newNormals.Add(((normal0 + normal1) / 2f).normalized);
				newNormals.Add(((normal1 + normal2) / 2f).normalized);
				newNormals.Add(((normal2 + normal0) / 2f).normalized);
				//COPY EXISTING UVS TO NEW
				newUvs.Add((uv0 + uv1) / 2f);
				newUvs.Add((uv1 + uv2) / 2f);
				newUvs.Add((uv2 + uv0) / 2f);
				//NOW, THIS RESULTS IN THE NEW TRIANGLE PLUS THE OTHER 3 FOR EACH CORNER OF THE ORIGINAL TRIANGLE.
				//BUILD AND ADD THEM IN ORDER
				//ORIG:
				//newTriangles.Add(vertexIndex0);  //NOT NEEDED, REPLACED BY NEW TRIANGLES
				//newTriangles.Add(vertexIndex1);  //NOT NEEDED, REPLACED BY NEW TRIANGLES
				//newTriangles.Add(vertexIndex2);  //NOT NEEDED, REPLACED BY NEW TRIANGLES
				//NEW IN ORDER INSIDE ORIGINAL TO BUILD OUT 4 NEW TRIANGLES.
				//IMAGINE LOOKING AT A FLAT TRIANGLE WITH A MIDDLE TRIANGLE IN THE MIDDLE WITH EACH VERTICE INTERSECTING
				//HALF WAY ON SIDE OF OUTSIDE TRIANGLE.  NOW, IMAGINE YOU HAVE 4 TRIANGLES RESULTING FROM THIS:
				//LEFT CORNER--TOP CORNER--RIGHT CORNER--MIDDLE
				//LEFT CORNER:  BUILD IN CLOCKWISE ORDER SO UNITY RENDERS CORRECTLY.
				newTriangles.Add(newVertexIndex2);
				newTriangles.Add(vertexIndex0);
				newTriangles.Add(newVertexIndex0);
				//TOP CORNER:
				newTriangles.Add(newVertexIndex0);
				newTriangles.Add(vertexIndex1);
				newTriangles.Add(newVertexIndex1);
				//RIGHT CORNER:
				newTriangles.Add(newVertexIndex1);
				newTriangles.Add(vertexIndex2);
				newTriangles.Add(newVertexIndex2);
				//MIDDLE:
				newTriangles.Add(newVertexIndex2);
				newTriangles.Add(newVertexIndex0);
				newTriangles.Add(newVertexIndex1);


			}

		}
	}
}
