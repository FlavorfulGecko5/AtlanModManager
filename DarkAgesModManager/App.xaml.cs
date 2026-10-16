using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace DarkAgesModManager
{
    /// <summary>
    /// Lógica de interacción para App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Expected game folder (working directory)
        /// </summary>
        public static string GameFolder = ".";

        /// <summary>
        /// Determines if the program is restoring the backups or not
        /// </summary>
        public static bool IsRestoringBackups;

        public static void RunAtlan(bool UninstallMode)
        {
            // Prompt to create the folder if it doesn't exist
            if (!Utils.CheckModsDirectory("mods"))
            {
                return;
            }

            // If we can't find AtlanModLoader.exe, warn the user
            if (!File.Exists(Path.Combine(App.GameFolder, "AtlanModLoader.exe")))
            {
                MessageBox.Show("Can't find AtlanModLoader.exe\nMake sure that the modding tools are installed.", "Missing tools", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var process = new Process())
            {
                var processStartInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    WorkingDirectory = App.GameFolder,
                    FileName = "AtlanModLoader.exe",
                    Arguments = UninstallMode ? "--uninstall" : AdvancedOptionsWindow.ModLoaderSettings.GetModLoaderArgumentsString(),
                };

                process.StartInfo = processStartInfo;
                process.Start();
            }

            if(!UninstallMode)
            {
                Environment.Exit(0);
            }
        }

        /// <summary>
        /// Fired on startup
        /// </summary>
        /// <param name="e">startup event args</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Check for game
            if (!File.Exists(Path.Combine(GameFolder, "DOOMTheDarkAges.exe"))
                && !File.Exists(Path.Combine(GameFolder, "DOOMEternalx64vk.exe")))
            {
                MessageBox.Show("Can't find valid game executable\nThis tool needs to be placed in the game folder.", "Missing game", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }

            // Check for tools
            if (!File.Exists(Path.Combine(GameFolder, "AtlanModLoader.exe")))
            {
                MessageBox.Show("Can't find AtlanModLoader.exe\nMake sure that the modding tools are installed.", "Missing tools", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }

            // Create the mods folder if it doesn't exist
            // Atlan uses a lowercase mods folder name, which matters for Linux
            if (!Directory.Exists(Path.Combine(GameFolder, "mods")))
            {
                if (MessageBox.Show("This tool needs a mods folder in your game folder to place mods, and this folder is currently missing.\n\n" +
                    "In order to proceed the folder needs to be created, do you want to create it?", "Mods folder", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                {
                    try
                    {
                        Directory.CreateDirectory(Path.Combine(GameFolder, "mods"));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error while creating the mods folder\n\n" + ex, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Environment.Exit(1);
                    }
                }
                else
                {
                    Environment.Exit(0);
                }
            }

            // Create the disabled mods folder if it doesn't exist
            if (!Directory.Exists(Path.Combine(GameFolder, "DisabledMods")))
            {
                if (MessageBox.Show("This tool needs a disabled mods folder in your game folder to place disabled mods, and this folder is currently missing.\n\n" +
                    "In order to proceed the folder needs to be created, do you want to create it?", "Disabled mods folder", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                {
                    try
                    {
                        Directory.CreateDirectory(Path.Combine(GameFolder, "DisabledMods"));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error while creating the disabled mods folder\n\n" + ex, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Environment.Exit(1);
                        return;
                    }
                }
                else
                {
                    Environment.Exit(0);
                    return;
                }
            }

            // Load settings.
            AdvancedOptionsWindow.LoadModLoaderSettingsFile();
        }
    }
}
