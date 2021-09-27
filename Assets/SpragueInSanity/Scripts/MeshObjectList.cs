using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace SpragueInSanity
{
    /// <summary>
    /// Help class that stored the list of game object configurations for creating extrusion.  Used in memory and persisted JSON file
    /// </summary>
    [Serializable]
    public class MeshObjectList
    {
        [SerializeField]
        public MeshObjectItem[] meshObjectItems;
    }
}
