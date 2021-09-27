using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;


namespace SpragueInSanity
{
    /// <summary>
    //This is the coordination class to create snow for Terrain, SpeedTrees and game objects.  The Terrain data object is cloned to memory
    //so that changes to the paint details is in memory only and does not impact the persisted Terrain data.  This class allows an existing
    //scene to be used as-is and apply snow coverage to it.  No need to build snow specific objects or terrain.
    //NOTE: THE ONLY ISSUE THAT CAN HAPPEN IS IF SNOW IS APPLIED BUT THE GAME IS STOPPED BEFORE RESET.  THE SPEEDTREES WILL STILL BE
    //SNOW COVERED(ACTUALLY SHADER GRAPH APPLIED TO ORIGINAL MATERIAL). THIS IS EASILY CORRECTED BY RUNNING THIS AGAIN AND SETTING NO SNOW.
    //THE ORIGINAL MATERIAL PASSED IN THE PARAMETER WILL BE APPLIED.
    //REFERENCES:
    //  https://youtu.be/kmPj2GmoMWo : By Léo Chaumartin
    //  https://youtu.be/bY7r6blL1K8: By World of Zero
    //	https://answers.unity.com/questions/13854/edit-terrain-foliagetexture-at-runtime.html: By duck
    //  https://answers.unity.com/questions/13377/can-i-modify-grass-or-details-on-the-terrain-at-ru.html: By duck
    //  http://the-game-designers-journey.blogspot.com/2014/05/createremove-unity-grass-at-runtime.html: By Mads
    //  https://stackoverflow.com/questions/62442429/best-way-to-get-shape-of-terrain-at-1x1-size-for-road-system by derHugo
    //  https://github.com/renangalves/MeshExtrusion: By renangalves
    //	https://youtu.be/BVCNDUcnE1o: By Tvtig
    /// </summary>
    public class CreateSnow
    {
        private List<GameObject> snowObjects = new List<GameObject>();
        Material terrMat;
        Material terrainShaderMaterial;
        TerrainData originalTerrainData = new TerrainData();
        TerrainData clonedTerrainData;
        bool terrainIsCloned = false;
        GameObject terrGameObject;

        /// <summary>
        /// CALLED BY EXTERNAL CLASS TO INITIATE THE CREATION OF SNOW OBJECTS FOR THE TOP OF EACH GAME OBJECT.
        /// USING A CONFIGURATION JSON FILE, PROCESS EACH GAME OBJECT IN THE FILE AND APPLY THE SNOW OPTIONS.
        /// TO GUARANTEE UNIQUENESS, THE CONFIGURATION FILE WILL LIST THE PARENT OBJECT AND CHILD OBJECT FROM THE HIERARCHY.  IF
        /// THIS IS A SIMPLE OBJECT WITH ONLY ONE LEVEL IN THE HIERARCHY, LIST THE NAME IN BOTH THE PARENT AND CHILD.  IT IS POSSIBLE
        /// TO HAVE MANY OBJECTS WITH THE SAME SET OF SUBOBJECTS WITH THE SAME NAME.  ONLY THE HIERARCHY MAKES THEM UNIQUE.
        /// </summary>
        /// <param name="snowObjectsFile"></param>
        public void CreateSnowForObjects(string snowObjectsFile)
        {
            //SAFETY CHECK TO MAKE SURE OBJECTS ARE ONLY CREATED ONCE.
            if (snowObjects.Count > 0)
            {
                return;
            }
            //READ INPUT GAME OBJECTS CONFIGURATION FILE
            string snowObjectsData = File.ReadAllText(Application.dataPath + snowObjectsFile);
            MeshObjectList loadSnowOnObjects = JsonUtility.FromJson<MeshObjectList>(snowObjectsData);

            for (int x = 0; x < loadSnowOnObjects.meshObjectItems.Length; x++)
            {
                //FOR EACH ACTIVE OBJECT IN FILE, CREATE THE SNOW COVERED OBJECT
                if (loadSnowOnObjects.meshObjectItems[x].active)
                {
                    GameObject parentGO = GameObject.Find(loadSnowOnObjects.meshObjectItems[x].gameObjectTopLevelName);
                    if (parentGO == null)
                    {
                        Debug.LogError("CreateSnow:CreateSnowForObjects: Top Level Game Object " + loadSnowOnObjects.meshObjectItems[x].gameObjectTopLevelName + " does not exist or is invalid!  Check spelling.");

                        return;
                    }
                    GameObject go;
                    if (parentGO.transform.childCount > 0)
                    {
                        go = GameObject.Find(loadSnowOnObjects.meshObjectItems[x].gameObjectTopLevelName + "/" + loadSnowOnObjects.meshObjectItems[x].gameObjectPartName);
                    }
                    else
                    {
                        //THIS IS A SIMPLE GAME OBJECT WITH NO CHILD OBJECTS
                        go = GameObject.Find(loadSnowOnObjects.meshObjectItems[x].gameObjectPartName);
                    }
                    if (go == null)
                    {
                        Debug.LogError("CreateSnow:CreateSnowForObjects: Game Object Part " + loadSnowOnObjects.meshObjectItems[x].gameObjectPartName + " does not exist or is invalid!  Check spelling.");

                        return;
                    }

                    ProcessGameObject(parentGO,go, loadSnowOnObjects.meshObjectItems[x]);

                }
            }

        }
        //THIS IS THE MAIN LOGIC COORDINATION TO CREATE A LAYER OF SNOW ON TOP OF EACH GAME OBJECT FOUND IN THE INPUT JSON CONFIGURATION
        //FILE.  IT USES MESH EXTRACTION, MESH EXTRUSION, SUBDIVISION TO ADD DETAIL FOR SMOOTHING AND FINALLY SMOOTHING TO MAKE
        //THE SNOW LAYER LOOK A LITTLE LESS RIGID.
        private void ProcessGameObject(GameObject parentGO, GameObject go, MeshObjectItem meshObjectItem)
        {
            
            //GET MESH EXTRACT FROM ORIGINAL MESH BASED ON DIRECTION, ANGLE CONFIGURATION
            //FOR THIS MESH OBJECT
            MeshExtract meshExtract = new MeshExtract();
            Mesh topMesh = meshExtract.GetMeshExtract(meshObjectItem);
            List<Vector3> pairedEdgeArray = new List<Vector3>();
            pairedEdgeArray = meshExtract.GetPairedEdgeVertices();

            //EXTRUDE THE MESH TO A NEW OBJECT SIZE BASED ON EXTRUDE DISTANCE
            //USES MESH FROM MeshExtract AND EDGE ARRAY FROM
            //MeshExtract TO PROPERLY IDENFITY TOP VERTICES AND VERTICES THAT 
            //MAKE UP THE EDGE FACES.
            MeshExtrude meshExtrude = new MeshExtrude();
            Mesh newMesh = meshExtrude.GetExtrudedMesh(topMesh, meshObjectItem, pairedEdgeArray);

            //SUBDIVIDE NEW MESH TO ALLOW FOR SMOOTHING AND FINER GRAIN DEFORMATIONS
            MeshSubdivide subdivideMesh = new MeshSubdivide();
            newMesh = subdivideMesh.Subdivide(newMesh, meshObjectItem.subDivisionPasses);

            // APPLY SMOOTHING ALGORITHM TO TAMP DOWN SHARP EDGES.
            newMesh = MeshSmoothFilter.LaplacianSmoothing(newMesh, meshObjectItem.smoothnessFactor, meshObjectItem.smoothnessPasses);

            //RENDER SNOW LAYER OBJECT ON TOP OF ORIGINAL GAME OBJECT.
            GameObject newObject = CreateMeshGameObject(go);
            newObject.name = string.Format("{0}_snow", parentGO.name);
            newObject.GetComponent<MeshFilter>().mesh = newMesh;
            MeshRenderer meshRenderer = newObject.GetComponent<MeshRenderer>();
            Material newMat = Resources.Load("SnowForObjects", typeof(Material)) as Material;
            meshRenderer.material = newMat;
            //CAPTURE OBJECT SO WE CAN DESTROY WHEN TURNING OFF SNOW.
            snowObjects.Add(newObject);

        }
        //CREATE NEW GAME OBJECT MIRRORING SOME OF THE CONFIGURATION FROM ORIGINAL OBJECT
        private static GameObject CreateMeshGameObject(GameObject originalObject)
        {
            var originalMaterial = originalObject.GetComponent<MeshRenderer>().materials;

            GameObject meshGameObject = new GameObject();

            meshGameObject.AddComponent<MeshFilter>();
            meshGameObject.AddComponent<MeshRenderer>();

            //meshGameObject.GetComponent<MeshRenderer>().materials = originalMaterial;

            meshGameObject.transform.localScale = originalObject.transform.localScale;
            meshGameObject.transform.rotation = originalObject.transform.rotation;
            meshGameObject.transform.position = originalObject.transform.position;

            meshGameObject.tag = originalObject.tag;

            return meshGameObject;
        }
        /// <summary>
        /// CALLED BY EXTERNAL CLASS TO INITIATE THE CREATION OF SNOW OBJECT TO COVER THE TERRAIN.  THIS IS MEANT TO PROVIDE
        /// A THICK SNOW LOOK TO IMMEDIATE SCENE VIEW OF PLAYER.
        /// THIS IS ACCOMPLISHED BY CREATING A MIRROR MESH OF A SECTION OF THE TERRAIN AS DEFINED BY THE INPUT PARAMETERS.
        /// THEN, THE SHADER GRAPH IS APPLIED TO THE ORIGINAL TERRAIN TO GIVE APPEARANCE OF SNOW WHERE NOT COVERED BY THE MESH.
        /// THIS GIVES DISTANT PLAYER VIEWING PERSPECTIVE OF SNOW ON THE TERRAIN HILLS, ETC.
        /// 
        /// </summary>
        /// <param name="terrain"></param>
        /// <param name="startPosition"></param>
        /// <param name="width"></param>
        /// <param name="depth"></param>
        /// <param name="shaderSnowCoverage"></param>
        public void CreateSnowForTerrain(Terrain terrain, Vector3 startPosition, float width, float depth, float shaderSnowCoverage)
        {
            //SAFETY CHECK TO MAKE SURE OBJECTS ARE ONLY CREATED ONCE.
            if (terrGameObject != null)
            {
                return;
            }
            TerrainMesh terrainMesh = new TerrainMesh();
            //CREATE MIRROR MESH OF TERRAIN IN AREA DEFINED BY INPUT PARAMETERS
            Mesh newMesh = terrainMesh.GetAreaMesh(terrain, startPosition, width, depth);
            //CREATE GAME OBJECT FOR MESH TO RENDER ON TOP OF THIS SECTION OF TERRAIN.
            terrGameObject = new GameObject();

            terrGameObject.AddComponent<MeshFilter>();
            terrGameObject.AddComponent<MeshRenderer>();
            terrGameObject.transform.position = startPosition;
            terrGameObject.name = string.Format("{0}_snow", "terrain");
            terrGameObject.GetComponent<MeshFilter>().mesh = newMesh;
            MeshRenderer meshRenderer = terrGameObject.GetComponent<MeshRenderer>();
            Material newMat = Resources.Load("SnowForObjects", typeof(Material)) as Material;
            meshRenderer.material = newMat;

            //APPLY SNOW SHADER TO COLOR THE TERRAIN.  THIS GIVES UNCOVERED TERRAIN
            //APPEARANCE OF SNOW.
            terrainShaderMaterial = Resources.Load<Material>("SnowToppedTerrain");
            if (terrainShaderMaterial == null)
            {
                Debug.Log("Terrain Material Not Found.  Ignored Snow Setting.");
                return;
            }
            terrMat = terrain.materialTemplate;
            terrainShaderMaterial.SetFloat("SnowAmount", shaderSnowCoverage);
            terrain.materialTemplate = terrainShaderMaterial;


        }

        /// <summary>
        /// CALLED BY EXTERNAL CLASS TO APPLY A NEW SHADER GRAPH SNOW MATERIAL TO THE SPEEDTREES.
        /// THE APPEARANCE LOOKS GOOD FROM A GAME VIEW, SO NO NEED TO TRY TO CREATE THICK SNOW
        /// LIKE IS DONE FOR OTHER GAME OBJECTS.  (AT THIS TIME ;) )
        /// </summary>
        /// <param name="TreeSnowAmount"></param>
        /// <param name="snowShader"></param>
        public void CreateSnowForSpeedTrees(float TreeSnowAmount, string snowShader)
        {
            Shader snowShaderObject = Shader.Find(snowShader);
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            //SEARCH ALL GAME OBJECTS FOR THE TREE COMPONENTS.
            foreach (GameObject go in allObjects)
            {

                Tree treeObject = go.GetComponent<Tree>();

                if (treeObject != null)
                {
                    //FOR EACH TREE FOUND, REPLACE ITS MATERIAL.
                    foreach (Material matt in go.gameObject.GetComponent<Renderer>().sharedMaterials)
                    {
                        if (matt != null)
                        {
                            matt.shader = snowShaderObject;
                            matt.SetFloat("SnowAmount", TreeSnowAmount);

                        }
                    }
                }
            }

        }
        /// <summary>
        /// CALLED BY EXTERNAL CLASS TO REMOVE ANY PAINT DETAILS FROM THE TERRAIN AT A GIVEN AREA.
        /// PAINT DETAILS ARE THE TERRAIN TOOLS CREATED GRASS, GRASS FLOWERS.  THIS ROUTINE IS USED
        /// TO HIDE THOSE DETAILS SO GRASS AND GRASS FLOWERS ARE NOT POKING ABOVE THE SNOW COVER MESH.  PROVIDES
        /// CLEAN SNOW COVER LOOK.
        /// THIS IS A NON-DESCTRUCTIVE ROUTINE.  THE TERRAIN DATA IS CLONED AND ONLY THE CLONE IS IMPACTED, NOT
        /// THE ORIGINAL TERRAIN.
        /// </summary>
        /// <param name="terrain"></param>
        /// <param name="startPosition"></param>
        /// <param name="width"></param>
        /// <param name="depth"></param>
        public void RemoveTerrainPaintDetails(Terrain terrain, Vector3 startPosition, float width, float depth)
        {
            //SAFETY CHECK TO MAKE SURE OBJECTS ARE ONLY CREATED ONCE.
            if (terrainIsCloned)
            {
                return;
            }
            // COPY ORIGINAL TERRAIN DATA TO MEMORY SO ALL CHANGES ARE IN MEMORY ONLY.
            // THIS PREVENTS PERMANENT DESTRUCTION OF PAINT DETAILS FROM ORIGINAL TERRAIN.
            CloneTerrain(terrain);

            //GET TERRAIN SIZE AND DENSITY
            int TerrainDetailMapSize = terrain.terrainData.detailResolution;
            float PrPxSize = TerrainDetailMapSize / clonedTerrainData.size.x;
            //ADJUST START POSITION TO WITH DENSITY
            Vector3 TexturePoint3D = startPosition;
            TexturePoint3D = TexturePoint3D * PrPxSize;
            //Debug.Log(TexturePoint3D);

            //FIX AREA OF TERRAIN TO REMOVE PAINT DETAILS
            float[] xymaxmin = new float[4];
            xymaxmin[0] = TexturePoint3D.z + depth;
            xymaxmin[1] = TexturePoint3D.z;
            xymaxmin[2] = TexturePoint3D.x + width;
            xymaxmin[3] = TexturePoint3D.x;

            //Debug.Log("Grid to remove grass: " + xymaxmin[0] + "," + xymaxmin[1] + "," + xymaxmin[2] + "," + xymaxmin[3]);
            //GET NUMBER OF PAINT DETAIL LAYERS ASSIGNED: I.E. GRASS, FLOWER, ETC
            int numDetails = terrain.terrainData.detailPrototypes.Length;
            List<int[,]> maps = new List<int[,]>();

            //GET DETAIL MATRIX MAP FOR THE AREA AND LAYER
            for (int layerNum = 0; layerNum < numDetails; layerNum++)
            {
                int[,] map = terrain.terrainData.GetDetailLayer(0, 0, terrain.terrainData.detailWidth, terrain.terrainData.detailHeight, layerNum);
                int[,] newMap = map.Clone() as int[,];
                maps.Add(newMap);
            }
            //LOOP THROUGH THE ENTIRE AREA AND LAYER MAPS FOR EACH PART OF THE AREA
            //AND SET THE DETAIL COUNT TO 0.  THIS ESSENTIALLY TELLS UNITY THERE ARE NO PAINT
            //DETAILS AT THE AREA OF THE TERRAIN FOR THIS LAYER.
            for (int y = (int)xymaxmin[3]; y < (int)xymaxmin[2]; y++)
            {
                for (int x = (int)xymaxmin[1]; x < (int)xymaxmin[0]; x++)
                {

                    if (xymaxmin[0] > x && xymaxmin[1] < x && xymaxmin[2] > y && xymaxmin[3] < y)
                    {
                        for (int layerNum = 0; layerNum < numDetails; layerNum++)
                        {
                            int[,] map = maps[layerNum];
                            map[x, y] = 0;
                            maps[layerNum] = map;
                        }
                    }
                }
            }


            //ASSIGN THE NEW ZERO DETAIL MAPS BACK TO EACH LAYER
            for (int layerNum = 0; layerNum < numDetails; layerNum++)
            {
                int[,] map = maps[layerNum];
                terrain.terrainData.SetDetailLayer(0, 0, layerNum, map);


            }
            //Debug.Log("Number of layers: " + numDetails);
            //Debug.Log("Number of maps: " + maps.Count);


        }
        //COPY TERRAIN DATA TO MEMORY FOR APPLICATION OF CHANGES TO PAINT DETAILS SUCH AS GRASS, FLOWERS, ETC.
        private void CloneTerrain(Terrain terrain)
        {
            originalTerrainData = (TerrainData)Object.Instantiate(terrain.terrainData);
            clonedTerrainData = (TerrainData)Object.Instantiate(originalTerrainData);
            terrain.terrainData = clonedTerrainData;
            TerrainCollider tc = terrain.gameObject.GetComponent<TerrainCollider>();
            tc.terrainData = clonedTerrainData;
            terrainIsCloned = true;
            return;
        }

        /// <summary>
        /// CALLED BY EXTERNAL CLASS TO RESET THE SPEEDTREES ORIGINAL MATERIAL - TURN OFF THE SNOW LOOK SHADER.
        /// </summary>
        /// <param name="defaultShader"></param>
        public void ResetSpeedTrees(string defaultShader)
        {
            Shader defaultShaderObject = Shader.Find(defaultShader);
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

            foreach (GameObject go in allObjects)
            {

                Tree treeObject = go.GetComponent<Tree>();

                if (treeObject != null)
                {

                    foreach (Material matt in go.gameObject.GetComponent<Renderer>().sharedMaterials)
                    {
                        if (matt != null)
                        {
                            matt.shader = defaultShaderObject;
                        }
                    }

                }
            }

        }

        /// <summary>
        /// CALLED BY EXTERNAL CLASS TO RESET THE TERRAIN BACK TO ORIGINAL STATE - TURN OFF SNOW EFFECTS
        /// </summary>
        /// <param name="terrain"></param>
        public void ResetTerrain(Terrain terrain)
        {
            if (terrainIsCloned)
            {
                //Reset in memory object to original material, data.
                terrain.materialTemplate = terrMat;
                terrain.terrainData = originalTerrainData;

            }
            if (terrGameObject != null)
            {
                //Destroy snow mesh object that covers section of terrain.
                GameObject.Destroy(terrGameObject);
                terrGameObject = null;
            }
            terrainIsCloned = false;

        }

        /// <summary>
        /// CALLED BY EXTERANL CLASS TO DESTROY ALL THE GAME OBJECTS GENERATED SNOW COVER OBJECTS.
        /// </summary>
        public void DestroySnowObjects()
        {
            foreach (GameObject snowObject in snowObjects)
            {
                GameObject.Destroy(snowObject);
            }
            snowObjects.Clear();
        }
 

    }
}
