using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DarkAgesModManager
{
    /// <summary>
    /// Lógica de interacción para AdvancedOptionsWindow.xaml
    /// </summary>
    public partial class AdvancedOptionsWindow : Window
    {
        /// <summary>
        /// Mod loader settings
        /// </summary>
        public static ModLoaderSettings ModLoaderSettings { get; set; } = new ModLoaderSettings();

        /// <summary>
        /// Original mod loader settings object, to track setting changes
        /// </summary>
        private static ModLoaderSettings OriginalModLoaderSettings = new ModLoaderSettings();

        /// <summary>
        /// Advanced Options Window constructor
        /// </summary>
        public AdvancedOptionsWindow()
        {
            InitializeComponent();

            // Set the data context for the mod loader settings controls to this window
            // so that they can be bound to the injector settings object properties
            VerboseLoggingCheckBox.DataContext = this;
            AutoLaunchGameCheckBox.DataContext = this;
            IgnorePatcherErrors.DataContext = this;

            // Load the current mod loader settings file
            LoadModLoaderSettingsFile();

            // Bind the events now
            VerboseLoggingCheckBox.Checked += InjectorSettingsControlChanged;
            VerboseLoggingCheckBox.Unchecked += InjectorSettingsControlChanged;
            AutoLaunchGameCheckBox.Checked += InjectorSettingsControlChanged;
            AutoLaunchGameCheckBox.Unchecked += InjectorSettingsControlChanged;
            IgnorePatcherErrors.Checked += InjectorSettingsControlChanged;
            IgnorePatcherErrors.Unchecked += InjectorSettingsControlChanged;

            Loaded += AdvancedOptionsWindow_Loaded;
            Closing += AdvancedOptionsWindow_Closing;
        }

        /// <summary>
        /// Loads the mod loader settings file
        /// </summary>
        public static void LoadModLoaderSettingsFile()
        {
            var settingsFilePath = Path.Combine(App.GameFolder, "ModLoaderSettings.txt");

            if (!File.Exists(settingsFilePath))
            {
                return;
            }

            // Load the settings from the file now
            var injectorSettings = File.ReadAllLines(settingsFilePath).Where(line => line.StartsWith(":"));

            // Currently, all the settings are booleans or strings
            foreach (var setting in injectorSettings)
            {
                var settingData = setting.Split('=');

                if (settingData == null || settingData.Length == 0)
                {
                    continue;
                }

                bool settingValue = false;

                if (settingData.Length > 1)
                {
                    if (settingData[0] != ":GAME_PARAMETERS")
                    {
                        int value = 0;
                        int.TryParse(settingData[1], out value);

                        settingValue = value == 0 ? false : true;
                    }
                }

                switch (settingData[0])
                {
                    case ":VERBOSE":
                        ModLoaderSettings.EnableVerboseLogging = settingValue;
                        break;
                    case ":NOLAUNCH":
                        ModLoaderSettings.AutoLaunchGame = !settingValue;
                        break;
                    case ":FORCELOAD":
                        ModLoaderSettings.IgnorePatcherErrors = settingValue;
                        break;
                    default:
                        break;
                }
            }

            OriginalModLoaderSettings = ModLoaderSettings.Clone();
        }

        /// <summary>
        /// Fired when the window is loaded
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">event args</param>
        private void AdvancedOptionsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Disable the parent window main grid
            (Owner as MainWindow).MainGrid.IsEnabled = false;
        }

        /// <summary>
        /// Fired when attempting to close the window
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">event args</param>
        private void AdvancedOptionsWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (App.IsRestoringBackups)
            {
                MessageBox.Show("Please wait until the backup restore process is finished.", "Restoring backups", MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Cancel = true;
                return;
            }

            (Owner as MainWindow).MainGrid.IsEnabled = true;
            Owner = null;
        }

        /// <summary>
        /// Open enabled mods folder button click event handler
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">event args</param>
        private void OpenEnabledModsButton_Click(object sender, RoutedEventArgs e)
        {
            // Prompt to create the folder if it doesn't exist
            if (!CheckModsDirectory("Mods"))
            {
                return;
            }

            Process.Start(Path.Combine(App.GameFolder, "Mods"));
        }

        /// <summary>
        /// Open disabled mods folder button click event handler
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">event args</param>
        private void OpenDisabledModsButton_Click(object sender, RoutedEventArgs e)
        {
            // Prompt to create the folder if it doesn't exist
            if (!CheckModsDirectory("DisabledMods"))
            {
                return;
            }

            Process.Start(Path.Combine(App.GameFolder, "DisabledMods"));
        }

        /// <summary>
        /// Open game folder button click event handler
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">event args</param>
        private void OpenGameFolderButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Path.Combine(App.GameFolder));
        }

        /// <summary>
        /// Restore backups button click event handler
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">event args</param>
        private void RestoreBackupsButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show($"This will restore your game to vanilla state by restoring the unmodded backed up game files.\n\nThis process might take a while depending on the speed of your disk, so please be patient.\n\nAre you sure you want to continue?", "Restore backups", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.No)
            {
                return;
            }

            // Disable the UI
            MainGrid.IsEnabled = false;
            RestoreBackupsButton.Content = "Restoring backups...";
            App.IsRestoringBackups = true;

            // Restore the backups asynchronously
            Task.Run(() =>
            {
                int restoredCount = 0;

                // Restore packagemapspec.json
                var packageMapSpecJsonBackupFilePath = Path.Combine(App.GameFolder, "base", "packagemapspec.json.backup");

                try
                {
                    if (File.Exists(packageMapSpecJsonBackupFilePath))
                    {
                        File.Copy(packageMapSpecJsonBackupFilePath, packageMapSpecJsonBackupFilePath.Replace(".backup", ""), true);
                        restoredCount++;
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("Error while restoring backup file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                // Restore backups in the "base" directory
                foreach (var file in Directory.EnumerateFiles(Path.Combine(App.GameFolder, "base"), "*.resources.backup", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        File.Copy(file, file.Replace(".backup", ""), true);
                        restoredCount++;
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Error while restoring backup file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        continue;
                    }
                }

                // Restore backups in the "base/game" directory (all directories)
                foreach (var file in Directory.EnumerateFiles(Path.Combine(App.GameFolder, "base", "game"), "*.resources.backup", SearchOption.AllDirectories))
                {
                    try
                    {
                        File.Copy(file, file.Replace(".backup", ""), true);
                        restoredCount++;
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Error while restoring backup file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        continue;
                    }
                }

                // Restore backups in the "base/sound/soundbanks/pc" directory
                foreach (var file in Directory.EnumerateFiles(Path.Combine(App.GameFolder, "base", "sound", "soundbanks", "pc"), "*.snd.backup", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        File.Copy(file, file.Replace(".backup", ""), true);
                        restoredCount++;
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Error while restoring backup file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        continue;
                    }
                }

                // Re-enable the UI
                Dispatcher.Invoke(() =>
                {
                    MainGrid.IsEnabled = true;
                    RestoreBackupsButton.Content = "Restore backups";
                    MessageBox.Show($"{restoredCount} backups were restored.", "Restore backups", MessageBoxButton.OK, MessageBoxImage.Information);
                });

                App.IsRestoringBackups = false;
            });
        }


        private void UninstallModsButton_Click(object sender, RoutedEventArgs e)
        {
            const string prompt = "This will permanently uninstall all idStudio level mods.\n\nYou will need to manually redownload and reinstall these mods to play with them again.\n\nAre you sure you want to continue?";

            if(MessageBoxResult.Yes != MessageBox.Show(prompt, "Uninstall Level Mods", MessageBoxButton.YesNo, MessageBoxImage.Warning)) 
            {
                return;
            }

            App.RunAtlan(true);
        }

        /// <summary>
        /// Reset backups button click event handler
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">event args</param>
        private void ResetBackupsButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show($"This will delete your backed up game files. Normally this should only be done if you accidentally backed up modded game files, or after a game update.\n\nThe next time mods are injected the backups will be re-created using the game files installed at that moment, so make sure to verify your game files before or after doing this.\n\nAre you sure you want to continue?", "Reset backups", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
            {
                return;
            }

            int deletedCount = 0;

            // Delete mod loader cache.
            var modLoaderCacheFilePath = Path.Combine(App.GameFolder, "modloader_cache.bin");

            try
            {
                if (File.Exists(modLoaderCacheFilePath))
                {
                    File.Delete(modLoaderCacheFilePath);
                    deletedCount++;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Error while deleting backup file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Delete packagemapspec.json.backup
            var packageMapSpecJsonBackupFilePath = Path.Combine(App.GameFolder, "base", "packagemapspec.json.backup");

            try
            {
                if (File.Exists(packageMapSpecJsonBackupFilePath))
                {
                    File.Delete(packageMapSpecJsonBackupFilePath);
                    deletedCount++;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Error while deleting backup file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Delete backups in the "base" directory
            foreach (var file in Directory.EnumerateFiles(Path.Combine(App.GameFolder, "base"), "*.resources.backup", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    File.Delete(file);
                    deletedCount++;
                }
                catch (Exception)
                {
                    MessageBox.Show("Error while deleting backup file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    continue;
                }
            }

            // Delete backups in the "base/game" directory (all directories)
            foreach (var file in Directory.EnumerateFiles(Path.Combine(App.GameFolder, "base", "game"), "*.resources.backup", SearchOption.AllDirectories))
            {
                try
                {
                    File.Delete(file);
                    deletedCount++;
                }
                catch (Exception)
                {
                    MessageBox.Show("Error while deleting backup file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    continue;
                }
            }

            // Delete backups in the "base/sound/soundbanks/pc" directory
            foreach (var file in Directory.EnumerateFiles(Path.Combine(App.GameFolder, "base", "sound", "soundbanks", "pc"), "*.snd.backup", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    File.Delete(file);
                    deletedCount++;
                }
                catch (Exception)
                {
                    MessageBox.Show("Error while deleting backup file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    continue;
                }
            }

            MessageBox.Show($"{deletedCount} backups were deleted.", "Reset backups", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Checks whether or not the specified mods folder exists
        /// If it doesn't, it will prompt the user to create it
        /// </summary>
        /// <param name="modsFolder">mods folder</param>
        /// <returns>true if the problem was resolved, false otherwise</returns>
        private bool CheckModsDirectory(string modsFolder)
        {
            // Prompt to create the folder if it doesn't exist
            if (!Directory.Exists(Path.Combine(App.GameFolder, modsFolder)))
            {
                if (MessageBox.Show($"The {modsFolder} folder has been deleted. In order for this application to function correctly, the folder must exist. Please restore the folder.\n\n" +
                    "Do you want to create it instead?", "Missing folder", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
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

        /// <summary>
        /// Fired when an injector settings control has changed
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">event args</param>
        private void InjectorSettingsControlChanged(object sender, RoutedEventArgs e)
        {
            SaveInjectorSettingsButton.IsEnabled = !ModLoaderSettings.IsEqualTo(OriginalModLoaderSettings);
        }

        /// <summary>
        /// Fired when the "Save mod injector settings" button is clicked
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">event args</param>
        private void SaveInjectorSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsFilePath = Path.Combine(App.GameFolder, "ModLoaderSettings.txt");
            var settingsStringBuilder = new StringBuilder();

            settingsStringBuilder.AppendLine($":VERBOSE={(ModLoaderSettings.EnableVerboseLogging ? "1" : "0")}");
            settingsStringBuilder.AppendLine($":NOLAUNCH={(ModLoaderSettings.AutoLaunchGame ? "0" : "1")}");
            settingsStringBuilder.AppendLine($":FORCELOAD={(ModLoaderSettings.IgnorePatcherErrors ? "1" : "0")}");

            try
            {
                // Write the settings file
                File.WriteAllText(settingsFilePath, settingsStringBuilder.ToString());
                SaveInjectorSettingsButton.IsEnabled = false;
                OriginalModLoaderSettings = ModLoaderSettings.Clone();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error while saving the mod loader settings\n\n" + ex, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        /// <summary>
        /// Fired when the "Copy darkagesmod.txt template" button is clicked
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">event args</param>
        private void CopyTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText("modinfo = {\r\n\tname = \"\";\r\n\tauthor = \"\";\r\n\tdescription = \"\";\r\n\tversion = \"1.0\";\r\n\tloadPriority = 0;\r\n\trequiredVersion = 1;\r\n}");
            MessageBox.Show("darkagesmod.txt template copied to your clipboard.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
