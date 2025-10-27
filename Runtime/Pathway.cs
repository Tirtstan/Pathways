using System;
using System.IO;
using System.Linq;

namespace Pathways
{
    /// <summary>
    /// Manages a directory (a 'pathway') and file locations without handling actual data persistence.
    /// </summary>
    public class Pathway
    {
        private string pathwayId;

        /// <summary>
        /// Unique identifier for the pathway using a directory's name.
        /// </summary>
        public string PathwayId
        {
            get => pathwayId;
            set
            {
                pathwayId = value;
                Refresh();
            }
        }
        public FileInfo[] Files { get; private set; }

        /// <summary>
        /// Full path to the pathway directory.
        /// </summary>
        public string FullPath => Path.Combine(PathwaysGlobalConfigs.StorageLocation, PathwayId);
        public int FileCount => Files?.Length ?? 0;

        /// <summary>
        /// Most recent file in the pathway based on last write time.
        /// </summary>
        public FileInfo RecentFile =>
            FileCount > 0 ? Files.OrderByDescending(f => f.LastWriteTime).FirstOrDefault() : null;

        public Pathway(string pathwayId)
        {
            PathwayId = pathwayId;
            Refresh();
        }

        /// <summary>
        /// Gets the full path for a manual save file.
        /// This method triggers directory creation if it doesn't exist.
        /// </summary>
        /// <param name="fileName">Optional filename, defaults to timestamp-based name.</param>
        /// <returns>Full path for the save file.</returns>
        /// <remarks>
        /// If no filename is provided, a default name based on the current timestamp will be used, see <see cref="PathwaysGlobalConfigs"/>.
        /// </remarks>
        public string GetSavePath(string fileName = null)
        {
            EnsureDirectoryExists();

            if (string.IsNullOrEmpty(fileName))
                fileName = GetDefaultFileName();

            return Path.Combine(FullPath, fileName);
        }

        /// <summary>
        /// Gets the full path for the next auto-save file, cycling through available slots.
        /// This method triggers directory creation if it doesn't exist.
        /// </summary>
        /// <returns>Full path for the auto-save file.</returns>
        public string GetAutoSavePath()
        {
            EnsureDirectoryExists();

            int nextSlot = GetNextAutoSaveSlot();
            string fileName = GetAutoSaveFileName(nextSlot);
            return Path.Combine(FullPath, fileName);
        }

        /// <summary>
        /// Gets the full path for a specific file in the pathway directory.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>Full path to the file.</returns>
        public string GetFilePath(string fileName) => Path.Combine(FullPath, fileName);

        /// <summary>
        /// Checks if a save file exists.
        /// </summary>
        /// <param name="fileName">Name of the file to check.</param>
        /// <returns>True if the file exists.</returns>
        public bool FileExists(string fileName) => File.Exists(GetFilePath(fileName));

        /// <summary>
        /// Gets all manual save files ordered by last write time (newest first).
        /// </summary>
        public FileInfo[] GetManualSaves()
        {
            return Files
                    ?.Where(f => !f.Name.StartsWith(PathwaysGlobalConfigs.AutoSavePrefix))
                    .OrderByDescending(f => f.LastWriteTime)
                    .ToArray() ?? new FileInfo[0];
        }

        /// <summary>
        /// Gets all auto-save files ordered by last write time (newest first).
        /// </summary>
        public FileInfo[] GetAutoSaves()
        {
            return Files
                    ?.Where(f => f.Name.StartsWith(PathwaysGlobalConfigs.AutoSavePrefix))
                    .OrderByDescending(f => f.LastWriteTime)
                    .ToArray() ?? new FileInfo[0];
        }

        /// <summary>
        /// Gets the path for the most recent save in this pathway (includes auto saves and manual saves).
        /// </summary>
        /// <returns>Path to the most recent save file, or null if none exists.</returns>
        public string GetRecentSavePath() => RecentFile?.FullName;

        /// <summary>
        /// Gets the path to the most recent manual save.
        /// </summary>
        /// <returns>Path to the most recent manual save, or null if none exists.</returns>
        public string GetRecentManualSavePath()
        {
            FileInfo recentFile = GetManualSaves().FirstOrDefault();
            return recentFile != null ? GetFilePath(recentFile.Name) : null;
        }

        /// <summary>
        /// Gets the path to the most recent auto-save.
        /// </summary>
        /// <returns>Path to the most recent auto-save, or null if none exists.</returns>
        public string GetRecentAutoSavePath()
        {
            FileInfo recentFile = GetAutoSaves().FirstOrDefault();
            return recentFile != null ? GetFilePath(recentFile.Name) : null;
        }

        /// <summary>
        /// Deletes a specific file within the pathway directory.
        /// </summary>
        /// <param name="fileName">The name of the file to delete.</param>
        /// <returns>True if file was deleted, false if it didn't exist.</returns>
        public bool DeleteFile(string fileName)
        {
            string filePath = Path.Combine(FullPath, fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Refresh();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Deletes the entire pathway directory and all its files.
        /// </summary>
        /// <returns>True if the pathway directory was deleted, false if it didn't exist.</returns>
        public bool Delete()
        {
            if (Directory.Exists(FullPath))
            {
                Directory.Delete(FullPath, true);
                Files = new FileInfo[0];

                return true;
            }

            return false;
        }

        /// <summary>
        /// Refreshes the files list from this pathways directory.
        /// </summary>
        public void Refresh() => Files = GetFiles();

        /// <summary>
        /// Gets the <see cref="DirectoryInfo"/> for the pathway directory. Triggers directory creation if it doesn't exist.
        /// </summary>
        /// <returns>DirectoryInfo for the pathway directory.</returns>
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

        private int GetNextAutoSaveSlot()
        {
            FileInfo[] autoSaves = GetAutoSaves();
            if (autoSaves.Length == 0)
                return 1;

            int autoSaveSlots = PathwaysGlobalConfigs.AutoSaveSlots;

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

            // cycle through existing slots, use the oldest one
            FileInfo oldestAutoSave = autoSaves.OrderBy(f => f.LastWriteTime).First();
            string oldestName = Path.GetFileNameWithoutExtension(oldestAutoSave.Name);
            string[] parts = oldestName.Split('_');
            return int.TryParse(parts.Last(), out int oldestSlot) ? oldestSlot : 1;
        }

        private FileInfo[] GetFiles()
        {
            if (string.IsNullOrEmpty(PathwayId))
                return new FileInfo[0];

            var directory = new DirectoryInfo(FullPath);
            if (!directory.Exists)
                return new FileInfo[0];

            return directory
                .GetFiles($"*.{PathwaysGlobalConfigs.SaveExtension}", SearchOption.TopDirectoryOnly)
                .OrderByDescending(f => f.LastWriteTime)
                .ToArray();
        }

        private static string GetDefaultFileName() =>
            $"save_{GetTimestampedFileName()}.{PathwaysGlobalConfigs.SaveExtension}";

        private static string GetAutoSaveFileName(int slot) =>
            $"{PathwaysGlobalConfigs.AutoSavePrefix}{slot}.{PathwaysGlobalConfigs.SaveExtension}";

        private static string GetTimestampedFileName() => DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

        public override string ToString() => $"Pathway: {PathwayId}, Files: {FileCount}, Full Path: {FullPath}";
    }
}
