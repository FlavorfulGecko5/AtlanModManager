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

        public MultiplayerBackend backend;


        /* Variables owned by window */

        private CancellationTokenSource m_cancelSource;
        private readonly string InProgressPath = Path.Combine(App.GameFolder, "Atlan_Active_Download.bin");
        private long m_modtime;
        private long m_fileSize;

        private void UpdateLabel(string newLabel)
        {
            this.Dispatcher.Invoke(() => { this.TextBlockStatus.Text = newLabel; });
        }

        private void DownloadWithProgress(FileStream fstream, Stream downloadStream, CancellationToken cancelToken)
        {
            byte[] buffer = new byte[81920];

            int lastPercent = 0;
            int percent = 0;

            double totalRead = 0;
            int bytesRead;

            while((bytesRead = downloadStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (cancelToken.IsCancellationRequested)
                    return;
                fstream.Write(buffer, 0, bytesRead);

                totalRead += bytesRead;
                percent = (int)((totalRead / m_fileSize) * 100);

                if(percent - lastPercent >= 5)
                {
                    lastPercent = percent;
                    this.Dispatcher.Invoke(() => { TextBlockProgress.Text = $"Progress: {lastPercent}%"; });
                }
            }
            this.Dispatcher.Invoke(() => { TextBlockProgress.Text = "Progress: 100%"; });
        }

        private async void TryDownload()
        {
            FileStream fstream = null;
            Stream downloadStream = null;
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Head, backend.url);
                HttpResponseMessage response = await backend.httpclient.SendAsync(request, m_cancelSource.Token);
                response.EnsureSuccessStatusCode();

                m_fileSize = response.Content.Headers.ContentLength.Value;
                m_modtime = response.Content.Headers.LastModified.Value.UtcTicks;

                if(backend.history.TimestampMatches(backend.historykey, m_modtime))
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

                downloadStream = await backend.httpclient.GetStreamAsync(backend.url);
                fstream = new FileStream(InProgressPath, FileMode.Create, FileAccess.Write);

                await Task.Run(() => DownloadWithProgress(fstream, downloadStream, m_cancelSource.Token), m_cancelSource.Token);
                m_cancelSource.Token.ThrowIfCancellationRequested();

                await fstream.FlushAsync(m_cancelSource.Token);
                fstream.Close();
                downloadStream.Close();


                if (File.Exists(backend.destination))
                    File.Delete(backend.destination);
                File.Move(InProgressPath, backend.destination);

                backend.history.UpdateAndSave(backend.historykey, new MultiplayerHistoryEntry(m_modtime, DateTime.UtcNow.Ticks));

                this.Dispatcher.Invoke(() =>
                {
                    TextBlockStatus.Text = $"Download Complete: File saved to \n{backend.destination}";
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
