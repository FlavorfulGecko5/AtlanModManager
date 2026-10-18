using System;
using System.IO;
using System.Windows;

namespace DarkAgesModManager
{
    /// <summary>
    /// Utils class
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// Checks whether or not the specified mods folder exists
        /// If it doesn't, it will prompt the user to create it
        /// </summary>
        /// <param name="modsFolder">mods folder</param>
        /// <returns>true if the problem was resolved, false otherwise</returns>
        private static bool CheckModsDirectory(string modsFolder, bool startup = false)
        {
            // Prompt to create the folder if it doesn't exist
            if (!Directory.Exists(Path.Combine(App.GameFolder, modsFolder)))
            {
                string prompt;
                if(startup)
                {
                    prompt = $"The {modsFolder} folder does not exist in your game folder.\n\n"
                        + "In order to proceed the folder needs to be created, do you want to create it?";
                }
                else
                {
                    prompt = $"The {modsFolder} folder has been deleted. In order for this application to function correctly, the folder must exist. Please restore the folder.\n\n"
                        + "Do you want to create it instead?";
                }

                if (MessageBoxResult.Yes == MessageBox.Show(prompt, "Missing Folder", MessageBoxButton.YesNo, startup ? MessageBoxImage.Information : MessageBoxImage.Error))
                {
                    try
                    {
                        Directory.CreateDirectory(Path.Combine(App.GameFolder, modsFolder));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error while creating {modsFolder} folder\n\n" + ex, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        public static bool CheckBoth_Startup()
        {
            return CheckModsDirectory("mods", true) && CheckModsDirectory("DisabledMods", true);
        }

        public static bool CheckBoth()
        {
            return CheckModsDirectory("mods") && CheckModsDirectory("DisabledMods");
        }

        public static bool CheckDisabled()
        {
            return CheckModsDirectory("DisabledMods");
        }

        public static bool CheckMods()
        {
            return CheckModsDirectory("mods");
        }

    }
}
