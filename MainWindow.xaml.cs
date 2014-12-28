using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SharpTerminal
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;
            Title += " (" + version + ")";

            DispatcherTimer timer = new DispatcherTimer(TimeSpan.FromSeconds(5), DispatcherPriority.Background, Timer, this.Dispatcher);
            timer.Start();
        }
        void Timer(object obj, EventArgs e)
        {
            ViewModel.Serial.UpdatePorts();
        }
        MainViewModel ViewModel
        {
            get
            {
                return (MainViewModel)DataContext;
            }
        }
        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Serial.Open();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Serial.Close();
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string s = SendText.Text;
            if (s.Length > 0)
            {
                if (SendText.Items.Contains(s))
                    SendText.Items.Remove(s);
                SendText.Items.Add(s);
            }
            await ViewModel.Serial.SendAsync(s + Encoding.ASCII.GetString(ViewModel.SelectedLineEnding.Data));
            SendText.Text = "";
        }

        private void SendText_KeyDown(object sender, KeyEventArgs e)
        {

            if(e.Key == Key.Return)
            {
                SendButton_Click(null, null);
            }
        }

        private void SentBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (AutoScrollCheck.IsChecked == true)
            {
                SentBox.ScrollToEnd();
                if (ViewModel.Serial.SentData.Any())
                    SentDataGrid.ScrollIntoView(ViewModel.Serial.SentData.Last());
            }
        }

        private void ReceivedBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (AutoScrollCheck.IsChecked == true)
            {
                ReceivedBox.ScrollToEnd();
                if (ViewModel.Serial.ReceivedData.Any())
                    ReceivedDataGrid.ScrollIntoView(ViewModel.Serial.ReceivedData.Last());
            }
        }

        private void ClearSentData_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Serial.ClearSentData();
        }

        private void ClearReceivedData_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Serial.ClearReceivedData();
        }

        private async void SendAgain_Click(object sender, RoutedEventArgs e)
        {
            var selected = SentDataGrid.SelectedItem as SerialData;
            if(selected != null)
            {
                await ViewModel.Serial.SendAsync(selected.Data);
            }
        }

    }
}
