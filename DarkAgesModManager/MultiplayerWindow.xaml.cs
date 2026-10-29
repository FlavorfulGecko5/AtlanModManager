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
    public class MultiplayerDownloadHistory
    {
        const string filename = "ModLoaderDownloads.txt";

        Dictionary<string, long> history = new Dictionary<string, long>();

        public MultiplayerDownloadHistory()
        {
            string historyfile = Path.Combine(App.GameFolder, filename);
            if (!File.Exists(historyfile))
                return;

            foreach(string line in File.ReadAllLines(historyfile))
            {
                string[] tokens = line.Split('=');

                if (tokens.Length != 2)
                    continue;

                long timestamp;

                if (!long.TryParse(tokens[1], out timestamp))
                    continue;

                history[tokens[0]] = timestamp;
            }
        }

        public bool TimestampMatches(string key, long serverTimestamp)
        {
            if(history.ContainsKey(key))
            {
                return history[key] == serverTimestamp;
            }

            return false;
        }

        public void UpdateAndSave(string newKey, long newTimestamp)
        {
            history[newKey] = newTimestamp;
            string historyfile = Path.Combine(App.GameFolder, filename);

            using(FileStream fstream = new FileStream(historyfile, FileMode.Create, FileAccess.Write))
            {
                using(StreamWriter sw = new StreamWriter(fstream))
                {
                    foreach (KeyValuePair<string, long> pair in history)
                    {
                        sw.Write(pair.Key);
                        sw.Write('=');
                        sw.Write(pair.Value);
                        sw.Write('\n');
                    }
                }
            }
        }
    }

    /// <summary>
    /// Interaction logic for MultiplayerWindow.xaml
    /// </summary>
    public partial class MultiplayerWindow : Window
    {
        private HttpClient m_httpclient;

        private MultiplayerDownloadHistory m_history;


        public MultiplayerWindow()
        {
            InitializeComponent();

            m_httpclient = new HttpClient();
            m_httpclient.Timeout = new System.TimeSpan(0, 0, 10);
            m_history = new MultiplayerDownloadHistory();
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

        private void DownloadMod(string url, string modName, string historyKey)
        {
            if (!Utils.CheckBoth())
                return;

            string destination = Path.Combine(App.DirDisabled, modName);
            if(!File.Exists(destination))
            {
                destination = Path.Combine(App.DirMods, modName);
            }
            
            MultiplayerDownloadBox window = new MultiplayerDownloadBox();
            window.Owner = this;
            window.m_httpclient = m_httpclient;
            window.m_url = url;
            window.m_destination = destination;
            window.m_history = m_history;
            window.m_historyKey = historyKey;
            window.Show();
            window.Focus();
        }

        private void Button_DownloadCoop_Click(object sender, RoutedEventArgs e)
        {
            const string url = "https://github.com/Velserus/doom-eternal-modded-multiplayer/releases/download/co-op/Co-operative_Mode.zip";
            DownloadMod(url, App.NameCoop, "coop");
        }

        private void Button_DownloadSVS_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Button_DownloadInvasion_Click(object sender, RoutedEventArgs e)
        {
            const string url = "https://github.com/Velserus/doom-eternal-modded-multiplayer/releases/download/invasion/Invasion_Mode.zip";
            DownloadMod(url, App.NameInvasion, "invasion");
        }
    }
}
