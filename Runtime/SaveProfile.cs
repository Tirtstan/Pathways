using System;
using System.IO;
using System.Linq;

namespace Tirt.Pathways
{
    /// <summary>
    /// Represents a single save profile directory and manages its file locations.
    /// </summary>
    public class SaveProfile
    {
        private string profileId;
        private FileInfo[] cachedFiles;

        /// <summary>
        /// Unique identifier for the profile using the directory's name.
        /// </summary>
        public string ProfileId
        {
            get => profileId;
            set
            {
                profileId = value;
                Refresh();
            }
        }

        /// <summary>
        /// All files currently in the profile directory (automatically refreshed from disk).
        /// </summary>
        public FileInfo[] Files
        {
            get
            {
                Refresh();
                return cachedFiles ?? Array.Empty<FileInfo>();
            }
        }

        /// <summary>
        /// Full path to the profile directory.
        /// </summary>
        public string FullPath => Path.Combine(Pathways.StorageLocation, ProfileId);

        /// <summary>
        /// Total number of save files in the profile directory.
        /// </summary>
        public int FileCount => Files.Length;

        /// <summary>
        /// The most recent save file of any type in the profile based on last write time.
        /// </summary>
        public FileInfo RecentFile =>
            FileCount > 0 ? Files.OrderByDescending(f => f.LastWriteTime).FirstOrDefault() : null;

        public SaveProfile(string profileId)
        {
            this.profileId = profileId;
            Refresh();
        }

        /// <summary>
        /// Resolves a new path to write a save file to.
        /// </summary>
        /// <param name="type">The type of save file (Manual, QuickSave, AutoSave).</param>
        /// <param name="customName">Optional custom name for Manual saves. If omitted, uses a timestamp.</param>
        /// <returns>The full path for writing the save file.</returns>
        public string GetPath(SaveType type, string customName = null)
        {
            EnsureDirectoryExists();
            string ext = Pathways.SaveExtension;

            switch (type)
            {
                case SaveType.QuickSave:
                    return Path.Combine(FullPath, $"{ProfileId}_{Pathways.QuickSaveFileName}.{ext}");

                case SaveType.AutoSave:
                    int nextSlot = GetNextAutoSaveSlot();
                    return Path.Combine(FullPath, $"{ProfileId}_{Pathways.AutoSavePrefix}{nextSlot}.{ext}");

                case SaveType.Manual:
                default:
                    string fileName = string.IsNullOrEmpty(customName)
                        ? $"save_{GetTimestampedFileName()}"
                        : SanitizeFileName(customName);
                    return Path.Combine(FullPath, $"{ProfileId}_{fileName}.{ext}");
            }
        }

        /// <summary>
        /// Finds the full path to the most recent existing save file matching the filter.
        /// </summary>
        /// <param name="type">Optional save type filter. If null, returns the most recent save of any type.</param>
        /// <returns>The full path to the save file, or null if none exists.</returns>
        public string GetLatest(SaveType? type = null)
        {
            FileInfo[] files = GetSaves(type);
            return files.Length > 0 ? files[0].FullName : null;
        }

        /// <summary>
        /// Returns all existing save files matching the filter, ordered by date descending (newest first).
        /// </summary>
        /// <param name="type">Optional save type filter. If null, returns all saves.</param>
        /// <returns>Array of FileInfo objects.</returns>
        public FileInfo[] GetSaves(SaveType? type = null)
        {
            Refresh();
            if (cachedFiles == null || cachedFiles.Length == 0)
                return Array.Empty<FileInfo>();

            if (type == null)
                return cachedFiles;

            return type.Value switch
            {
                SaveType.QuickSave => cachedFiles.Where(f => IsQuickSave(f.Name)).ToArray(),
                SaveType.AutoSave => cachedFiles.Where(f => IsAutoSave(f.Name)).ToArray(),
                _ => cachedFiles.Where(f => IsManualSave(f.Name)).ToArray(),
            };
        }

        /// <summary>
        /// Gets the full path for a specific file in the profile directory.
        /// </summary>
        public string GetFilePath(string fileName) => Path.Combine(FullPath, fileName);

        /// <summary>
        /// Checks if a file exists in the profile directory.
        /// </summary>
        public bool FileExists(string fileName) => File.Exists(GetFilePath(fileName));

        /// <summary>
        /// Deletes a specific file within the profile directory.
        /// </summary>
        /// <returns>True if the file was deleted, false if it did not exist.</returns>
        public bool DeleteFile(string fileName)
        {
            string filePath = GetFilePath(fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Refresh();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Deletes the entire profile directory and all its files.
        /// </summary>
        /// <returns>True if the directory was deleted, false if it did not exist.</returns>
        public bool Delete()
        {
            if (Directory.Exists(FullPath))
            {
                Directory.Delete(FullPath, true);
                cachedFiles = Array.Empty<FileInfo>();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Refreshes the file list directly from the disk directory.
        /// </summary>
        public void Refresh()
        {
            if (string.IsNullOrEmpty(ProfileId) || string.IsNullOrEmpty(Pathways.StorageLocation))
            {
                cachedFiles = Array.Empty<FileInfo>();
                return;
            }

            var directory = new DirectoryInfo(FullPath);
            if (!directory.Exists)
            {
                cachedFiles = Array.Empty<FileInfo>();
                return;
            }

            cachedFiles = directory
                .GetFiles($"*.{Pathways.SaveExtension}", SearchOption.TopDirectoryOnly)
                .OrderByDescending(f => f.LastWriteTime)
                .ToArray();
        }

        /// <summary>
        /// Gets the DirectoryInfo for the profile directory, creating it if it does not exist.
        /// </summary>
        public DirectoryInfo GetDirectoryInfo()
        {
            EnsureDirectoryExists();
            return new DirectoryInfo(FullPath);
        }

        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(FullPath))
                Directory.CreateDirectory(FullPath);
        }

        private bool IsAutoSave(string fileName)
        {
            return fileName.StartsWith($"{ProfileId}_{Pathways.AutoSavePrefix}", StringComparison.OrdinalIgnoreCase)
                || fileName.StartsWith(Pathways.AutoSavePrefix, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsQuickSave(string fileName)
        {
            string ext = Pathways.SaveExtension;
            return fileName.Equals(
                    $"{ProfileId}_{Pathways.QuickSaveFileName}.{ext}",
                    StringComparison.OrdinalIgnoreCase
                ) || fileName.Equals($"{Pathways.QuickSaveFileName}.{ext}", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsManualSave(string fileName) => !IsAutoSave(fileName) && !IsQuickSave(fileName);

        private int GetNextAutoSaveSlot()
        {
            FileInfo[] autoSaves = GetSaves(SaveType.AutoSave);
            if (autoSaves.Length == 0)
                return 1;

            int autoSaveSlots = Pathways.AutoSaveSlots;
            if (autoSaves.Length < autoSaveSlots)
            {
                int[] usedSlots = autoSaves
                    .Select(f =>
                    {
                        string name = Path.GetFileNameWithoutExtension(f.Name);
                        string[] parts = name.Split('_');
                        return int.TryParse(parts.Last(), out int slot) ? slot : 0;
                    })
                    .ToArray();

                for (int i = 1; i <= autoSaveSlots; i++)
                {
                    if (!usedSlots.Contains(i))
                        return i;
                }
            }

            FileInfo oldestAutoSave = autoSaves.OrderBy(f => f.LastWriteTime).First();
            string oldestName = Path.GetFileNameWithoutExtension(oldestAutoSave.Name);
            string[] parts = oldestName.Split('_');
            return int.TryParse(parts.Last(), out int oldestSlot) ? oldestSlot : 1;
        }

        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');

            return name.Replace(' ', '_');
        }

        private static string GetTimestampedFileName() => DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

        public override string ToString() => $"SaveProfile: {ProfileId}, Files: {FileCount}, Path: {FullPath}";
    }
}
