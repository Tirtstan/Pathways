using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Pathways
{
    public class PathwaysManager : MonoBehaviour
    {
        public static PathwaysManager Instance { get; private set; }

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

        public Pathway CurrentPathway { get; private set; }
        public bool CanAutoSave { get; private set; }
        public int AutoSaveSlots
        {
            get => CurrentPathway?.AutoSaveSlots ?? 0;
            private set
            {
                if (CurrentPathway != null)
                    CurrentPathway.AutoSaveSlots = value;
            }
        }
        public float AutoSaveInterval { get; private set; }
        public bool UseUnscaledTime { get; private set; }

        private readonly Dictionary<string, Pathway> loadedPathways = new();
        private float autoSaveTimer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeSingletonOnLoad()
        {
            Instance = FindFirstObjectByType<PathwaysManager>();

            if (Instance == null)
            {
                var singletonObject = new GameObject(typeof(PathwaysManager).Name);
                Instance = singletonObject.AddComponent<PathwaysManager>();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Refresh();
        }

        private void Update()
        {
            if (CurrentPathway == null)
                return;

            if (CanAutoSave)
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
        /// Creates or loads a pathway with the given ID. By default, when created, that pathway becomes the current pathway.
        /// </summary>
        /// <param name="pathwayId">The pathway identifier.</param>
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
        /// Sets whether auto-saving is enabled for the current pathway.
        /// </summary>
        /// <param name="enabled">To enable or disable auto-saving.</param>
        public void ToggleAutoSave(bool enabled) => CanAutoSave = enabled;

        /// <summary>
        /// Sets the number of auto-save slots to use for the current pathway.
        /// </summary>
        /// <param name="slots">The number of auto-save slots to cycle through.</param>
        public void SetAutoSaveSlots(int slots = 3) => AutoSaveSlots = slots;

        /// <summary>
        /// Sets the auto-save interval (seconds) to use for the current pathway.
        /// </summary>
        /// <param name="interval">The auto-save interval in seconds. Defaults to 300 seconds (5 minutes).</param>
        public void SetAutoSaveInterval(float interval = 300f) => AutoSaveInterval = interval;

        /// <summary>
        /// Restarts the auto-save timer.
        /// </summary>
        public void RestartAutoSaveTimer() => autoSaveTimer = 0f;

        /// <summary>
        /// Sets whether to use unscaled time for auto-saving.
        /// </summary>
        /// <param name="useUnscaled">If true, uses unscaled time for auto-saving; otherwise, uses scaled time.</param>
        public void SetTime(bool useUnscaled) => UseUnscaledTime = useUnscaled;

        /// <summary>
        /// Selects and sets the most recent (last saved to) pathway as the current active pathway.
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
        /// Sets the current active pathway, creating a new one if not found within already loaded pathways.
        /// </summary>
        /// <param name="pathwayId">The ID of the pathway to make active.</param>
        public Pathway SetCurrentPathway(string pathwayId)
        {
            Pathway pathway = CreateOrLoadPathway(pathwayId);
            SetCurrentPathway(pathway);

            return pathway;
        }

        /// <summary>
        /// Sets the current active pathway.
        /// </summary>
        /// <param name="pathway">The pathway to make active.</param>
        private Pathway SetCurrentPathway(Pathway pathway) // private as the pathway may be not in the loaded, use above then
        {
            CurrentPathway = pathway;
            autoSaveTimer = 0f;

            OnCurrentPathwayChanged?.Invoke(CurrentPathway);
            return CurrentPathway;
        }

        /// <summary>
        /// Gets all available save pathways.
        /// </summary>
        /// <returns>Array of pathway directory names (ids).</returns>
        public string[] GetAllPathwayIds()
        {
            var storageDir = new DirectoryInfo(StorageLocation);
            if (!storageDir.Exists)
                return new string[0];

            return storageDir.GetDirectories().Select(d => d.Name).ToArray();
        }

        /// <summary>
        /// Gets the path for a manual save in the current pathway.
        /// </summary>
        /// <param name="fileName">Optional filename, defaults to timestamp-based name.</param>
        /// <returns>Full path for the save file, or null if no pathway is active.</returns>
        public string GetManualSavePath(string fileName = null) => CurrentPathway?.GetSavePath(fileName);

        /// <summary>
        /// Gets the path for an auto-save in the current pathway.
        /// </summary>
        /// <returns>Full path for the auto-save file, or null if no pathway is active.</returns>
        public string GetAutoSavePath() => CurrentPathway?.GetAutoSavePath();

        /// <summary>
        /// Requests the path for an auto-save and notifies subscribers.
        /// </summary>
        /// <returns>The auto-save path, or null if no pathway is active.</returns>
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
        /// Gets all save files in the current pathway.
        /// </summary>
        /// <returns>Array of FileInfo objects, or empty if no pathway is active.</returns>
        public FileInfo[] GetAllSaveFiles() => CurrentPathway?.Files ?? new FileInfo[0];

        /// <summary>
        /// Gets all manual save files in the current pathway.
        /// </summary>
        /// <returns>Array of FileInfo objects, or empty if no pathway is active.</returns>
        public FileInfo[] GetManualSaveFiles() => CurrentPathway?.GetManualSaves() ?? new FileInfo[0];

        /// <summary>
        /// Gets all auto-save files in the current pathway.
        /// </summary>
        /// <returns>Array of FileInfo objects, or empty if no pathway is active.</returns>
        public FileInfo[] GetAutoSaveFiles() => CurrentPathway?.GetAutoSaves() ?? new FileInfo[0];

        /// <summary>
        /// Gets the most recent manual save file info.
        /// </summary>
        /// <returns>FileInfo of most recent manual save, or null if none exists.</returns>
        public FileInfo GetRecentManualSaveFile() => CurrentPathway?.GetManualSaves().FirstOrDefault();

        /// <summary>
        /// Gets the most recent auto-save file info.
        /// </summary>
        /// <returns>FileInfo of most recent auto-save, or null if none exists.</returns>
        public FileInfo GetMostRecentAutoSave() => CurrentPathway?.GetAutoSaves().FirstOrDefault();

        /// <summary>
        /// Refreshes the file list for the current pathway.
        /// </summary>
        public void RefreshCurrentPathway() => CurrentPathway?.Refresh();

        /// <summary>
        /// Refreshes through <see cref="StorageLocation"/> to get all pathways.
        /// </summary>
        public void Refresh()
        {
            loadedPathways.Clear();

            string[] pathwayIds = GetAllPathwayIds();
            foreach (var pathwayId in pathwayIds)
                CreateOrLoadPathway(pathwayId);
        }

        /// <summary>
        /// Gets all loaded pathways. Use <see cref="Refresh"/> to update the list.
        /// </summary>
        /// <returns>Array of Pathway instances.</returns>
        public Pathway[] GetAllPathways() => loadedPathways.Values.ToArray();
    }
}
