using Microsoft.Win32;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using Path = System.IO.Path;

namespace DakkaDataLink
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
            dataManager = DisplayManager.Instance;
            LoadSettings();
            
            if (dataManager.userOptions.SaveSelectedLanguage == false)
            {
                string cultureName = CultureInfo.CurrentCulture.EnglishName;
                string languageName = cultureName.Split(' ')[0].Trim();

                string[] supportedLanguages = ["English", "German", "Italian", "Polish", "Russian", "Spanish"];

                if (supportedLanguages.Contains(languageName))
                {
                    LoadLanguage(languageName);
                }
                else
                {
                    LoadLanguage(dataManager.userOptions.Language);
                }
            }
            else
            {
                //           CheckSaveSelectedLanguageWithoudEvent(true);
                LoadLanguage(dataManager.userOptions.Language);
            }
            InitializeComponent();
            theUserOptionsUserControl.updateKeyBindingStrings();
            Title = $"Dakka Data Link - v{DdlConstants.VERSION_STRING}";
            SizeToContent = SizeToContent.WidthAndHeight;

            StatusBar_GunsConnectedValue_TextBlock.DataContext = dataManager;

            SaveSelectedLanguage_MenuItem.DataContext = dataManager.userOptions;
        }

        DisplayManager dataManager;

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            //displayManager.StopGrpcClient();
            //displayManager.StopGrpcServer();
            dataManager.StopUdp();
            theUserOptionsUserControl.CloseAllWindows();
            GlobalLogger.Log("Application exiting.");
            GlobalLogger.Shutdown();
            #if DEBUG
            FreeConsole();
            #endif
            //theSpotterUserControl.CloseAllWindows();
            //theGunnerUserControl.CloseAllWindows();
        }

        public void SetOperatingMode(DisplayManager.ProgramOperatingMode mode)
        {
            switch (mode)
            {
                case DisplayManager.ProgramOperatingMode.eIdle:
                    //Gunner_TabItem.IsEnabled = true;
                    //Spotter_TabItem.IsEnabled = true;
                    dataManager.DeactivateKeyboardListener();
                    Gunner_TabItem.Visibility = Visibility.Collapsed;
                    Spotter_TabItem.Visibility = Visibility.Collapsed;
                    break;

                case DisplayManager.ProgramOperatingMode.eSpotter:
                    //Spotter_TabItem.IsSelected = true;
                    //Gunner_TabItem.IsEnabled = false;
                    dataManager.ActivateKeyboardListener();
                    Spotter_TabItem.Visibility = Visibility.Visible;
                    Gunner_TabItem.Visibility = Visibility.Collapsed;
                    break;

                case DisplayManager.ProgramOperatingMode.eGunner:
                    //Gunner_TabItem.IsSelected = true;
                    //Spotter_TabItem.IsEnabled = false;
                    dataManager.DeactivateKeyboardListener();
                    Gunner_TabItem.Visibility = Visibility.Visible;
                    Spotter_TabItem.Visibility = Visibility.Collapsed;
                    break;

                default:
                    dataManager.DeactivateKeyboardListener();
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
                aboutWindow = new AboutWindow
                {
                    Title = "Version Info",
                    ResizeMode = ResizeMode.CanMinimize,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

                aboutWindow.Closing += (o, ev) =>
                {
                    aboutWindow = null;
                };
            }
            aboutWindow.Show();
        }

        private void Language_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item)
            {
                string languageName = item.Name[..^9];
                if (languageName == "Orky")
                {
                    LoadLanguage("Orkish");
                }
                else
                {
                    LoadLanguage(languageName);
                }
                dataManager.userOptions.Language = languageName;
            }
        }

        private void LoadLanguage(string languageName)
        {
            Application.Current.Resources.MergedDictionaries.Clear();
            ResourceDictionary dictionary = new()
            {
                Source = new Uri((@"\Resources\" + languageName + ".xaml"), UriKind.Relative)
            };
            Application.Current.Resources.MergedDictionaries.Add(dictionary);
        }

        private void SaveSettings_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DakkaDataLink");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            SaveFileDialog saveFileDialog1 = new SaveFileDialog
            {
                Filter = "XML File|*.xml",
                Title = "Save to File",
                InitialDirectory = folderPath
            };

            if (saveFileDialog1.ShowDialog() == true)
            {
                if (!string.IsNullOrEmpty(saveFileDialog1.FileName))
                {
                    SerializableUserOptions options = new(dataManager.userOptions);
                    options.SerializeToFile(saveFileDialog1.FileName);
                }
            }
        }

        private void LoadSettings_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DakkaDataLink");

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                FileName = "Settings", // Default file name
                DefaultExt = ".xml", // Default file extension
                Filter = "XML File (.xml)|*.xml", // Filter files by extension
                InitialDirectory = folderPath
            };

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                string filename = dialog.FileName;

                SerializableUserOptions theOptions = new(dataManager.userOptions);
                theOptions.DeserializeFrom(filename);
                dataManager.userOptions = theOptions;
                theUserOptionsUserControl.updateKeyBindingStrings();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            SaveUserSettings();
        }

        private void SaveUserSettings()
        {
            string programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string pitBossFolderPath = Path.Combine(programDataPath, "DakkaDataLink");

            if (!Directory.Exists(pitBossFolderPath))
            {
                Directory.CreateDirectory(pitBossFolderPath);
            }

            string settingsFilePath = Path.Combine(pitBossFolderPath, "Settings.xml");

            SerializableUserOptions options = new(dataManager.userOptions);
            options.SerializeToFile(settingsFilePath);
        }

        private void LoadSettings()
        {
            string programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string pitBossFolder = Path.Combine(programDataPath, "DakkaDataLink");
            string settingsFilePath = Path.Combine(pitBossFolder, "Settings.xml");

            if (File.Exists(settingsFilePath))
            {
                try
                {
                    SerializableUserOptions theOptions = new(dataManager.userOptions);
                    theOptions.DeserializeFrom(settingsFilePath);
                    dataManager.userOptions = theOptions;
                    
                    //theUserOptionsUserControl.updateKeyBindingStrings();
                }
                catch (Exception ex)
                {
                    GlobalLogger.Log("*** LoadSettings Exception. Settings files possibly corrupted. Rewriting with default values.");
                    SaveUserSettings();
                }
            }
            else
            {
                SaveUserSettings();
            }
        }

        //    private void SaveSelectedLanguage_CheckBox_Changed(object sender, RoutedEventArgs e)
        //    {
        //        displayManager.userOptions.SaveSelectedLanguage = SaveSelectedLanguage_CheckBox.IsChecked == true;
        //    }
        //
        //    private void CheckSaveSelectedLanguageWithoudEvent(bool isChecked)
        //    {
        //        SaveSelectedLanguage_CheckBox.Checked -= SaveSelectedLanguage_CheckBox_Changed;
        //        SaveSelectedLanguage_CheckBox.Unchecked -= SaveSelectedLanguage_CheckBox_Changed;
        //
        //        SaveSelectedLanguage_CheckBox.IsChecked = isChecked;
        //
        //        SaveSelectedLanguage_CheckBox.Checked += SaveSelectedLanguage_CheckBox_Changed;
        //        SaveSelectedLanguage_CheckBox.Unchecked += SaveSelectedLanguage_CheckBox_Changed;
        //    }
    }
}