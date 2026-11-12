using System.Collections.Generic;
using System.Linq;

namespace DarkAgesModManager
{
    /// <summary>
    /// Mod injector settings class
    /// </summary>
    public class ModLoaderSettings
    {
        public bool AutoLaunchGame { get; set; } = true;
        public bool EnableVerboseLogging { get; set; } = false;
        public bool IgnorePatcherErrors { get; set; } = false;
        public bool CheckForModUpdates { get; set;} = true;

        public string GetModLoaderArgumentsString()
        {
            var argList = new List<string>();
            
            if (EnableVerboseLogging)
            {
                argList.Add("--verbose");
            }

            if (!AutoLaunchGame)
            {
                argList.Add("--nolaunch");
            }

            if (IgnorePatcherErrors)
            {
                argList.Add("--forceload");
            }

            return string.Join(" ", argList);
        }

        /// <summary>
        /// Returns true if the two mod loader settings objects are equal, false otherwise
        /// </summary>
        /// <param name="injectorSettings">mod loader settings object to compare against</param>
        /// <returns>true if the two mod loader settings objects are equal, false otherwise</returns>
        public bool IsEqualTo(ModLoaderSettings injectorSettings)
        {
            return EnableVerboseLogging == injectorSettings.EnableVerboseLogging
                && AutoLaunchGame == injectorSettings.AutoLaunchGame
                && IgnorePatcherErrors == injectorSettings.IgnorePatcherErrors
                && CheckForModUpdates == injectorSettings.CheckForModUpdates;
        }

        /// <summary>
        /// Clones the mod loader settings object
        /// </summary>
        /// <returns>A clone of the mod loader settings object</returns>
        public ModLoaderSettings Clone()
        {
            var clone = new ModLoaderSettings()
            {
                EnableVerboseLogging = EnableVerboseLogging,
                AutoLaunchGame = AutoLaunchGame,
                IgnorePatcherErrors = IgnorePatcherErrors,
                CheckForModUpdates = CheckForModUpdates
            };

            return clone;
        }
    }
}
