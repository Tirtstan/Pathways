using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tirt.Pathways;
using UnityEngine;
using UnityEngine.InputSystem;

public class SaveManager : MonoBehaviour
{
    [Header("Components")]
    [SerializeField]
    private Item itemPrefab;

    private void Awake()
    {
        // 1. Configure the static Pathways configurations (default: Application.persistentDataPath)
        Pathways.StorageLocation = Path.Combine(Application.persistentDataPath, "Saves");

        // 2. Load our save profile. All path operations will work relative to this active profile.
        Pathways.LoadProfile("Spire Coast");

        // 3. Register the auto-save event and enable the system
        Pathways.OnAutoSavePathRequested += OnAutoDataPathRequested;
        Pathways.EnableAutoSave(interval: 300f, slots: 3);
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        // Select the most recently modified profile
        if (Keyboard.current.rKey.wasPressedThisFrame)
            SelectRecentProfile();

        // Spawn some random items to save
        if (Keyboard.current.cKey.wasPressedThisFrame)
            CreateRandomItems();

        // Manual Save (creates a new timestamped file)
        if (Keyboard.current.sKey.wasPressedThisFrame)
            SaveGameData(Pathways.GetPath(SaveType.Manual));

        // Manual Load (loads the most recent save file of any type)
        if (Keyboard.current.lKey.wasPressedThisFrame)
            LoadGameData(Pathways.GetLatest());

        // Quick Save (creates or overwrites a single dedicated quicksave file)
        if (Keyboard.current.qKey.wasPressedThisFrame)
            SaveGameData(Pathways.GetPath(SaveType.QuickSave));
#endif
    }

    private void SelectRecentProfile()
    {
        SaveProfile profile = Pathways.SelectRecentProfile();
        if (profile != null)
        {
            Debug.Log($"Switched to most recent SaveProfile: {profile.ProfileId}");
        }
        else
        {
            Debug.LogWarning("No recent save profile found.");
        }
    }

    private void CreateRandomItems()
    {
        for (int i = 0; i < 10; i++)
        {
            Item item = Instantiate(itemPrefab, Random.insideUnitCircle * 5f, Quaternion.identity);
            item.RandomiseProperties();
        }

        Debug.Log("Created 10 random items in scene");
    }

    private void OnAutoDataPathRequested(string autoDataPath)
    {
        SaveDataToPath(autoDataPath);
        Debug.Log($"Auto-saved game state to: {autoDataPath}");
    }

    public void SaveGameData(string targetPath)
    {
        if (string.IsNullOrEmpty(targetPath))
        {
            Debug.LogError("Save failed: Target path is null or empty. Ensure a save profile is active.");
            return;
        }

        SaveDataToPath(targetPath);
        Pathways.ActiveProfile?.Refresh(); // Refresh the profile so it updates its file lists immediately

        Debug.Log($"Saved game data to: {targetPath} | Active Profile: {Pathways.ActiveProfile}");
    }

    public void LoadGameData(string loadPath)
    {
        if (string.IsNullOrEmpty(loadPath) || !File.Exists(loadPath))
        {
            Debug.LogWarning($"Load failed: No save file found at path '{loadPath}'");
            return;
        }

        LevelData levelData = LoadDataFromPath(loadPath);
        ApplyLevelData(levelData);

        Debug.Log($"Loaded game data from: {loadPath} | Spawned Items: {levelData?.SaveData?.Length ?? 0}");
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

        if (levelData?.SaveData == null)
            return;

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
        Pathways.OnAutoSavePathRequested -= OnAutoDataPathRequested;
    }
}
