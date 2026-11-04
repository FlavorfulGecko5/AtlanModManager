using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DarkAgesModManager
{
    /// <summary>
    /// Interaction logic for MultiplayerAboutWindow.xaml
    /// </summary>
    public partial class MultiplayerAboutWindow : Window
    {
        public MultiplayerAboutWindow()
        {
            InitializeComponent();
        }

        public void SetModDescription(string key)
        {
            Block coop = MainTextBox.Document.Blocks.FirstBlock;
            Block invasion = coop.NextBlock;
            Block svs = invasion.NextBlock;

            if (key != "coop")
                MainTextBox.Document.Blocks.Remove(coop);

            if (key != "invasion")
                MainTextBox.Document.Blocks.Remove(invasion);

            if (key != "svs")
                MainTextBox.Document.Blocks.Remove(svs);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            (Owner as MultiplayerWindow).MainGrid.IsEnabled = false;
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            (Owner as MultiplayerWindow).MainGrid.IsEnabled = true;
            Owner = null;
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
