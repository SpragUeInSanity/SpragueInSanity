using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SpragueInSanity
{
    /// <summary>
    /// HELPER CLASS TO STORE TRIANGLE INFORMATION TO HELP DRAW TRIANGLES IN CLOCKWISE ROTATION 
    /// </summary>
    public class MeshTriangleData
    {
        public Vector3 startVertex;
        public Vector3 startNormal;
        public Vector3 nextVertex;
        public Vector3 nextNormal;
        public Vector3 endVertex;
        public Vector3 endNormal;
        public Vector2 startUV;
        public Vector2 nextUV;
        public Vector2 endUV;
        public int currentEdgeIndex = 0;

    }
}
