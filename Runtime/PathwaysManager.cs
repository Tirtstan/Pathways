using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Pathways
{
    [DefaultExecutionOrder(-1000)]
    public class PathwaysManager : MonoBehaviour
    {
        private static PathwaysManager instance;
        public static PathwaysManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<PathwaysManager>();
                    if (instance == null)
                    {
                        var go = new GameObject(nameof(PathwaysManager) + " (Auto Created)");
                        instance = go.AddComponent<PathwaysManager>();
                    }
                }
                return instance;
            }
        }

        /// <summary>
        /// Event triggered when an auto-save path is requested. Provides the path for the auto-save file.
        /// </summary>
        public event Action<string> OnAutoSavePathRequested;

        /// <summary>
        /// Event triggered when the current pathway changes. Provides the new pathway.
        /// </summary>
        public event Action<Pathway> OnCurrentPathwayChanged;

        public string StorageLocation
        {
            get => PathwaysGlobalConfigs.StorageLocation;
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException("Storage location cannot be null or empty.");

                PathwaysGlobalConfigs.StorageLocation = value;
                Refresh();
            }
        }

        public bool IsAutoSaveEnabled { get; private set; }
        public int AutoSaveSlots
        {
            get => PathwaysGlobalConfigs.AutoSaveSlots;
            private set => PathwaysGlobalConfigs.AutoSaveSlots = value;
        }
        public float AutoSaveInterval { get; private set; }
        public bool UseUnscaledTime { get; private set; }
        public Pathway CurrentPathway { get; private set; }

        private readonly Dictionary<string, Pathway> loadedPathways = new();
        private float autoSaveTimer;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            Refresh();
        }

        private void Update()
        {
            if (CurrentPathway == null)
                return;

            if (CanAutoSave())
            {
                autoSaveTimer += UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                if (autoSaveTimer >= AutoSaveInterval)
                {
                    RequestAutoSavePath();
                    autoSaveTimer = 0f;
                }
            }
        }

        /// <summary>
        /// Sets the storage location for all the pathways. Will trigger a refresh of the pathways.
        /// </summary>
        /// <param name="location">New storage location path.</param>
        public void SetStorageLocation(string location) => StorageLocation = location;

        /// <summary>
        /// Creates or loads a pathway with the given ID. By default, the loaded/created pathway becomes the current pathway.
        /// </summary>
        /// <param name="pathwayId">The pathway identifier (directory name).</param>
        /// <param name="setCurrent">If true, sets this pathway as the current pathway.</param>
        /// <returns>The Pathway instance.</returns>
        public Pathway CreateOrLoadPathway(string pathwayId, bool setCurrent = true)
        {
            if (loadedPathways.TryGetValue(pathwayId, out Pathway existingPathway))
            {
                if (setCurrent)
                    SetCurrentPathway(existingPathway);
                return existingPathway;
            }

            var pathway = new Pathway(pathwayId);
            loadedPathways[pathwayId] = pathway;

            if (setCurrent)
                SetCurrentPathway(pathway);

            return pathway;
        }

        /// <summary>
        /// Checks if auto-saving is enabled and if the current pathway is valid for auto-saving.
        /// Checks if auto-save slots and interval are set correctly (greater than 0).
        /// </summary>
        /// <returns>True if auto-saving can proceed, false otherwise.</returns>
        public bool CanAutoSave() =>
            IsAutoSaveEnabled && CurrentPathway != null && AutoSaveSlots > 0 && AutoSaveInterval > 0f;

        /// <summary>
        /// Toggles auto-saving for the current pathway.
        /// </summary>
        /// <param name="enable">To enable or disable auto-saving.</param>
        public void ToggleAutoSave(bool enable) => IsAutoSaveEnabled = enable;

        /// <summary>
        /// Sets whether auto-saving is enabled for the current pathway.
        /// </summary>
        /// <param name="enable">To enable or disable auto-saving.</param>
        /// <param name="slots">Number of auto-save slots to use. Defaults to 3.</param>
        /// <param name="interval">Auto-save interval in seconds. Defaults to 300 seconds (5 minutes).</param>
        public void ToggleAutoSave(bool enable, int slots = 3, float interval = 300f)
        {
            IsAutoSaveEnabled = enable;
            AutoSaveSlots = slots;
            AutoSaveInterval = interval;
        }

        /// <summary>
        /// Restarts the auto-save timer.
        /// </summary>
        public void RestartAutoSaveTimer() => autoSaveTimer = 0f;

        /// <summary>
        /// Sets whether to use unscaled time for auto-saving.
        /// </summary>
        /// <param name="useUnscaled">If true, uses unscaled time for auto-saving; otherwise, uses scaled time.</param>
        public void SetTimescale(bool useUnscaled) => UseUnscaledTime = useUnscaled;

        /// <summary>
        /// Selects and sets the most recent (last saved to) pathway as the current pathway.
        /// If no recent pathway exists, returns null.
        /// </summary>
        /// <returns>The most recent Pathway instance, or null if none found.</returns>
        public Pathway SelectRecentPathway()
        {
            Pathway recentPathway = loadedPathways
                .OrderByDescending(kvp => kvp.Value.RecentFile?.LastWriteTime)
                .FirstOrDefault()
                .Value;

            if (recentPathway == null)
                return null;

            return SetCurrentPathway(recentPathway);
        }

        /// <summary>
        /// Sets the current pathway, creating a new one if not found within already loaded pathways.
        /// </summary>
        /// <param name="pathwayId">The ID of the pathway to make current.</param>
        public Pathway SetCurrentPathway(string pathwayId)
        {
            Pathway pathway = CreateOrLoadPathway(pathwayId);
            SetCurrentPathway(pathway);

            return pathway;
        }

        /// <summary>
        /// Sets the current pathway.
        /// </summary>
        /// <param name="pathway">The pathway to make current.</param>
        private Pathway SetCurrentPathway(Pathway pathway)
        {
            CurrentPathway = pathway;
            autoSaveTimer = 0f;

            OnCurrentPathwayChanged?.Invoke(CurrentPathway);
            return CurrentPathway;
        }

        /// <summary>
        /// Gets all available pathway ids.
        /// </summary>
        /// <returns>Array of pathway ids (directory names).</returns>
        public string[] GetAllPathwayIds()
        {
            var storageDir = new DirectoryInfo(StorageLocation);
            if (!storageDir.Exists)
                return new string[0];

            return storageDir.GetDirectories().Select(d => d.Name).ToArray();
        }

        /// <summary>
        /// Gets the path of the most recent save file in the current pathway. If no save file exists, it generates a new manual save path.
        /// Ideal for a "Save" button that overwrites the last save or creates a new one.
        /// </summary>
        /// <returns>A valid save path, or null if no pathway is current.</returns>
        public string GetOrCreateRecentSavePath(string fileName = null)
        {
            if (CurrentPathway == null)
                return null;

            if (!string.IsNullOrEmpty(fileName))
                return CurrentPathway.GetSavePath(fileName);

            return CurrentPathway.GetRecentSavePath() ?? CurrentPathway.GetSavePath();
        }

        /// <summary>
        /// Gets the path for a manual save in the current pathway.
        /// </summary>
        /// <param name="fileName">Optional filename, defaults to timestamp-based name.</param>
        /// <returns>Full path for the save file, or null if no pathway is current.</returns>
        public string GetManualSavePath(string fileName) => CurrentPathway?.GetSavePath(fileName);

        /// <summary>
        /// Gets the path for an auto-save in the current pathway.
        /// </summary>
        /// <returns>Full path for the auto-save file, or null if no pathway is current.</returns>
        public string GetAutoSavePath() => CurrentPathway?.GetAutoSavePath();

        /// <summary>
        /// Requests the path for an auto-save and notifies subscribers.
        /// </summary>
        /// <returns>The auto-save path, or null if no pathway is current.</returns>
        public string RequestAutoSavePath()
        {
            if (CurrentPathway == null)
                return null;

            string path = CurrentPathway.GetAutoSavePath();
            OnAutoSavePathRequested?.Invoke(path);

            RefreshCurrentPathway();
            return path;
        }

        /// <summary>
        /// Deletes the current pathway. If successful, refreshes the pathways list.
        /// </summary>
        /// <returns>Whether the pathway was deleted.</returns>
        public bool DeleteCurrentPathway()
        {
            bool success = CurrentPathway?.Delete() ?? false;
            if (success)
                Refresh();

            return success;
        }

        /// <summary>
        /// Deletes a specific file within the current pathway. If successful, refreshes the current pathway.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>Whether the file within the current pathway was deleted.</returns>
        public bool DeleteFile(string fileName)
        {
            bool success = CurrentPathway?.DeleteFile(fileName) ?? false;
            if (success)
                RefreshCurrentPathway();

            return success;
        }

        /// <summary>
        /// Refreshes the file list for the current pathway.
        /// </summary>
        public void RefreshCurrentPathway() => CurrentPathway?.Refresh();

        /// <summary>
        /// Searches through <see cref="StorageLocation"/> to get all pathways.
        /// </summary>
        public void Refresh()
        {
            loadedPathways.Clear();
            CurrentPathway = null;

            string[] pathwayIds = GetAllPathwayIds();
            foreach (var pathwayId in pathwayIds)
                CreateOrLoadPathway(pathwayId);
        }

        /// <summary>
        /// Gets all loaded pathways. Use <see cref="Refresh"/> to update the list.
        /// </summary>
        /// <returns>Array of Pathway instances.</returns>
        public Pathway[] GetAllPathways() => loadedPathways.Values.ToArray();

        private void OnApplicationQuit() => instance = null;
    }
}
