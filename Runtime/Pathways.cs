using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tirt.Pathways
{
    /// <summary>
    /// The global static facade for Pathways. Manages configurations, active profiles, and auto-saves.
    /// </summary>
    public static class Pathways
    {
        private static string storageLocation;
        private static SaveProfile activeProfile;
        private static readonly Dictionary<string, SaveProfile> loadedProfiles = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Event triggered when an auto-save path is requested. Provides the target save path.
        /// </summary>
        public static event Action<string> OnAutoSavePathRequested;

        /// <summary>
        /// Event triggered when the active save profile is changed.
        /// </summary>
        public static event Action<SaveProfile> OnActiveProfileChanged;

        /// <summary>
        /// The root directory where all profiles are saved.
        /// </summary>
        public static string StorageLocation
        {
            get => string.IsNullOrEmpty(storageLocation) ? Application.persistentDataPath : storageLocation;
            set
            {
                storageLocation = value;
                Refresh();
            }
        }

        /// <summary>
        /// File extension for all save files. Defaults to "sav".
        /// </summary>
        public static string SaveExtension { get; set; } = "sav";

        /// <summary>
        /// Prefix for auto-saved files. Defaults to "auto_save_".
        /// </summary>
        public static string AutoSavePrefix { get; set; } = "auto_save_";

        /// <summary>
        /// Filename for quicksave. Defaults to "quicksave".
        /// </summary>
        public static string QuickSaveFileName { get; set; } = "quicksave";

        /// <summary>
        /// Maximum number of auto-save slots. Defaults to 3.
        /// </summary>
        public static int AutoSaveSlots { get; set; } = 3;

        /// <summary>
        /// The currently active save profile.
        /// </summary>
        public static SaveProfile ActiveProfile => activeProfile;

        /// <summary>
        /// Whether the auto-save system is currently enabled.
        /// </summary>
        public static bool IsAutoSaveEnabled { get; private set; }

        /// <summary>
        /// Time interval in seconds between auto-saves.
        /// </summary>
        public static float AutoSaveInterval { get; private set; }

        /// <summary>
        /// If true, auto-save timer uses unscaled time. Otherwise, uses scaled time.
        /// </summary>
        public static bool UseUnscaledTime { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            activeProfile = null;
            loadedProfiles.Clear();
            OnAutoSavePathRequested = null;
            OnActiveProfileChanged = null;
            IsAutoSaveEnabled = false;
            AutoSaveInterval = 0f;
            UseUnscaledTime = false;

            storageLocation = null;
            SaveExtension = "sav";
            AutoSavePrefix = "auto_save_";
            QuickSaveFileName = "quicksave";
            AutoSaveSlots = 3;

            InternalTicker.DestroyInstance();
        }

        /// <summary>
        /// Creates or loads a save profile by ID.
        /// </summary>
        /// <param name="profileId">The name of the profile directory.</param>
        /// <param name="setActive">If true, immediately sets this profile as the active profile.</param>
        /// <returns>The SaveProfile instance.</returns>
        public static SaveProfile LoadProfile(string profileId, bool setActive = true)
        {
            if (string.IsNullOrEmpty(profileId))
                throw new ArgumentException("Profile ID cannot be null or empty.");

            if (!loadedProfiles.TryGetValue(profileId, out SaveProfile profile))
            {
                profile = new SaveProfile(profileId);
                loadedProfiles[profileId] = profile;
            }

            if (setActive)
                SetActiveProfile(profile);

            return profile;
        }

        /// <summary>
        /// Switches the active profile to the profile with the given ID.
        /// </summary>
        public static void SetActiveProfile(string profileId)
        {
            var profile = LoadProfile(profileId, false);
            SetActiveProfile(profile);
        }

        /// <summary>
        /// Switches the active profile to the given instance.
        /// </summary>
        public static void SetActiveProfile(SaveProfile profile)
        {
            activeProfile = profile;

            if (IsAutoSaveEnabled)
            {
                InternalTicker.EnsureCreated();
                if (InternalTicker.Instance != null)
                    InternalTicker.Instance.ResetTimer();
            }

            OnActiveProfileChanged?.Invoke(activeProfile);
        }

        /// <summary>
        /// Detects and switches to the most recently updated save profile.
        /// </summary>
        /// <returns>The most recent SaveProfile, or null if none exist.</returns>
        public static SaveProfile SelectRecentProfile()
        {
            string[] ids = GetAllProfileIds();
            foreach (var id in ids)
            {
                if (!loadedProfiles.ContainsKey(id))
                    loadedProfiles[id] = new SaveProfile(id);
            }

            SaveProfile recent = loadedProfiles
                .Values.OrderByDescending(p => p.RecentFile?.LastWriteTime)
                .FirstOrDefault();

            if (recent != null)
                SetActiveProfile(recent);

            return recent;
        }

        /// <summary>
        /// Gets all loaded save profiles.
        /// </summary>
        public static SaveProfile[] GetAllProfiles()
        {
            string[] ids = GetAllProfileIds();
            foreach (var id in ids)
            {
                if (!loadedProfiles.ContainsKey(id))
                    loadedProfiles[id] = new SaveProfile(id);
            }

            return loadedProfiles.Values.ToArray();
        }

        /// <summary>
        /// Scans StorageLocation and returns the directory names of all profiles.
        /// </summary>
        public static string[] GetAllProfileIds()
        {
            var dir = new DirectoryInfo(StorageLocation);
            if (!dir.Exists)
                return Array.Empty<string>();

            return dir.GetDirectories().Select(d => d.Name).ToArray();
        }

        /// <summary>
        /// Deletes the profile directory and removes it from memory cache.
        /// </summary>
        /// <returns>True if deleted successfully.</returns>
        public static bool DeleteProfile(string profileId)
        {
            if (loadedProfiles.TryGetValue(profileId, out SaveProfile profile))
            {
                bool deleted = profile.Delete();
                loadedProfiles.Remove(profileId);

                if (activeProfile == profile)
                {
                    activeProfile = null;
                    OnActiveProfileChanged?.Invoke(null);
                }

                return deleted;
            }

            string fullPath = Path.Combine(StorageLocation, profileId);
            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, true);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Convenience method to resolve a path for the active profile.
        /// </summary>
        public static string GetPath(SaveType type, string customName = null)
        {
            if (activeProfile == null)
                throw new InvalidOperationException("Cannot resolve path: No active profile is loaded.");

            return activeProfile.GetPath(type, customName);
        }

        /// <summary>
        /// Convenience method to find the most recent save file path for the active profile.
        /// </summary>
        public static string GetLatest(SaveType? type = null) => activeProfile?.GetLatest(type);

        /// <summary>
        /// Convenience method to retrieve all existing save files for the active profile.
        /// </summary>
        public static FileInfo[] GetSaves(SaveType? type = null) =>
            activeProfile?.GetSaves(type) ?? Array.Empty<FileInfo>();

        /// <summary>
        /// Enables the auto-save system.
        /// </summary>
        /// <param name="interval">Save interval in seconds.</param>
        /// <param name="slots">Number of backup save slots to cycle through.</param>
        public static void EnableAutoSave(float interval, int slots = 3)
        {
            IsAutoSaveEnabled = true;
            AutoSaveInterval = interval;
            AutoSaveSlots = slots;

            InternalTicker.EnsureCreated();
            if (InternalTicker.Instance != null)
                InternalTicker.Instance.ResetTimer();
        }

        /// <summary>
        /// Disables the auto-save system.
        /// </summary>
        public static void DisableAutoSave()
        {
            IsAutoSaveEnabled = false;
            InternalTicker.DestroyInstance();
        }

        /// <summary>
        /// Restarts the timer of the auto-save loop.
        /// </summary>
        public static void RestartAutoSaveTimer()
        {
            if (InternalTicker.Instance != null)
                InternalTicker.Instance.ResetTimer();
        }

        /// <summary>
        /// Manually requests an auto-save path and fires the notification event.
        /// </summary>
        public static string RequestAutoSavePath()
        {
            if (activeProfile == null)
                return null;

            string path = activeProfile.GetPath(SaveType.AutoSave);
            OnAutoSavePathRequested?.Invoke(path);
            activeProfile.Refresh();
            return path;
        }

        /// <summary>
        /// Clears the memory cache and re-scans the storage directory.
        /// </summary>
        public static void Refresh()
        {
            loadedProfiles.Clear();
            activeProfile = null;

            string[] ids = GetAllProfileIds();
            foreach (var id in ids)
                loadedProfiles[id] = new SaveProfile(id);
        }

        private class InternalTicker : MonoBehaviour
        {
            private static InternalTicker instance;
            public static InternalTicker Instance => instance;

            private float autoSaveTimer;

            public static void EnsureCreated()
            {
                if (instance != null)
                    return;
                if (!Application.isPlaying)
                    return;

                var go = new GameObject("Pathways_Internal_Ticker") { hideFlags = HideFlags.HideAndDontSave };
                DontDestroyOnLoad(go);
                instance = go.AddComponent<InternalTicker>();
            }

            public static void DestroyInstance()
            {
                if (instance != null)
                {
                    DestroyImmediate(instance.gameObject);
                    instance = null;
                }
            }

            public void ResetTimer()
            {
                autoSaveTimer = 0f;
            }

            private void Update()
            {
                if (activeProfile == null || !IsAutoSaveEnabled)
                    return;

                if (AutoSaveSlots <= 0 || AutoSaveInterval <= 0f)
                    return;

                autoSaveTimer += UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                if (autoSaveTimer >= AutoSaveInterval)
                {
                    RequestAutoSavePath();
                    autoSaveTimer = 0f;
                }
            }
        }
    }
}
