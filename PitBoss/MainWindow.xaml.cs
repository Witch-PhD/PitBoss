﻿using Microsoft.Win32;
using PitBoss.UserControls;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static PitBoss.PitBossConstants;

namespace PitBoss
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
#if DEBUG
        [DllImport("Kernel32")]
        public static extern void AllocConsole();

        [DllImport("Kernel32", SetLastError = true)]
        public static extern void FreeConsole();
#endif
        public MainWindow()
        {
#if DEBUG
            AllocConsole();
#endif
            InitializeComponent();
            this.Title = "The Pit Boss";
            this.SizeToContent = SizeToContent.WidthAndHeight;
            dataManager = DataManager.Instance;
            StatusBar_GunsConnectedValue_TextBlock.DataContext = dataManager;
        }

        DataManager dataManager;

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            theUserOptionsUserControl.CloseAllWindows();
#if DEBUG
            FreeConsole();
#endif
            //theSpotterUserControl.CloseAllWindows();
            //theGunnerUserControl.CloseAllWindows();
        }

        public void SetOperatingMode(DataManager.ProgramOperatingMode mode)
        {
            switch (mode)
            {
                case DataManager.ProgramOperatingMode.eIdle:
                    //Gunner_TabItem.IsEnabled = true;
                    //Spotter_TabItem.IsEnabled = true;
                    Gunner_TabItem.Visibility = Visibility.Collapsed;
                    Spotter_TabItem.Visibility = Visibility.Collapsed;
                    break;

                case DataManager.ProgramOperatingMode.eSpotter:
                    //Spotter_TabItem.IsSelected = true;
                    //Gunner_TabItem.IsEnabled = false;
                    Spotter_TabItem.Visibility = Visibility.Visible;
                    Gunner_TabItem.Visibility = Visibility.Collapsed;
                    break;

                case DataManager.ProgramOperatingMode.eGunner:
                    //Gunner_TabItem.IsSelected = true;
                    //Spotter_TabItem.IsEnabled = false;
                    Gunner_TabItem.Visibility= Visibility.Visible;
                    Spotter_TabItem.Visibility = Visibility.Collapsed;
                    break;

                default:
                    Gunner_TabItem.IsEnabled = true;
                    Spotter_TabItem.IsEnabled = true;
                    //Gunner_TabItem.Visibility = Visibility.Visible;
                    //Spotter_TabItem.Visibility = Visibility.Visible;
                    break;
            }
        }

        AboutWindow? aboutWindow;
        private void About_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (aboutWindow == null)
            {
                aboutWindow = new AboutWindow();
                aboutWindow.Title = "Version Info";
                aboutWindow.ResizeMode = ResizeMode.CanMinimize;
                aboutWindow.SizeToContent = SizeToContent.WidthAndHeight;
                aboutWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                aboutWindow.Closing += (o, ev) =>
                {
                    aboutWindow = null;
                };
            }
            aboutWindow.Show();
        }

        private void Language_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if(sender is MenuItem item)
            {
                string languageName = item.Name[..^9];
                Application.Current.Resources.MergedDictionaries.Clear();
                ResourceDictionary dictionary = new()
                {
                    Source = new Uri((@"\Rescources\"+languageName+".xaml"), UriKind.Relative)
                };
                Application.Current.Resources.MergedDictionaries.Add(dictionary);
            }
        }

        private void SaveSettings_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "XML File|*.xml";
            saveFileDialog1.Title = "Save to File";
            saveFileDialog1.ShowDialog();

            // If the file name is not an empty string open it for saving.
            if (saveFileDialog1.FileName != "")
            {
                SerializableUserOptions options = new SerializableUserOptions(dataManager.userOptions);
                options.SerializeToFile(saveFileDialog1.FileName);
            }
        }

        private void LoadSettings_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.FileName = "Settings"; // Default file name
            dialog.DefaultExt = ".xml"; // Default file extension
            dialog.Filter = "XML File (.xml)|*.xml"; // Filter files by extension

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                string filename = dialog.FileName;

                SerializableUserOptions theOptions = new(dataManager.userOptions);
                theOptions.DeserializeFrom(filename);
                dataManager.userOptions = theOptions;
                theUserOptionsUserControl.updateKeyBindingStrings();
            }
        }
    }
}