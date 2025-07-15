# Pathways

A Unity package that provides a comprehensive set of tools to manage data file pathways and directories without handling actual data persistence. Perfect for save systems and sessions, user profiles, level data, and any scenario requiring organized file management.

## Features

-   **Pathway Management**: Create and manage data with `Pathway` (directories).
-   **Auto-Data System**: Automatic data saving through events with configurable slots and intervals.
-   **Manual Data Operations**: Full control over manual data saving and loading.
-   **File Organization**: Separate manual and auto-data files with default or custom naming.
-   **Recent Pathway Selection**: Quickly switch to the most recently used `Pathway`.

## Requirements

-   Unity 2019.4 or later

## Quick Start

Pathways uses a `PathwaysManager` singleton to manage and interface with the API. It is automatically added to a scene and is configured to `DontDestroyOnLoad`.
If you want, you can add an instance of it yourself within a given scene.

> [!NOTE]  
> `PathwaysManager` (during runtime) provides useful debug information and tooling.

### Basic Setup

Optionally configure basic settings:

```csharp
// Set custom storage location (defaults to 'Application.persistentDataPath')
PathwaysManager.Instance.SetStorageLocation(
    Path.Combine(Application.persistentDataPath, "Saves")
);

// Create or load a pathway (directory) and set it as current (true by default)
Pathway pathway = PathwaysManager.Instance.CreateOrLoadPathway("SaveSession1", setCurrent: true);
```

### Auto-Save Configuration

Set up automatic saving with customisable intervals and slot rotation:

```csharp
// Enable auto-data with 3 slots, saving every 2 minutes
PathwaysManager.Instance.ToggleAutoSave(true);
PathwaysManager.Instance.SetAutoSaveSlots(3); // cycles through 3 auto save files
PathwaysManager.Instance.SetAutoSaveInterval(120f); // 2 minutes

// Subscribe to auto-save events
PathwaysManager.Instance.OnAutoSavePathRequested += (autoSavePath) =>
{
    string gameData = CreateGameDataJson();
    File.WriteAllText(autoSavePath, gameData);
    Debug.Log($"Auto-saved to: {autoSavePath}");
};
```

## Core Functionality

### Manual Operations

#### Save To Current Pathway

```csharp
// Save with automatic timestamp-based filename
string savePath = PathwaysManager.Instance.GetManualSavePath();
SaveGameDataToPath(savePath);

// Save with custom filename
string customPath = PathwaysManager.Instance.GetManualSavePath("MyCustomSave.json");
SaveGameDataToPath(customPath);

// Always refresh pathway after saving
PathwaysManager.Instance.RefreshCurrentPathway();
```

#### Load To Current Pathway

```csharp
// Load most recent manual data file
FileInfo recentFile = PathwaysManager.Instance.GetRecentManualSaveFile();
if (recentFile != null)
{
    string jsonData = File.ReadAllText(recentFile.FullName);
    GameData data = JsonUtility.FromJson<GameData>(jsonData);
    ApplyGameData(data);
}

// Load specific data file
string specificPath = PathwaysManager.Instance.GetManualSavePath("MyCustomSave");
if (File.Exists(specificPath))
{
    string jsonData = File.ReadAllText(specificPath);
    GameData data = JsonUtility.FromJson<GameData>(jsonData);
    ApplyGameData(data);
}
```

### Pathway Management

#### Switch Between Pathways

```csharp
// Create or switch to a specific pathway using its pathwayId (directory name)
Pathway levelPathway = PathwaysManager.Instance.SetCurrentPathway("SaveSession1");

// Select the most recent pathway (last written to)
Pathway recentPathway = PathwaysManager.Instance.SelectRecentPathway();
if (recentPathway != null)
{
    Debug.Log($"Switched to recent pathway: {recentPathway.PathwayId}");
}
```

#### Get Pathway Information

```csharp
Pathway currentPathway = PathwaysManager.Instance.CurrentPathway;
if (currentPathway != null)
{
    Debug.Log($"Pathway: {currentPathway}"); // outputs: Pathway: {PathwayId}, Files: {FileCount}, Full Path: {Path}
}
```

### File Management

#### Get All Save Files

```csharp
// Get all files in current pathway
FileInfo[] allFiles = PathwaysManager.Instance.GetAllSaveFiles();

// Get only manual save files (newest first)
FileInfo[] manualFiles = PathwaysManager.Instance.GetManualSaveFiles();

// Get only auto-save files (newest first)
FileInfo[] autoFiles = PathwaysManager.Instance.GetAutoSaveFiles();

foreach (var file in manualFiles)
{
    Debug.Log($"Manual save: {file.Name} ({file.LastWriteTime})");
}
```

#### File Operations

```csharp
// Check if a specific file exists
bool fileExists = PathwaysManager.Instance.FileExists("MyCustomSave.sav");

// Delete a specific data file
bool deleted = PathwaysManager.Instance.DeleteFile("OldSave.sav");

// Delete current pathway and all its files
bool pathwayDeleted = PathwaysManager.Instance.DeleteCurrentPathway();
```

### Advanced Features

#### Multiple Pathway Management

```csharp
// Get all available pathway IDs (directory names)
string[] allPathwayIds = PathwaysManager.Instance.GetAllPathwayIds();

// Load specific pathways without switching
Pathway level1 = PathwaysManager.Instance.CreateOrLoadPathway("Level_01", setCurrent: false);
Pathway level2 = PathwaysManager.Instance.CreateOrLoadPathway("Level_02", setCurrent: false);

// Get all loaded pathways
Pathway[] loadedPathways = PathwaysManager.Instance.GetAllPathways();
```

#### Event Handling

```csharp
// Listen for current pathway changes
PathwaysManager.Instance.OnCurrentPathwayChanged += (newPathway) =>
{
    Debug.Log($"Pathway changed to: {newPathway.PathwayId}");
    UpdateUI();
};

// Handle auto-data requests
PathwaysManager.Instance.OnAutoSavePathRequested += (autoSavePath) =>
{
    PerformAutoSave(autoSavePath);
};
```

#### Time Configuration

```csharp
// Use unscaled time for auto-save
PathwaysManager.Instance.SetTime(useUnscaled: true);

// Manually restart the auto-save timer
PathwaysManager.Instance.RestartAutoSaveTimer();
```

## Configuration

### Global Settings

Customise the global behaviour through [`PathwaysGlobalConfigs`](Runtime/PathwaysGlobalConfigs.cs):

> [!NOTE]  
> It is recommended to set `PathwaysGlobalConfigs.StorageLocation` using `PathwaysManager.SetStorageLocation(string)` as it will refresh the stored pathways automatically.

```csharp
// Set custom file extension
PathwaysGlobalConfigs.SaveExtension = "json";

// Change auto-save prefix
PathwaysGlobalConfigs.AutoSavePrefix = "autosave_";

// Set default storage location
PathwaysGlobalConfigs.StorageLocation = Path.Combine(Application.persistentDataPath, "Saves");
// OR (will auto refresh pathways)
PathwaysManager.Instance.SetStorageLocation(Path.Combine(Application.persistentDataPath, "Saves"));
```

## Example Implementation

Here's a complete example showing how to implement a basic save system:

```csharp
public class GameSaveSystem : MonoBehaviour
{
    private void Awake()
    {
        InitializePathways();
        SetupAutoSave();
    }

    private void InitializePathways()
    {
        PathwaysManager.Instance.SetStorageLocation(
            Path.Combine(Application.persistentDataPath, "GameSaves")
        );

        // Create or load pathway
        PathwaysManager.Instance.CreateOrLoadPathway("World1");
    }

    private void SetupAutoSave()
    {
        PathwaysManager.Instance.ToggleAutoSave(true);
        PathwaysManager.Instance.SetAutoSaveSlots(3);
        PathwaysManager.Instance.SetAutoSaveInterval(300f); // 5 minutes

        PathwaysManager.Instance.OnAutoSavePathRequested += SaveGameToPath;
    }

    public void SaveGame()
    {
        string savePath = PathwaysManager.Instance.GetManualSavePath();
        SaveGameToPath(savePath);
        PathwaysManager.Instance.RefreshCurrentPathway();
    }

    public void LoadGame()
    {
        FileInfo recentSave = PathwaysManager.Instance.GetRecentManualSaveFile();
        if (recentSave != null)
        {
            LoadGameFromPath(recentSave.FullName);
        }
    }

    private void SaveGameToPath(string path)
    {
        // You handle data serialization and persistence using the provided save path
        var gameData = new GameData();
        string json = JsonUtility.ToJson(gameData);
        File.WriteAllText(path, json);
    }

    private void LoadGameFromPath(string path)
    {
        string json = File.ReadAllText(path);
        GameData gameData = JsonUtility.FromJson<GameData>(json);
        ApplyGameData(gameData);
    }
}
```

## Package Structure

-   **Runtime**: Core pathway management functionality

    -   [`Pathway.cs`](Runtime/Pathway.cs) - Individual pathway management
    -   [`PathwaysManager.cs`](Runtime/PathwaysManager.cs) - Central singleton manager and auto-save system
    -   [`PathwaysGlobalConfigs.cs`](Runtime/PathwaysGlobalConfigs.cs) - Global configuration settings

-   **Editor**: Unity Editor integration

    -   [`PathwaysEditor.cs`](Editor/PathwaysEditor.cs) - Custom inspector for PathwaysManager

-   **Samples**: Example implementations
    -   [Examples](Samples/Examples/) - Complete save system example with items

## Tips and Best Practices

1. **Always refresh**: Call `PathwaysManager.Instance.RefreshCurrentPathway()` after saving to update file lists.
2. **Error handling**: Check for null pathways and file existence before operations.
