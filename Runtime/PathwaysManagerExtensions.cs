using System.IO;
using System.Linq;

namespace Pathways
{
    public static class PathwaysManagerExtensions
    {
        /// <summary>
        /// Gets all save files in the current pathway.
        /// </summary>
        /// <returns>Array of FileInfo objects, or empty if no pathway is current.</returns>
        public static FileInfo[] GetAllSaveFiles(this PathwaysManager manager) =>
            manager.CurrentPathway?.Files ?? new FileInfo[0];

        /// <summary>
        /// Gets all manual save files in the current pathway.
        /// </summary>
        /// <returns>Array of FileInfo objects, or empty if no pathway is current.</returns>
        public static FileInfo[] GetManualSaveFiles(this PathwaysManager manager) =>
            manager.CurrentPathway?.GetManualSaves() ?? new FileInfo[0];

        /// <summary>
        /// Gets all auto-save files in the current pathway.
        /// </summary>
        /// <returns>Array of FileInfo objects, or empty if no pathway is current.</returns>
        public static FileInfo[] GetAutoSaveFiles(this PathwaysManager manager) =>
            manager.CurrentPathway?.GetAutoSaves() ?? new FileInfo[0];

        /// <summary>
        /// Gets the most recent save file info from the current pathway based on last write time.
        /// </summary>
        /// <returns>FileInfo of the most recent save file, or null if no pathway is current.</returns>
        public static FileInfo GetRecentSaveFile(this PathwaysManager manager) =>
            manager.CurrentPathway?.RecentFile ?? null;

        /// <summary>
        /// Gets the most recent manual save file info.
        /// </summary>
        /// <returns>FileInfo of most recent manual save, or null if none exists.</returns>
        public static FileInfo GetRecentManualSaveFile(this PathwaysManager manager) =>
            manager.CurrentPathway?.GetManualSaves().FirstOrDefault();

        /// <summary>
        /// Gets the most recent auto-save file info.
        /// </summary>
        /// <returns>FileInfo of most recent auto-save, or null if none exists.</returns>
        public static FileInfo GetRecentAutoSaveFile(this PathwaysManager manager) =>
            manager.CurrentPathway?.GetAutoSaves().FirstOrDefault();

        /// <summary>
        /// Checks if a file exists in the current pathway.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>Whether the file within the current pathway exists.</returns>
        public static bool FileExists(this PathwaysManager manager, string fileName) =>
            manager.CurrentPathway?.FileExists(fileName) ?? false;
    }
}
