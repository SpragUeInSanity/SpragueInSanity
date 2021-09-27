using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SpragueInSanity
{
	/// <summary>
	/// THIS CLASS APPLIES A VERSION OF THE LAPLACIAN ALGORITHM TO SMOOTH THE SHARP EDGES OF THE INPUT MESH.
	/// REFERENCE:
	/// https://wiki.unity3d.com/index.php/MeshSmoother: By MarkGX
	/// https://github.com/mattatz/unity-mesh-smoothing: By mattatz
	/// </summary>

	public class MeshSmoothFilter
    {
		/// <summary>
		/// CALLED BY EXTERNAL CLASS TO SMOOTH THE SHARP EDGES OF THE INPUT MESH.  PROVIDE NUMBER OF PASSES
		/// TO SMOOTH MORE AND IF WANTED, SMOOTHING FACTOR TO INCREASE OR DECREASE THE AVERAGE SMOOTHING VERTEX
		/// MOVEMENT FOR OTHER EFFECTS.
		/// </summary>
		/// <param name="mesh"></param>
		/// <param name="smoothingFactor"></param>
		/// <param name="smoothnessPasses"></param>
		/// <returns></returns>
		public static Mesh LaplacianSmoothing(Mesh mesh, float smoothingFactor, int smoothnessPasses)
		{
			Hashtable adjacentVerticesLookup = new Hashtable();
			//APPLY ALGORITHM NUMBER OF TIMES
			for (int i = 0; i < smoothnessPasses; i++)
			{
				//GET HASH LOOKUP OF ALL VERTICES WITH AVERAGES BASED ON FOUND NEIGHBOR VERTICES IN ASSOCIATED TRIANGLES.
				adjacentVerticesLookup = MeshUtils.findAdjacentNeighborsLookup(mesh);

				//APPLY SMOOTING ALGORITHM
				mesh = LaplacianFilterAlternative(mesh, smoothingFactor, adjacentVerticesLookup);

			}
			if (smoothnessPasses > 0)
			{
				mesh.RecalculateNormals();
				mesh.RecalculateBounds();

			}
			return mesh;
		}
		// LAPLACIAN ALGORITHM
		private static Mesh LaplacianFilterAlternative(Mesh mesh, float smoothingFactor, Hashtable adjacentVerticesLookup)
		{
			float dx = 0.0f;
			float dy = 0.0f;
			float dz = 0.0f;

			Vector3[] sv = mesh.vertices;
			int[] t = mesh.triangles;
			Vector3[] wv = new Vector3[sv.Length];

			for (int vi = 0; vi < sv.Length; vi++)
			{

				List<Vector3> adjacentVertices = (List<Vector3>)adjacentVerticesLookup[sv[vi].ToString()];
				if (adjacentVertices == null)
				{
					//NOT SURE HOW YOU GET VERTICES WITH NO TRIANGLES. BUT, IT HAPPENS AND NEEDS TO BE TRAPPED.
					wv[vi] = sv[vi];
					continue;

				}
				dx = 0.0f;
				dy = 0.0f;
				dz = 0.0f;

				//Debug.Log("Vertex Index Length = "+vertexIndexes.Length);
				// Add the vertices and divide by the number of vertices
				for (int j = 0; j < adjacentVertices.Count; j++)
				{
					dx += adjacentVertices[j].x;
					dy += adjacentVertices[j].y;
					dz += adjacentVertices[j].z;
				}

				Vector3 targetPosition = new Vector3(dx / adjacentVertices.Count, dy / adjacentVertices.Count, dz / adjacentVertices.Count);
				Vector3 distanceChange = (sv[vi] - targetPosition) * smoothingFactor;
				wv[vi] = sv[vi] - distanceChange;
			}
			mesh.vertices = wv;
			return mesh;

		}



	}
}
