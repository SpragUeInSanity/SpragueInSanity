using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SpragueInSanity
{
    // Configuration information/options for Create Snow.
    public class UISnowOptionsSettings
    {
        public bool turnOnSnow = true; //On/Off
        public Vector3 startPosition = new Vector3(550f, -6.7f, 600f); //Default location of new terrain snow mesh
        public float width = 150; //Size of terrain snow mesh
        public float depth = 150;
        public float farOffSnowCoverage = .11f; //Shader Graph snow coverage on terrain not covered by terrain snow mesh.
        public float treeSnowAmount = .153f; //Shader Graph snow amount for SpeedTrees
        public string snowObjectsFile = "/SnowObjects.json"; //Name of configuration file for creating snow tops on objects.
        public string snowShader = "Shader Graphs/SnowToppedObjects"; //Name of snow shader used for terrain and SpeedTrees.
        public string defaultTreeShader = "Universal Render Pipeline/Nature/SpeedTree7";  //Default out-of-box SpeedTrees material for reset.
    }
}
