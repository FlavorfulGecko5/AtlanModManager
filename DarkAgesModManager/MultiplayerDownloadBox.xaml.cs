using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Threading;
using System.Net.Http;
using System.Security.Policy;
using System.Runtime.CompilerServices;
using System.Net;

namespace DarkAgesModManager
{
    /// <summary>
    /// Interaction logic for MultiplayerDownloadBox.xaml
    /// </summary>
    public partial class MultiplayerDownloadBox : Window
    {

        /* These variables are passed to the window by it's creator */

        public HttpClient m_httpclient;
        public MultiplayerDownloadHistory m_history;
        public string m_historyKey;
        public string m_url;
        public string m_destination;


        /* Variables owned by window */

        private CancellationTokenSource m_cancelSource;
        private readonly string InProgressPath = Path.Combine(App.GameFolder, "Atlan_Active_Download.bin");
        private long m_modtime;
        private long m_fileSize;

        private void UpdateLabel(string newLabel)
        {
            this.Dispatcher.Invoke(() => { this.TextBlockStatus.Text = newLabel; });
        }

        private async void TryDownload()
        {
            FileStream fstream = null;
            Stream downloadStream = null;
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Head, m_url);
                HttpResponseMessage response = await m_httpclient.SendAsync(request, m_cancelSource.Token);
                response.EnsureSuccessStatusCode();

                m_fileSize = response.Content.Headers.ContentLength.Value;
                m_modtime = response.Content.Headers.LastModified.Value.Ticks;

                if(m_history.TimestampMatches(m_historyKey, m_modtime))
                {
                    string prompt = "You've already downloaded the latest version of this file!"
                        + "\n\nClick 'Yes' to re-download this file anyways."
                        + "\n\nClick 'No' to cancel the download.";

                    MessageBoxResult result = MessageBox.Show(prompt, "Force Download?", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);

                    if(result != MessageBoxResult.Yes)
                    {
                        this.Dispatcher.Invoke(() => { Close(); });
                        return;
                    }
                }

                UpdateLabel($"Downloading {(m_fileSize / 1000000)} megabyte file");

                downloadStream = await m_httpclient.GetStreamAsync(m_url);
                fstream = new FileStream(InProgressPath, FileMode.Create, FileAccess.Write);

                // TODO: If we want to have a download progress tracker, we'll
                // need to replace this function with our own read/write loop
                await downloadStream.CopyToAsync(fstream, 81920, m_cancelSource.Token);

                await fstream.FlushAsync(m_cancelSource.Token);
                fstream.Close();
                downloadStream.Close();


                if (File.Exists(m_destination))
                    File.Delete(m_destination);
                File.Move(InProgressPath, m_destination);

                m_history.UpdateAndSave(m_historyKey, m_modtime);

                this.Dispatcher.Invoke(() =>
                {
                    TextBlockStatus.Text = "Download Complete";
                    ButtonCancelDownload.Content = "Okay";
                });
            }
            catch(Exception ex)
            {
                UpdateLabel($"ERROR: {ex.Message}: {(ex.InnerException != null ? ex.InnerException.Message : "")}");

                // Need to close the streams to prevent IO exceptions
                // when attempting to delete the files
                if (fstream != null)
                    fstream.Close();
                if (downloadStream != null)
                    downloadStream.Close();

                if (File.Exists(InProgressPath))
                    File.Delete(InProgressPath);
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            (Owner as MultiplayerWindow).MainGrid.IsEnabled = false;
            
            TryDownload();
        }

        /* Boiler Plate */

        public MultiplayerDownloadBox()
        {
            InitializeComponent();
            m_cancelSource = new CancellationTokenSource();
        }

        private void ButtonCancelDownload_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            m_cancelSource.Cancel();
            (Owner as MultiplayerWindow).MainGrid.IsEnabled = true;
            Owner = null;
        }
    }
}
