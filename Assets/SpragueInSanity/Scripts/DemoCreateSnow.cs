using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
namespace SpragueInSanity
{
    public class DemoCreateSnow : MonoBehaviour
    {
        [SerializeField] Terrain terrainObject; //Terrain
        private CreateSnow createSnow;
        private UISnowOptionsSettings uiSnowOptionsSettings;
        // Start is called before the first frame update
        void Start()
        {
            uiSnowOptionsSettings = new UISnowOptionsSettings();
            createSnow = new CreateSnow();
            LoadSettings();
            createSnow.CreateSnowForTerrain(terrainObject, uiSnowOptionsSettings.startPosition, uiSnowOptionsSettings.width, uiSnowOptionsSettings.depth, uiSnowOptionsSettings.farOffSnowCoverage);
            createSnow.CreateSnowForObjects(uiSnowOptionsSettings.snowObjectsFile);
            createSnow.CreateSnowForSpeedTrees(uiSnowOptionsSettings.treeSnowAmount, uiSnowOptionsSettings.snowShader);
            createSnow.RemoveTerrainPaintDetails(terrainObject, uiSnowOptionsSettings.startPosition, uiSnowOptionsSettings.width, uiSnowOptionsSettings.depth);

        }

        // Update is called once per frame
        void Update()
        {

        }
        private void OnDestroy()
        {
            createSnow.ResetSpeedTrees(uiSnowOptionsSettings.defaultTreeShader);
            createSnow.ResetTerrain(terrainObject);
            createSnow.DestroySnowObjects();

        }
        private void LoadSettings()
        {

            // Automatically handle missing file.
            if (!File.Exists(Application.dataPath + "/UISnowOptionsSettings.json"))
            {
                SaveSettings(); //CREATE/INITIALIZE DEFAULT FILE IF NOT CREATED YET
            }
            string fileData = File.ReadAllText(Application.dataPath + "/UISnowOptionsSettings.json");
            uiSnowOptionsSettings = JsonUtility.FromJson<UISnowOptionsSettings>(fileData);

        }
        private void SaveSettings()
        {
            // save all configuration data to json file.
            string jsonData = JsonUtility.ToJson(uiSnowOptionsSettings, true);
            File.WriteAllText(Application.dataPath + "/UISnowOptionsSettings.json", jsonData);
        }
    }
}

