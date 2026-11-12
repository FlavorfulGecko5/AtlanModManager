using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Net.Http;
using DarkAgesModManager.DeclParser;
using System.Runtime.CompilerServices;
using System.Threading;
//using System.Windows.Shapes;

namespace DarkAgesModManager
{
    public struct MultiplayerHistoryEntry
    {
        public long filetime;
        public long updateChecktime;

        public MultiplayerHistoryEntry(long p_filetime = 0, long p_updateCheckTime = 0)
        {
            filetime = p_filetime;
            updateChecktime = p_updateCheckTime;
        }
    }

    public class MultiplayerDownloadHistory
    {
        const string filename = "ModLoaderDownloads.txt";

        Dictionary<string, MultiplayerHistoryEntry> history = new Dictionary<string, MultiplayerHistoryEntry>();

        public MultiplayerDownloadHistory()
        {
            string historyfile = Path.Combine(App.GameFolder, filename);
            if (!File.Exists(historyfile))
                return;

            foreach(string line in File.ReadAllLines(historyfile))
            {
                string[] tokens = line.Split('=');

                if (tokens.Length != 3)
                    continue;

                MultiplayerHistoryEntry entry;

                if (!long.TryParse(tokens[1], out entry.filetime))
                    continue;
                if (!long.TryParse(tokens[2], out entry.updateChecktime))
                    continue;

                history[tokens[0]] = entry;
            }
        }

        public bool TimestampMatches(string key, long serverTimestamp)
        {
            if(history.ContainsKey(key))
            {
                return history[key].filetime == serverTimestamp;
            }

            return false;
        }

        public bool ShouldCheckForUpdates(string key)
        {
            if(history.ContainsKey(key))
            {
                long tickrange = DateTime.UtcNow.Ticks - history[key].updateChecktime;
                double daysSinceLastDownload = tickrange / TimeSpan.TicksPerDay;

                return daysSinceLastDownload >= 3.0;
            }
            return true;
        }


        public void UpdateAndSave(string newKey, MultiplayerHistoryEntry newEntry)
        {
            history[newKey] = newEntry;
            string historyfile = Path.Combine(App.GameFolder, filename);

            using(FileStream fstream = new FileStream(historyfile, FileMode.Create, FileAccess.Write))
            {
                using(StreamWriter sw = new StreamWriter(fstream))
                {
                    foreach (KeyValuePair<string, MultiplayerHistoryEntry> pair in history)
                    {
                        sw.Write(pair.Key);
                        sw.Write('=');
                        sw.Write(pair.Value.filetime);
                        sw.Write('=');
                        sw.Write(pair.Value.updateChecktime);
                        sw.Write('\n');
                    }
                }
            }
        }
    }

    public class MultiplayerBackend
    {
        private const string url_coop = "https://github.com/Velserus/doom-eternal-modded-multiplayer/releases/download/co-op/Co-operative_Mode.zip";
        private const string url_invasion = "https://github.com/Velserus/doom-eternal-modded-multiplayer/releases/download/invasion/Invasion_Mode.zip";
        private const string url_svs = "https://github.com/Velserus/doom-eternal-modded-multiplayer/releases/download/slayer-vs-slayer/Slayer_vs_Slayer.zip";


        public HttpClient httpclient;
        public MultiplayerDownloadHistory history;

        public string historykey;   // Mod name NOT INCLUDING it's containg folder.
        public string url;
        public string destination;  // Containing folder + mod name

        public MultiplayerBackend()
        {
            httpclient = new HttpClient();
            httpclient.Timeout = new System.TimeSpan(0, 0, 10);
            history = new MultiplayerDownloadHistory();
        }

        public void SetMod(string p_modname)
        {
            historykey = p_modname;

            switch (p_modname)
            {
                case App.NameCoop: url = url_coop; break;
                case App.NameInvasion: url = url_invasion; break;
                case App.NameSVS: url = url_svs; break;

                default: throw new ArgumentException("MultiplayerBackend::SetMod Cannot resolve mod zip to URL");
            }

            // TODO EDGE CASE: Mod in both Mods and Disabled dir?
            destination = Path.Combine(App.DirDisabled, p_modname);
            if (!File.Exists(destination))
            {
                destination = Path.Combine(App.DirMods, p_modname);
            }
        }

        // Returns true if we can proceed with launching the game
        public static bool CheckForUpdates(string modname)
        {
            MultiplayerBackend backend = new MultiplayerBackend();
            backend.SetMod(modname);

            if (!backend.history.ShouldCheckForUpdates(backend.historykey))
                return true;

            // Async so we don't block the main thread
            // todo how to move this out of the way
            //Task.Run(() => { MessageBox.Show("Checking for mod updates..."); });

            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Head, backend.url);
                Task<HttpResponseMessage> responseTask = backend.httpclient.SendAsync(request);
                responseTask.Wait(5000);

                if (!responseTask.IsCompleted)
                {
                    throw new Exception("Update checker timed out");
                }

                long servertimestamp = responseTask.Result.Content.Headers.LastModified.Value.UtcTicks;

                if (backend.history.TimestampMatches(backend.historykey, servertimestamp))
                {
                    backend.history.UpdateAndSave(backend.historykey, new MultiplayerHistoryEntry(servertimestamp, DateTime.UtcNow.Ticks));
                    return true;
                }

                string prompt = $"An update is available for {modname}\n\nDownload now?";
                if (MessageBoxResult.Yes != MessageBox.Show(prompt, "Multiplayer Mod Updated", MessageBoxButton.YesNo, MessageBoxImage.Warning))
                {
                    return true;
                }

                MultiplayerWindow window = new MultiplayerWindow(backend);
                window.Owner = Application.Current.MainWindow;
                window.Show();
                window.Focus();
                window.LaunchDownloader();

                return false;
            }
            catch (Exception e)
            {
                string prompt = $"Failed to check for mod updates.\n"
                    + $"Error: {e.Message} {(e.InnerException == null ? "" : e.InnerException.Message)}"
                    + "\n\nLaunch the game anyways?";

                return MessageBoxResult.Yes == MessageBox.Show(prompt, "Update Check Failure", MessageBoxButton.YesNo, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// Interaction logic for MultiplayerWindow.xaml
    /// </summary>
    public partial class MultiplayerWindow : Window
    {

        MultiplayerBackend backend;


        public MultiplayerWindow( MultiplayerBackend p_backend)
        {
            InitializeComponent();
            backend = p_backend;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            (Owner as MainWindow).MainGrid.IsEnabled = false;
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            (Owner as MainWindow).MainGrid.IsEnabled = true;
            Owner = null;
        }

        public void LaunchDownloader()
        {
            MultiplayerDownloadBox window = new MultiplayerDownloadBox();
            window.Owner = this;
            window.backend = this.backend;
            window.Show();
            window.Focus();
        }

        private void DownloadMod( string modName)
        {
            if(!Utils.CheckBoth())
                return;

            backend.SetMod(modName);
            LaunchDownloader();
        }

        private void Button_DownloadCoop_Click(object sender, RoutedEventArgs e)
        {
            DownloadMod(App.NameCoop);
        }

        private void Button_DownloadSVS_Click(object sender, RoutedEventArgs e)
        {
            DownloadMod(App.NameSVS);
        }

        private void Button_DownloadInvasion_Click(object sender, RoutedEventArgs e)
        {
            DownloadMod(App.NameInvasion);
        }

        private void AboutMod(string key, string title)
        {
            MultiplayerAboutWindow aboutWindow = new MultiplayerAboutWindow();
            aboutWindow.Owner = this;
            aboutWindow.Title = title;
            aboutWindow.SetModDescription(key);
            aboutWindow.Show();
            aboutWindow.Focus();
        }

        private void Button_AboutCoop_Click(object sender, RoutedEventArgs e)
        {
            AboutMod("coop", "About Coop Campaign");
        }

        private void Button_AboutInvasion_Click(object sender, RoutedEventArgs e)
        {
            AboutMod("invasion", "About Invasion Mode");
        }

        private void Button_AboutSvs_Click(object sender, RoutedEventArgs e)
        {
            AboutMod("svs", "About Slayer vs Slayer");
        }
    }
}
