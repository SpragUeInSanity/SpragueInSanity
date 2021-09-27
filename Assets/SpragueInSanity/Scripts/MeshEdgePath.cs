using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SpragueInSanity
{
    /// <summary>
    /// HELPER CLASS TO CAPTURE VERTEX INFORMAITON FOR VERTICES THAT ARE FOUND TO BE EDGE OF TRIANGLE.
    /// </summary>
    public class MeshEdgePath
    {
        public Vector3 fromVertex;//STARTING VERTEX IN TRIANGLE EDGE
        public Vector3 toVertex;//ENDING VERTEX IN TRIANGLE EDGE
        public int count = 0;
        public bool isEdge = false;

    }
}
