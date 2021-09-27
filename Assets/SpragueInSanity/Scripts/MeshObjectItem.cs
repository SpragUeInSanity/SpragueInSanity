using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace SpragueInSanity
{
    /// <summary>
    /// THIS IS A HELPER CLASS TO HOLD WORKING STORAGE CONFIGURATION INFORMATION FOR A GIVEN GAME OBJECT MESH.
    /// THIS DEFINES THE OPTIONS FOR FINDING, COPYING, EXTRUDING, SUBDIVISION AND SMOOTHING.
    /// </summary>
    [Serializable]
    public class MeshObjectItem
    {
        public string gameObjectTopLevelName; //Level one name in Unity Hierarchy
        public string gameObjectPartName; //Second level in Hiearchy.  If only one level, this should be the same value as gameObjectTopLevelName
        public float extractAngle; //Max angle from normal direction (0 degress) to find vertices on mesh.
        //Defines direction to find extraction angle for this for this mesh.
        public float extractDirectionX = 0f; //-1=Left, 1=Right, 0=Ignore 
        public float extractDirectionY = 1f; //-1=Down/Bottom, 1=Up/Top, 0=Ignore
        public float extractDirectionZ = 0f; //-1=Back, 1=Forward, 0=Ignore
        public float extrudeDistance; //How big or thick to make the extruded object (aka snow)
        public int subDivisionPasses; //How many times to subdivide this extruded object for more detail to support better smoothing
        public int smoothnessPasses; //How may times to apply the smooth algorithm to the new object
        public float smoothnessFactor; //Override how much the vertices are moved by the smoothing algorith. 0-1
        //Adjust local object zero pivot location if it is far off or needs to be moved around to find extract direction and angle.
        public float adjustXPivot = 0f; 
        public float adjustYPivot = 0f;
        public float adjustZPivot = 0f;
        public bool debugVertexRaysOn = false; //Turns on ray beams to visualize where extract angle is trying to find vertices from Pivot point.
        public bool active = true; // Ability to turn on/off mesh routines for this object.

    }
}
