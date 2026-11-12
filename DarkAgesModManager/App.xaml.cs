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

        public static readonly string DirMods = Path.Combine(GameFolder, "mods");

        public static readonly string DirDisabled = Path.Combine(GameFolder, "DisabledMods");

        public const string NameCoop = "Atlan_Coop_Mode.zip";

        public const string NameInvasion = "Atlan_Invasion_Mode.zip";

        public const string NameSVS = "Atlan_Slayer_vs_Slayer.zip";


        /// <summary>
        /// Determines if the program is restoring the backups or not
        /// </summary>
        public static bool IsRestoringBackups;

        public static void RunAtlan(bool UninstallMode)
        {
            // Prompt to create the folder if it doesn't exist
            if (!Utils.CheckMods())
            {
                return;
            }

            // If we can't find AtlanModLoader.exe, warn the user
            if (!File.Exists(Path.Combine(App.GameFolder, "AtlanModLoader.exe")))
            {
                MessageBox.Show("Can't find AtlanModLoader.exe\nMake sure that the modding tools are installed.", "Missing tools", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Safeguard: Ensure the player does not have multiple downloaded multiplayer mods enabled.
            if(!UninstallMode)
            {
                string chosenMod = "";
                int numMultiplayerMods = 0;

                if (File.Exists(Path.Combine(DirMods, NameCoop)))
                {
                    chosenMod = NameCoop;
                    numMultiplayerMods++;
                }

                if (File.Exists(Path.Combine(DirMods, NameInvasion)))
                {
                    chosenMod = NameInvasion;
                    numMultiplayerMods++;
                }

                if (File.Exists(Path.Combine(DirMods, NameSVS)))
                {
                    chosenMod = NameSVS;
                    numMultiplayerMods++;
                }

                if (numMultiplayerMods > 1)
                {
                    const string prompt = "Multiplayer gamemode mods conflict with each other and cannot be installed simultaneously!"
                        + "\nPlease disable all but one of them!";
                    MessageBox.Show(prompt, "Multiplayer Mod Conflict", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if(chosenMod.Length > 0)
                {
                    if(!MultiplayerBackend.CheckForUpdates(chosenMod))
                    {
                        return;
                    }
                }
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

            if(!Utils.CheckBoth_Startup())
            {
                Environment.Exit(1);
            }

            // Load settings.
            AdvancedOptionsWindow.LoadModLoaderSettingsFile();
        }
    }
}
