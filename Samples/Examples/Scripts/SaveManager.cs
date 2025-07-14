using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Pathways.Samples
{
    public class SaveManager : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField]
        private Item itemPrefab;

        private void Awake()
        {
            PathwaysManager.Instance.OnAutoSavePathRequested += OnAutoDataPathRequested;

            PathwaysManager.Instance.ToggleAutoSave(true);
            PathwaysManager.Instance.SetAutoSaveSlots(3);
            PathwaysManager.Instance.SetAutoSaveInterval(120f);

            PathwaysManager.Instance.SetStorageLocation(Path.Combine(Application.persistentDataPath, "GameData"));
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
                SelectRecentPathway();

            if (Input.GetKeyDown(KeyCode.C))
                CreateRandomItems();

            if (Input.GetKeyDown(KeyCode.S))
                SaveGameData();

            if (Input.GetKeyDown(KeyCode.L))
                LoadGameData();
        }

        private void SelectRecentPathway()
        {
            Pathway pathway = PathwaysManager.Instance.SelectRecentPathway();
            Debug.Log($"Selected Recent Pathway: {pathway}");
        }

        private void CreateRandomItems()
        {
            for (int i = 0; i < 10; i++)
            {
                Item item = Instantiate(itemPrefab, Random.insideUnitCircle * 5f, Quaternion.identity);
                item.RandomiseProperties();
            }
            Debug.Log("Created 10 random items");
        }

        private void OnAutoDataPathRequested(string autoDataPath)
        {
            SaveDataToPath(autoDataPath);
            Debug.Log($"Auto-saved data to: {autoDataPath}");
        }

        public void SaveGameData(string fileName = null)
        {
            string dataPath = PathwaysManager.Instance.GetManualSavePath(fileName);
            SaveDataToPath(dataPath);
            PathwaysManager.Instance.RefreshCurrentPathway();

            Debug.Log($"Manually saved data to: {dataPath} | Pathway: {PathwaysManager.Instance.CurrentPathway}");
        }

        public void LoadGameData(string fileName = null)
        {
            string loadPath = GetLoadPath(fileName);
            if (string.IsNullOrEmpty(loadPath))
                return;

            LevelData levelData = LoadDataFromPath(loadPath);
            ApplyLevelData(levelData);

            Debug.Log($"Loaded data from: {loadPath} with Items: {levelData?.SaveData?.Length}");
        }

        private string GetLoadPath(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                FileInfo recentData = PathwaysManager.Instance.GetRecentManualSaveFile();
                if (recentData == null)
                {
                    Debug.LogWarning("No data file found!");
                    return null;
                }
                return recentData.FullName;
            }
            else
            {
                string loadPath = PathwaysManager.Instance.GetManualSavePath(fileName);
                if (!File.Exists(loadPath))
                {
                    Debug.LogWarning($"Data file not found: {fileName}");
                    return null;
                }
                return loadPath;
            }
        }

        private void SaveDataToPath(string path)
        {
            string jsonData = CreateGameDataJson();
            File.WriteAllText(path, jsonData);
        }

        private LevelData LoadDataFromPath(string path)
        {
            string jsonData = File.ReadAllText(path);
            return JsonUtility.FromJson<LevelData>(jsonData);
        }

        private string CreateGameDataJson()
        {
            var data = new LevelData(GetAllSaveableItems().Select(item => item.GetData()));
            return JsonUtility.ToJson(data);
        }

        private void ApplyLevelData(LevelData levelData)
        {
            ClearExistingItems();

            foreach (var itemData in levelData.SaveData)
            {
                Item item = Instantiate(
                    itemPrefab,
                    new Vector2(itemData.PositionX, itemData.PositionY),
                    Quaternion.identity
                );
                item.SetData(itemData);
            }
        }

        private void ClearExistingItems()
        {
            ISaveable<ItemData>[] existingItems = GetAllSaveableItems().ToArray();
            foreach (var item in existingItems)
            {
                if (item is MonoBehaviour mb)
                    Destroy(mb.gameObject);
            }
        }

        private IEnumerable<ISaveable<ItemData>> GetAllSaveableItems() =>
            FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .OfType<ISaveable<ItemData>>();

        private void OnDestroy()
        {
            PathwaysManager.Instance.OnAutoSavePathRequested -= OnAutoDataPathRequested;
        }
    }
}
