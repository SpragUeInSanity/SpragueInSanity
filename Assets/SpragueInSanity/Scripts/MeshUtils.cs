using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SpragueInSanity
{
	/// <summary>
	/// HELPER CLASS TO FIND ALL VERTICES THAT PARTICIPATE IN TRIANGLES FOR A GIVEN VERTEX.  IT ALSO BUILDS A NEW TRIANGLE
	/// FOR GIVEN INPUT SET OF VERTICES.
	/// REFERENCE: https://wiki.unity3d.com/index.php/MeshSmoother: By MarkGX
	///     https://github.com/renangalves/MeshExtrusion: By renangalves
	/// 	https://youtu.be/BVCNDUcnE1o: By Tvtig

	/// </summary>
	public class MeshUtils
	{
		private string createTriangleStartLoc = "TOP";
		public static Hashtable findAdjacentNeighborsLookup(Mesh mesh)
		{
			Hashtable adjacentV = new Hashtable();
			Vector3[] v = mesh.vertices;
			int[] t = mesh.triangles;

			for (int k = 0; k < t.Length; k = k + 3)
			{
				adjacentV = AddAdjacentVertex(adjacentV, v[t[k]].ToString(), v[t[k + 1]]);
				adjacentV = AddAdjacentVertex(adjacentV, v[t[k]].ToString(), v[t[k + 2]]);
				adjacentV = AddAdjacentVertex(adjacentV, v[t[k + 1]].ToString(), v[t[k]]);
				adjacentV = AddAdjacentVertex(adjacentV, v[t[k + 1]].ToString(), v[t[k + 2]]);
				adjacentV = AddAdjacentVertex(adjacentV, v[t[k + 2]].ToString(), v[t[k]]);
				adjacentV = AddAdjacentVertex(adjacentV, v[t[k + 2]].ToString(), v[t[k + 1]]);

			}
			return adjacentV;
		}
		private static Hashtable AddAdjacentVertex(Hashtable adjacentV, string key, Vector3 value)
		{
			if (adjacentV.ContainsKey(key))
			{
				List<Vector3> vertices = (List<Vector3>)adjacentV[key];
				vertices.Add(value);
				adjacentV[key] = vertices;

			}
			else
			{
				List<Vector3> vertices = new List<Vector3>();
				vertices.Add(value);
				adjacentV.Add(key, vertices);
			}

			return adjacentV;

		}
		/// <summary>
		// THIS TAKES AN EDGE PATH REPRESENTED BY PAIRS OF VERTICES AND BUILDS A TRIANGLE USING START VERTEX AS STARTING POINT
		// THIS SUPPORTS BUILDING A SQUARE REPRESENTED BY TWO TRIANGLES IN A SINGLE EDGE PAIR.  THIS IS DONE TO BUILD OUT NEW FACES
		// ON SIDE FACES FOR NEW EXTRUDED OBJECT THAT IS NOTHING MORE THAN NEW MESH REPLICATED BY DISTANCE.
		// NOTE: FROM/TO NEED TO BE REVERSED TO BUILD OUT TRIANGLE IN CLOCKWISE DIRECTION.
		/// </summary>
		/// <param name="indexEdge"></param>
		/// <param name="offsetTopVertex"></param>
		/// <param name="pairedEdgeArray"></param>
		/// <param name="vertexStatusLookup"></param>
		/// <returns></returns>
		public MeshTriangleData CreateNewTriangle(int indexEdge, Vector3 offsetTopVertex, List<Vector3> pairedEdgeArray, Hashtable vertexStatusLookup)
		{
			MeshTriangleData triDirection = new MeshTriangleData();
			Vector3 foundVertex = new Vector3();
			if (createTriangleStartLoc == "TOP")
			{
				foundVertex = pairedEdgeArray[indexEdge + 1];
				VertexStatus vertexStatus = (VertexStatus)vertexStatusLookup[foundVertex.ToString()];
				triDirection.startVertex = foundVertex + offsetTopVertex;
				triDirection.startUV = vertexStatus.uv;
				triDirection.startNormal = vertexStatus.normalLocal;

				triDirection.nextVertex = foundVertex;
				triDirection.nextNormal = -1 * vertexStatus.normalLocal;
				triDirection.nextUV = vertexStatus.uv;

				foundVertex = pairedEdgeArray[indexEdge];
				vertexStatus = (VertexStatus)vertexStatusLookup[foundVertex.ToString()];
				triDirection.endVertex = foundVertex;
				triDirection.endNormal = -1 * vertexStatus.normalLocal;
				triDirection.endUV = vertexStatus.uv;

				triDirection.currentEdgeIndex = indexEdge - 2;
				createTriangleStartLoc = "BOTTOM LEFT";
				return triDirection;
			}
			if (createTriangleStartLoc == "BOTTOM LEFT")
			{
				foundVertex = pairedEdgeArray[indexEdge];
				VertexStatus vertexStatus = (VertexStatus)vertexStatusLookup[foundVertex.ToString()];
				triDirection.startVertex = foundVertex;
				triDirection.startNormal = -1 * vertexStatus.normalLocal;
				triDirection.startUV = vertexStatus.uv;

				triDirection.nextVertex = foundVertex + offsetTopVertex;
				triDirection.nextNormal = vertexStatus.normalLocal;
				triDirection.nextUV = vertexStatus.uv;

				foundVertex = pairedEdgeArray[indexEdge + 1];
				vertexStatus = (VertexStatus)vertexStatusLookup[foundVertex.ToString()];
				triDirection.endVertex = foundVertex + offsetTopVertex;
				triDirection.endNormal = vertexStatus.normalLocal;
				triDirection.endUV = vertexStatus.uv;
				triDirection.currentEdgeIndex = indexEdge;

				createTriangleStartLoc = "TOP";

			}
			return triDirection;
		}
		/// <summary>
		/// BUILD LOOKUP TABLE FOR VERTEX INFORMATION INCUDING NORMALS AND UVS
		/// </summary>
		/// <param name="mesh"></param>
		/// <returns></returns>
		public Hashtable BuildVertexStatusLookup(Mesh mesh)
		{
			Hashtable vertexStatusLookup = new Hashtable();
			for (int x = 0; x < mesh.vertices.Length; x++)
			{
				VertexStatus vertexStatus = new VertexStatus();
				vertexStatus.vertexLocal = mesh.vertices[x];  //LOCAL POSITION
				vertexStatus.normalLocal = mesh.normals[x]; //LOCAL NORMAL
				vertexStatus.uv = mesh.uv[x];
				vertexStatus.vertexIndex = x;
				vertexStatusLookup.Add(vertexStatus.vertexLocal.ToString(), vertexStatus);
			}
			return vertexStatusLookup;
		}
	}
}
