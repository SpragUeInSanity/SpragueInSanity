using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SpragueInSanity
{
    /// <summary>
    /// HELPER STRUCTE TO CAPTURE INFORMATION ABOUT A VERTEX IN A MESH.
    /// </summary>
    public class VertexStatus
    {
        public int vertexIndex = 0;
        public Vector3 vertexLocal;
        public Vector3 vertexWorld;
        public Vector3 normalLocal;
        public Vector3 normalWorld;
        public Vector3 normalWorldDirection;
        public float normalWorldAngle = 0f;
        public Vector2 uv;
        public bool isEdge = false;
        public bool isFound = false;
        public int newVertexIndex = 0;
        public int edgePairs = 0;


    }
}
