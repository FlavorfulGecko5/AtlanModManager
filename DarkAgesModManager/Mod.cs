using System.IO;
using System.IO.Compression;

namespace DarkAgesModManager
{
    /// <summary>
    /// Mod class
    /// </summary>
    public class Mod
    {
        /// <summary>
        /// Mod file name
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Full path to the mod file
        /// </summary>
        public string FullPath { get; set; }

        /// <summary>
        /// Whether or not the mod is enabled
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Whether or not the mod is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Mod constructor
        /// </summary>
        /// <param name="fileName">mod file name</param>
        /// <param name="isEnabled">is the mod enabled?</param>
        public Mod(string fullPath, string fileName, bool isEnabled)
        {
            FullPath = fullPath;
            FileName = fileName;
            IsEnabled = isEnabled;
        }
    }
}
