using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SpragueInSanity
{
    /// <summary>
	/// THIS CLASS TAKES A GAME OBJECT AND FINDS THE PART OF THE OBJECT BASED ON ANGLE/DIRECTION IN THE WORLD PERSPECTIVE.
	/// PART IS DETERMINED BY THE ANGLE IN THE INPUT CONFIGURATION FILE FOR THE GAME OBJECT.  FOR EXAMPLE,
	/// IF 45 IS THE ANGLE AND DIRECTION IS UP, TOP WILL BE VERTICES FOUND IN UP ANGLE 0 - 45 DEGREES. 
	/// THE OUTPUT PRODUCED IS THE EXTRACTED MESH FROM THE ORIGINAL OBJECT
	/// IN ADDITION, ANOTHER METHOD IS PROVIDED TO PASS BACK WHICH OF THOSE
	/// VERTICES PARTICIPATE IN TRIANGLES THAT MAKE UP THE EDGE OF THE NEW MESH. 
	/// REFERENCE: 	https://youtu.be/BVCNDUcnE1o: By Tvtig
    /// </summary>
    public class MeshExtract
    {
        private Mesh originalMesh; //ORIGNAL SAVED SAVE TO USE FOR TRIANGLE LOGIC
        private Hashtable foundVertices; //WORKING LOOKUP COPY OF FOUND VERTICES
        private Hashtable meshVertices; //FINAL COPY OF FOUND VERTICES TO BE EXTRACTED FOR NEW MESH
        private List<Vector3> foundVertexArray = new List<Vector3>(); //ARRAY LIST OF FOUND/EXTRACTED VERTICES FOR NEW MESH
        private List<Vector3> foundNormalArray = new List<Vector3>(); //ASSOCIATED NORMALS
        private List<Vector2> foundUVArray = new List<Vector2>(); //ASSOCIATED UV'S
        private List<int> foundTriangles = new List<int>(); //FOUND TRIANGLES FOR EXTRACTED VERTICES FOR NEW MESH
        private List<Vector3> pairedEdgeVertexArray = new List<Vector3>(); //LIST OF FROM/TO PAIRS OF EDGE VERTICES FOR NEW MESH.
        private Hashtable pathCount = new Hashtable(); //LOOKUP OF EDGE VERTICES AND DETAILED INFORMATION THAT MIRRORS pairedEdgeVertexArray
                                                       // BUT ALLOWS KEY LOOKUP BY FROM/TO VERTICES.  QUICK WAY TO LOOKUP INSTEAD OF LOOPING.
        private List<MeshEdgePath> internalMeshTriangleEdges = new List<MeshEdgePath>(); //CAPTURES TRIANGLES WITH ALL VERTICES IN NEW MESH.
        private Vector3 zeroOrigin = new Vector4(0f, 0f, 0f); // USED IN SORTING ALGORITHM OF VERTICES.
        public Mesh GetMeshExtract(MeshObjectItem meshObjectItem)
        {
            Mesh extractedMesh;
            GameObject parentGo; // TOP LEVEL OR PARENT LEVEL OBJECT IN UNITY HIEARCHY FOR THIS MESH
            GameObject go; // GAME OBJECT IN UNITY HIEARCHY FOR THIS MESH.  NOTE IF NO HIERARCHY, THIS WILL BE SAME AS parentGo

            //ONLY PROCESS MESH IF SET AS ACTIVE
            if (!meshObjectItem.active)
            {
                return null;
            }
            parentGo = FindMeshTopLevelObject(meshObjectItem);
            if (parentGo == null)
            {
                Debug.LogError("MeshExtract:GetMeshExtract: Top Level Game Object " + meshObjectItem.gameObjectTopLevelName + " does not exist or is invalid!  Check spelling.");

                return null;
            }
            go = FindMeshChildObject(meshObjectItem, parentGo);
            if (go == null)
            {
                Debug.LogError("MeshExtract:GetMeshExtract: Game Object Part " + meshObjectItem.gameObjectPartName + " does not exist or is invalid!  Check spelling.");

                return null;
            }
            //GET VERTICES FOR GIVEN OBJECT BY DIRECTION AND ANGLE
            foundVertices = FindVerticesByAngle(parentGo, go, meshObjectItem);
            extractedMesh = CreateExtractedMesh(go);
            return extractedMesh;
        }
        /// <summary>
        /// THIS PROVIDES A FINAL PAIRED LIST ARRAY OF ALL FROM/TO VERTICES THAT FORM AN EDGE OF THE MESH CREATED WHEN GetMeshExtract(MeshObjectItem meshObjectItem)
        /// WAS CALLED.  IT DOES A FINAL PASS TO CLEANUP OR PRUNE OFF FALSE MESH FACES SUCH AS STRAIGHT EDGE LIKE A LINE.  THESE CAUSE ISSUES IN
        /// EXTRUSION SINCE IT JUST CREATES A FLAT PLANE.
        /// </summary>
        /// <returns></returns>
        public List<Vector3> GetPairedEdgeVertices()
        {
            List<Vector3> prunedEdgeVertexArray = new List<Vector3>();
  
            //REMOVE ARTIFACTS FROM FALSE POSITIVES ON EDGE PATHS.  ALL REAL
            //EDGES SHOULD BE COVERED BY TRIANGLES IN INTERIOR OF TOP MESH
            for (int x = 0; x < internalMeshTriangleEdges.Count; x++)
            {
                MeshEdgePath edgePath = internalMeshTriangleEdges[x];
                string key = edgePath.fromVertex.ToString() + edgePath.toVertex.ToString();

                if (pathCount.ContainsKey(key))
                {
                    MeshEdgePath edgePathFound = (MeshEdgePath)pathCount[key];
                    edgePathFound.isEdge = true;
                    pathCount[key] = edgePathFound;  //INTERNAL TRIANGLE AND EDGE IS TOP VERTICES - THIS IS EDGE!
                }

            }
            //CLEANUP THE MIRRORED ARRAY OF VECTOR3 REPRESENTATON OF EDGES TO MATCH CLEANED UP VERTICES ABOVE.

            for (int i = 0; i < pairedEdgeVertexArray.Count; i = i + 2)
            {
                MeshEdgePath edgePath = OrderPath(pairedEdgeVertexArray[i], pairedEdgeVertexArray[i + 1], zeroOrigin);
                MeshEdgePath edgePathCounts = (MeshEdgePath)pathCount[edgePath.fromVertex.ToString() + edgePath.toVertex.ToString()];
                if (edgePathCounts.isEdge)
                {

                    prunedEdgeVertexArray.Add(pairedEdgeVertexArray[i]);
                    prunedEdgeVertexArray.Add(pairedEdgeVertexArray[i + 1]);

                }
            }


            return prunedEdgeVertexArray;
        }
        /// <summary>
        /// FIND PARENT/MASTER MESH GAME OBJECT REPRESENTED BY TOP LEVEL NAME
        /// </summary>
        /// <param name="meshObjectItem"></param>
        /// <returns></returns>
        private GameObject FindMeshTopLevelObject(MeshObjectItem meshObjectItem)
        {
            GameObject go = GameObject.Find(meshObjectItem.gameObjectTopLevelName);
            return go;
        }
        /// <summary>
        /// FIND CHILD/PART MESH GAME OBJECT REPRESENTED BY PART OR CHILD LEVEL NAME
        /// IF THIS IS A TRUE HIERARCHY, THE ACTUAL CHILD OBJECT IS CONSTRUCTED.
        /// IF THIS IS A SIMPLE ONE LEVEL GAME OBJECT, BOTH PARENT AND CHILD WILL BE THE EXACT SAME OBJECT.
        /// 
        /// </summary>
        /// <param name="meshObjectItem"></param>
        /// <param name="parentGo"></param>
        /// <returns></returns>
        private GameObject FindMeshChildObject(MeshObjectItem meshObjectItem, GameObject parentGo)
        {
            GameObject go;
            if (parentGo.transform.childCount > 0)
            {
                go = GameObject.Find(meshObjectItem.gameObjectTopLevelName + "/" + meshObjectItem.gameObjectPartName);
            }
            else
            {
                //THIS IS A SIMPLE GAME OBJECT WITH NO CHILD OBJECTS
                go = GameObject.Find(meshObjectItem.gameObjectPartName);
            }
           
            return go;
        }
        /// <summary>
        /// BUILD HASHTABLE OF VERTICES FOUND BY ANGLE AND DIRECTION IN MESH
        /// </summary>
        /// <param name="parentGo"></param>
        /// <param name="go"></param>
        /// <param name="meshObjectItem"></param>
        /// <returns></returns>
        private Hashtable FindVerticesByAngle(GameObject parentGo, GameObject go, MeshObjectItem meshObjectItem)
        {
            Hashtable foundVertices = new Hashtable();
            GameObject parentGoTempForMove = new GameObject();
            Transform transform = go.transform;
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            Mesh mesh = meshFilter.sharedMesh;
            originalMesh = meshFilter.sharedMesh;
            //SET DIRECTION FOR ANGLE FIND
            Vector3 direction = new Vector3(meshObjectItem.extractDirectionX, meshObjectItem.extractDirectionY, meshObjectItem.extractDirectionZ);

            // CYCLE THROUGH ALL THE VERTICES AND IDENTIFY WHICH OF THESE ARE FOUND IN THE DIRECTION
            // ANGLE CHECK FROM WORLD VIEWPOINT
            // REDUNDANT VERTEX DATA IS IGNORED SINCE THIS ROUTINE IS ONLY INTERESTED IN A SINGLE SET OF VERTICES
            // FOR THE GIVEN NORMAL DIRECTION.  THUS, A HASHTABLE IS USED TO UNIQUELY KEY ON STRING VERSION OF VERTEX
            // WITH VALUE BEING A STRUCTURE TO CAPTURE ALL THE CALCULATED DETAILS OF THE VERTEX.
            for (int x = 0; x < mesh.vertices.Length; x++)
            {
                VertexStatus vertexStatus = new VertexStatus();
                vertexStatus.vertexLocal = mesh.vertices[x];  //LOCAL POSITION
                vertexStatus.vertexWorld = transform.TransformPoint(mesh.vertices[x]); //WORLD POSITION
                vertexStatus.normalLocal = mesh.normals[x]; //LOCAL NORMAL
                vertexStatus.normalWorld = transform.TransformPoint(mesh.normals[x]); //WORLD NORMAL

                //THIS IS USED TO CORRECT FOR GAME OBJECTS THAT HAVE PIVOT POINTS THAT DO NOT CONFORM TO NORMAL PRACTICES.  FOR EXAMPLE,
                //PIVOT POINTS FAR AWAY FROM THE GAME OBJECT.  CAN ALSO BE USED TO LOGICALLY RE-LOCATE THE PIVOT POINT IN THE MIDDLE OF A LARGE
                //GAME OBJECT IF THE DEFAULT PIVOT POINT CAUSES ISSUES WITH ANGLE DIRECTION FIND VIEW FROM PIVOT POINT.
                Vector3 movePoint = new Vector3(meshObjectItem.adjustXPivot, meshObjectItem.adjustYPivot, meshObjectItem.adjustZPivot);

                //CREATE TEMP GAME OBJECT TO MOVE TRANSFORM TO NEW LOGICAL LOCATION THAT WILL ALLOW ANGLE FIND FUNCTION TO WORK PROPERLY.
                //NOTE: FOR SIMPLE GAME OBJECTS parentGo = go.  FOR HIERARCHY WHERE PARENT HAS CHILD OBJECT, USING PARENT WILL GET CORRECT
                //SINCE CHILD WILL BE ZERO RELATIVE TO PARENT.
                parentGoTempForMove.transform.localScale = parentGo.transform.localScale;
                parentGoTempForMove.transform.rotation = parentGo.transform.rotation;
                parentGoTempForMove.transform.position = parentGo.transform.position + movePoint;
                Transform meshTransformParent = parentGoTempForMove.transform;

                //USING CORRECTED COORDINATES, GET CORRECT NORMALS.
                vertexStatus.normalWorldDirection = meshTransformParent.TransformDirection(vertexStatus.normalLocal);
                vertexStatus.normalWorldAngle = Vector3.Angle(vertexStatus.normalWorldDirection, direction);
                vertexStatus.uv = mesh.uv[x];
                vertexStatus.vertexIndex = x;
                vertexStatus.isFound = false;

                //VERTEX IN ANGLE?
                if (VertexInAngle(vertexStatus, meshObjectItem, meshTransformParent))
                {
                    // VERTEX FOUND, SAVE DATA TO HASHTABLE (UNIQUE ONLY)
                    vertexStatus.isFound = true;
                    if (!foundVertices.ContainsKey(vertexStatus.vertexLocal.ToString()))
                    {
                        foundVertices.Add(vertexStatus.vertexLocal.ToString(), vertexStatus);
                    }
                }
                
            }
            GameObject.Destroy(parentGoTempForMove);
            return foundVertices;
        }
        /// <summary>
        /// FLAGS A VERTEX AS FOUND IF IT IS FOUND IN THE MESH AT THE GIVEN ANGLE AND DIRECTION FROM THE PIVOT
        /// POINT.  ALSO PROVIDES RAY DRAW LINES FOR DEBUGGING.
        /// </summary>
        /// <param name="vertexStatus"></param>
        /// <param name="meshObjectItem"></param>
        /// <param name="meshTransform"></param>
        /// <returns></returns>
        private bool VertexInAngle(VertexStatus vertexStatus, MeshObjectItem meshObjectItem, Transform meshTransform)
        {
            bool isFound = false;
            if (vertexStatus.normalWorldAngle < meshObjectItem.extractAngle)
            {
                isFound = true;
                //TURNS ON RAW DRAW FOR HELP IN CONFIGURATION OF THE MESH OBJECT ITEM.  IT WILL SHOW LINES 
                //STARTING AT THE PIVOT POINT ANGLED OUT TO FIND THE VERTICES OF THE MESH.  THIS WILL HELP IDENTIFY
                //OBJECT THAT MAY HAVE SCREWY OR MISPLACED PIVOT POINTS CAUSING ISSUES WITH FINDING THE VERTICES IN A
                //GIVEN DIRECTION AND ANGLE
                if (meshObjectItem.debugVertexRaysOn)
                {
                    Vector3 dir = (vertexStatus.vertexWorld - meshTransform.position).normalized;
                    Debug.DrawLine(meshTransform.position, vertexStatus.vertexWorld + dir * 2f, Color.green, Mathf.Infinity);
                }
            }


            return isFound;
        }
        /// <summary>
        /// CREATES NEW EXTRACTED MESH
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        private Mesh CreateExtractedMesh(GameObject go)
        {
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            Mesh mesh = meshFilter.sharedMesh;

            //CREATE ARRAY STRUCTURE FROM HASHTABLE SO WE GET NEW INDEXES TO BE REFERENCED
            //BY THE TRIANGLES FOR THE NEW MESH.
            //CANNOT MODIFY HASH STRUCTURE IN LOOP. MAKE COPY TO LOOP
            meshVertices = new Hashtable();
            int newIndex = 0;
            foreach (string key in foundVertices.Keys)
            {
                VertexStatus vertexStatus = (VertexStatus)foundVertices[key];
                foundVertexArray.Add(vertexStatus.vertexLocal);
                foundNormalArray.Add(vertexStatus.normalLocal);
                foundUVArray.Add(vertexStatus.uv);
                vertexStatus.newVertexIndex = newIndex;
                meshVertices.Add(key,vertexStatus);
                newIndex++;
            }

            //ADD TRIANGLES ONLY FOR TOP VERTICES AND REINDEX TO NEW TOP ONLY STRUCTURE.
            for (int i = 0; i < mesh.triangles.Length; i += 3)
            {
                //GET ORIGINAL VERTEX VECTOR FOR EACH TRIANGLE POINT
                Vector3 vertex1point = mesh.vertices[mesh.triangles[i]];
                Vector3 vertex2point = mesh.vertices[mesh.triangles[i+1]];
                Vector3 vertex3point = mesh.vertices[mesh.triangles[i+2]];
                //CHECK IF TRIANGLE HAS FOUND VERTEX FOR EXTRACTION
                VertexStatus vertex1 = (VertexStatus)meshVertices[vertex1point.ToString()];
                VertexStatus vertex2 = (VertexStatus)meshVertices[vertex2point.ToString()];
                VertexStatus vertex3 = (VertexStatus)meshVertices[vertex3point.ToString()];
                if (vertex1 !=null && vertex2!=null && vertex3 !=null)
                {
                    foundTriangles.Add(vertex1.newVertexIndex);
                    foundTriangles.Add(vertex2.newVertexIndex);
                    foundTriangles.Add(vertex3.newVertexIndex);
                }
                //WHILE LOOPING FOR THE MESH CREATE, GO AHEAD AND BUILD CANDIDATE EDGE STRUCTURES TO SUPPORT EXTERANL ROUTINE CALLS
                //FOR THE EDGE DATA.  TAKE ADVANTAGE OF THE LOOPING HERE SO WE DON'T HAVE TO DO IT AGAIN IF DATA IS REQUESTED IN
                //OTHER CALLS.
                IdentifyCandidateEdges(vertex1, vertex2, vertex3);

            }
            Mesh extractedMesh = new Mesh();
            extractedMesh.vertices = foundVertexArray.ToArray();
            extractedMesh.triangles = foundTriangles.ToArray();
            extractedMesh.normals = foundNormalArray.ToArray();
            extractedMesh.uv = foundUVArray.ToArray();

            return extractedMesh;

        }
        /// <summary>
        /// THIS WILL BUILD A HASH LOOKUP OF EDGE VERTICES THAT ARE PART OF A TRIANGLE THAT HAS AT LEAST 2 COMMON VERTICES IN THE NEW MESH
        /// IN ADDITION, IT WILL BUILD A PAIRED ARRAY TO CAPTURE THE CORRECT ORDER OF THE FROM/TO VERTICES.
        /// THIS IS ALL DONE TO SUPPORT CALLS NEEDED FOR DATA IN OTHER ROUTINES SUCH AS EXTRUSIOIN THAT NEED TO KNOW WHERE THE EDGES OF THE MESH
        /// EXIST.
        /// </summary>
        /// <param name="vertex1"></param>
        /// <param name="vertex2"></param>
        /// <param name="vertex3"></param>
        private void IdentifyCandidateEdges(VertexStatus vertex1, VertexStatus vertex2, VertexStatus vertex3)
        {
            int vertexFoundCount = 0;
            bool vertex1IsEdge = false;
            bool vertex2IsEdge = false;
            bool vertex3IsEdge = false;

            //IDENTIFIES POTENTIAL EDGE VERTICES
            if (vertex1 != null)
            {
                vertexFoundCount++;
                vertex1IsEdge = true;
            }
            if (vertex2 != null)
            {
                vertexFoundCount++;
                vertex2IsEdge = true;
            }
            if (vertex3 != null)
            {
                vertexFoundCount++;
                vertex3IsEdge = true;
            }
            //BASED ON NUMBER OF HITS AND IF TOP, IDENTIFY VERTICES THAT PARTICIPATE IN EDGES.
            // 2 MEANS EDGE SUPPORTING EXTERNAL TRIANGLE TO MESH.  3 WOULD BE INTERNAL TRIANGLE TO MESH.
            if (vertexFoundCount == 2)
            {
                // CREATE SEVERAL LOOKUPS AS CANDIDATES FOR EDGES
                // SAVE THE FROM/TO AND ORIGINAL VERTEX DATA FOR EASY HASHTABLE LOOKUP BY FROM/TO COMBINED KEY
                // SAVE THE ORIGINAL VECTOR3 VERTEX FROM AND TO TO ARRAYS (NEEDED FOR ORDER WHEN BUILDING TRIANGLES CLOCKWISE BY OTHER ROUTINES LIKE EXTRUDE.
                if (vertex1IsEdge && vertex2IsEdge)
                {
                    Vector3 fromVertex = vertex1.vertexLocal;
                    Vector3 toVertex = vertex2.vertexLocal;
                    pairedEdgeVertexArray.Add(fromVertex);
                    pairedEdgeVertexArray.Add(toVertex);
                    UpdateOrderPath(fromVertex, toVertex);
                }
                if (vertex2IsEdge && vertex3IsEdge)
                {
                    Vector3 fromVertex = vertex2.vertexLocal;
                    Vector3 toVertex = vertex3.vertexLocal;
                    pairedEdgeVertexArray.Add(fromVertex);
                    pairedEdgeVertexArray.Add(toVertex);
                    UpdateOrderPath(fromVertex, toVertex);
                }
                if (vertex3IsEdge && vertex1IsEdge)
                {
                    Vector3 fromVertex = vertex3.vertexLocal;
                    Vector3 toVertex = vertex1.vertexLocal;
                    pairedEdgeVertexArray.Add(fromVertex);
                    pairedEdgeVertexArray.Add(toVertex);
                    UpdateOrderPath(fromVertex, toVertex);
                }

            }
            if (vertexFoundCount == 3)
            {
                //SAVE THIS INFORMATION TO IDENTIFY A TRIANGLE TO LIVES TOTALLY INSIDED THE NEW MESH.  WILL BE USED
                //FOR PRUNING IN OTHER EXTERNAL CALL FOR EDGE VERTICES.
                Vector3 fromVertex = vertex1.vertexLocal;
                Vector3 toVertex = vertex2.vertexLocal;
                MeshEdgePath edgePath = OrderPath(fromVertex, toVertex, zeroOrigin);
                internalMeshTriangleEdges.Add(edgePath);

                fromVertex = vertex2.vertexLocal;
                toVertex = vertex3.vertexLocal;
                edgePath = OrderPath(fromVertex, toVertex, zeroOrigin);
                internalMeshTriangleEdges.Add(edgePath);

                fromVertex = vertex3.vertexLocal;
                toVertex = vertex1.vertexLocal;
                edgePath = OrderPath(fromVertex, toVertex, zeroOrigin);
                internalMeshTriangleEdges.Add(edgePath);

            }
        }
        /// <summary>
        /// SAVE TRIANGLE VERTEX EDGE INFORMATION TO HASH LOOKUP STRUCTURE TO BE USED LATER FOR BUILDING
        /// FINAL EDGE ARRAYS TO BE USED BY OTHER EXTERNAL ROUTINES SUCH AS EXTRUSION.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        private void UpdateOrderPath(Vector3 first, Vector3 second)
        {
            //ORDER THE FROM/TO VERTEX DATA FOR CONSISTENT KEYS
            MeshEdgePath edgePath = OrderPath(first, second, zeroOrigin);
            string key = edgePath.fromVertex.ToString() + edgePath.toVertex.ToString();

            if (pathCount.Count == 0)
            {
                edgePath.count = 1;
                pathCount.Add(key, edgePath);
                return;
            }
            if (pathCount.ContainsKey(key))
            {
                MeshEdgePath edgePathFound = (MeshEdgePath)pathCount[key];
                edgePathFound.count++;
                pathCount[key] = edgePathFound;
            }
            else
            {
                edgePath.count = 1;
                pathCount.Add(key, edgePath);
            }

        }
        /// <summary>
        /// ORDER THE VECTOR VALUES SO KEY LOOKUPS AND STORAGE ARE CONSISTENT.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        private MeshEdgePath OrderPath(Vector3 first, Vector3 second, Vector3 origin)
        {
            MeshEdgePath edgePath = new MeshEdgePath();
            edgePath.fromVertex = first;
            edgePath.toVertex = second;

            if (edgePath.fromVertex == edgePath.toVertex)
                return edgePath;

            Vector3 firstOffset = edgePath.fromVertex - origin;
            Vector3 secondOffset = edgePath.toVertex - origin;

            float angle1 = Mathf.Atan2(firstOffset.x, firstOffset.z);
            float angle2 = Mathf.Atan2(secondOffset.x, secondOffset.z);


            if (angle1 < angle2)
                return edgePath;

            if (angle1 > angle2)
            {
                edgePath.fromVertex = second;
                edgePath.toVertex = first;
                return edgePath;
            }

            // Check to see which point is closest
            if (firstOffset.sqrMagnitude > secondOffset.sqrMagnitude)
            {
                edgePath.fromVertex = second;
                edgePath.toVertex = first;
                return edgePath;

            }

            return edgePath;
        }

    }
}
