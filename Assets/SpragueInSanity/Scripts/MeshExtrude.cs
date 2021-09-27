using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SpragueInSanity
{
    /// <summary>
    /// THIS CLASS TAKES THE INPUT MESH AND EDGE ARRAY (FROM MeshExtract) LISTING EDGE VERTICES FOR THE NEW MESH.  IT CREATES A NEW OBJECT
    /// USING EXTRUSION ON THE INPUT MESH.
    /// REFERENCES:
    /// 	https://github.com/renangalves/MeshExtrusion: By renangalves
    /// 	https://youtu.be/BVCNDUcnE1o: By Tvtig
    /// </summary>
    public class MeshExtrude
    {
        //MASTER LIST FOR EXTRUDED MESH
        private List<Vector3> allVertices = new List<Vector3>(); 
        private List<Vector3> allNormals = new List<Vector3>();
        private List<Vector2> allUVs = new List<Vector2>();
        private List<int> allTriangles = new List<int>();
        MeshUtils meshUtils;
        Hashtable vertexStatusLookup;
        public Mesh GetExtrudedMesh(Mesh originalMesh, MeshObjectItem meshObjectItem, List<Vector3> pairedEdgeArray)
        {
            Mesh extrudedMesh = originalMesh;
            if (meshObjectItem.extrudeDistance > 0f)
            {
                meshUtils = new MeshUtils();
                PrepMasterArrays(originalMesh, meshObjectItem);
                extrudedMesh = CreateExtrudedMesh(meshObjectItem, pairedEdgeArray);
            }
            return extrudedMesh;
        }
        /// <summary>
        /// INITIALIZE ALL THE MESH ARRAYS WITH THE ORIGINAL DATA AS WELL AS NEW DATA FOR THE EXTRUDED NEW MESH TO BE CREATED
        /// </summary>
        /// <param name="originalMesh"></param>
        /// <param name="meshObjectItem"></param>
        private void PrepMasterArrays(Mesh originalMesh, MeshObjectItem meshObjectItem)
        {
            allVertices.AddRange(originalMesh.vertices);//Add original mesh vertices - This represents original mesh before extrusion
            //ADD EXTRUDED VERTICES TO MOVE MESH TO DIRECTION BY DISTANCE.
            Vector3 direction = new Vector3(meshObjectItem.extractDirectionX, meshObjectItem.extractDirectionY, meshObjectItem.extractDirectionZ);
            for (int x = 0; x < originalMesh.vertices.Length; x++)
            {

                Vector3 extrudeVertex = originalMesh.vertices[x] + direction * meshObjectItem.extrudeDistance;
                allVertices.Add(extrudeVertex);
            }

            allNormals.AddRange(originalMesh.normals);  //Add original mesh normals - This represents original edge of mesh before extrusion
            // FLIP NORMALS TO POINT OTHER DIRECTION FOR ORIGINAL VERTICES SINCE EXTRUSION WILL ADD MESH TO DIRECTION OF ORIGINAL MESH NORMALS
            for (int x = 0; x < allNormals.Count; x++)
            {
                allNormals[x] = allNormals[x] * - 1;
            }
            allNormals.AddRange(originalMesh.normals);  //Add original mesh normals to match extruded vertices
            allUVs.AddRange(originalMesh.uv);  //Add original mesh UVs
            allUVs.AddRange(originalMesh.uv);  //Add again for extruded mesh

            allTriangles.AddRange(originalMesh.triangles); //Add original mesh triangles
            // Add triangles from original mesh but adjust indexes to point to new
            // location(vertices) in allVertices total list that was extruded
            int topIndexOffset = originalMesh.vertices.Length;
            for (int i = 0; i < originalMesh.triangles.Length; i++)
            {
                allTriangles.Add(originalMesh.triangles[i] + topIndexOffset);
            }
            vertexStatusLookup=meshUtils.BuildVertexStatusLookup(originalMesh);
        }
        /// <summary>
        /// This fills in the new faces between the top/bottom of the new extruded object
        /// Unity handles mesh as triangles.  This routine builds the triangles in clockwise rotation
        /// For example, imagine a simple cube and you are looking at one face of the cube looking down the X-axis
        /// In this simple object, your face is a plane with 4 vertices.  Imagine the numbering the vertices starting
        /// at top right = 0.  Going clockwise, bottom right = 1, bottom left = 2 and top left = 3
        /// The algorithm below builds out two triangles at a time as 0-1-2, 2-3-0.  Then, it goes to the next plane and repeats
        /// until all the vertices are connected with triangles from top to bottom meshes.
        /// </summary>
        /// <param name="meshObjectItem"></param>
        /// <param name="pairedEdgeArray"></param>
        /// <returns></returns>
        private Mesh CreateExtrudedMesh(MeshObjectItem meshObjectItem,List<Vector3> pairedEdgeArray)
        {
            int edgeCount = pairedEdgeArray.Count; //ARRAY BUILT WITH ALL EDGE VERTICES FROM MeshExtract
            Vector3 direction = new Vector3(meshObjectItem.extractDirectionX, meshObjectItem.extractDirectionY, meshObjectItem.extractDirectionZ);
            //SET OFFSET TO CORRECT AMOUNT BASED ON DIRECTION
            Vector3 vectorOffset = direction * meshObjectItem.extrudeDistance;
            MeshTriangleData triDirectionData = new MeshTriangleData();

            for (int x = 0; x < edgeCount; x+=2) //FROM/TO IS LISTED IN PAIRS IN THIS ARRAY
            {
                triDirectionData = meshUtils.CreateNewTriangle(x, vectorOffset, pairedEdgeArray, vertexStatusLookup);
                AddNewTrianglesNormalsAndUvs(triDirectionData);
 
                x = triDirectionData.currentEdgeIndex;

            }
            Mesh newMesh = new Mesh();
            newMesh.vertices = allVertices.ToArray();
            newMesh.triangles = allTriangles.ToArray();
            newMesh.normals = allNormals.ToArray();
            newMesh.uv = allUVs.ToArray();
            newMesh.RecalculateNormals();
            newMesh.RecalculateTangents();
            return newMesh;
        }
        /// <summary>
        /// COORDINATE CREATING THE NEW TRIANGLE ARRAY UPDATES AND NEW NORMALS
        /// </summary>
        /// <param name="triDirectionData"></param>

        private void AddNewTrianglesNormalsAndUvs(MeshTriangleData triDirectionData)
        {

            Vector3 normal1 = ComputeNormal(triDirectionData.startVertex, triDirectionData.nextVertex, triDirectionData.endVertex);

            AddVertNormalUv(triDirectionData.startVertex, (Vector3)normal1, triDirectionData.startUV);

            Vector3 normal2 = ComputeNormal(triDirectionData.nextVertex, triDirectionData.endVertex, triDirectionData.startVertex);

            AddVertNormalUv(triDirectionData.nextVertex, (Vector3)normal2, triDirectionData.nextUV);

            Vector3 normal3 = ComputeNormal(triDirectionData.endVertex, triDirectionData.startVertex, triDirectionData.nextVertex);

            AddVertNormalUv(triDirectionData.endVertex, (Vector3)normal3, triDirectionData.endUV);
        }
        /// <summary>
        /// RECALCULATE NORMALS BASED ON CROSS BETWEEN SIDES OF TRIANGLE
        /// </summary>
        /// <param name="vertex1"></param>
        /// <param name="vertex2"></param>
        /// <param name="vertex3"></param>
        /// <returns></returns>
        private Vector3 ComputeNormal(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3)
        {
            Vector3 side1 = vertex2 - vertex1;
            Vector3 side2 = vertex3 - vertex1;
            Vector3 normal = Vector3.Cross(side1, side2);

            return normal;
        }
        /// <summary>
        /// UPDATE MASTER ARRAYS WITH NEW TRIANGLE DATA.
        /// </summary>
        /// <param name="vertex"></param>
        /// <param name="normal"></param>
        /// <param name="uv"></param>

        private void AddVertNormalUv(Vector3 vertex, Vector3 normal, Vector2 uv)
        {

            allVertices.Add(vertex);
            allNormals.Add(normal);
            allUVs.Add(uv);
            int vertIndex = allVertices.Count - 1;
            allTriangles.Add(vertIndex);

        }

    }
}
