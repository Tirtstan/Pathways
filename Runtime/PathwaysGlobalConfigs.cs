using System.IO;
using UnityEngine;

namespace Pathways
{
    public static class PathwaysGlobalConfigs
    {
        /// <summary>
        /// The path to save and create pathways to.
        /// </summary>
        /// <remarks>
        /// Defaults to <see cref="Application.persistentDataPath"/>, recommend to append this path with <see cref="Path.Combine"/>.
        /// </remarks>
        public static string StorageLocation { get; set; } = Application.persistentDataPath;

        /// <summary>
        /// The file extension to use for saved files.
        /// </summary>
        /// <remarks>
        /// Defaults to "sav".
        /// </remarks>
        public static string SaveExtension { get; set; } = "sav";

        /// <summary>
        /// Prefix for auto-saved files.
        /// </summary>
        /// <remarks>
        /// Defaults to "auto_save_".
        /// </remarks>
        public static string AutoSavePrefix { get; set; } = "auto_save_";
    }
}
