using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace DakkaDataLink.UserControls
{
    /// <summary>
    /// Interaction logic for UserOptions_UserControl.xaml
    /// </summary>
    public partial class UserOptions_UserControl : UserControl, INotifyPropertyChanged
    {
        private double opacity = 0.1;
        private Uri checkedDictionaryUri;
        public ObservableCollection<TranslatableComboBoxItem> comboBoxColorOptions = new ObservableCollection<TranslatableComboBoxItem>();
        public ObservableCollection<TranslatableComboBoxItem> comboBoxWeightOptions = new ObservableCollection<TranslatableComboBoxItem>();
        public ObservableCollection<string> comboBoxAudioAlertOptions = new ObservableCollection<string>();

        private SolidColorBrush[] brushes =
        {
            Brushes.Red, Brushes.Orange, Brushes.Yellow, Brushes.Green,
                Brushes.Blue, Brushes.Purple, Brushes.Magenta, Brushes.Pink,
                Brushes.Black, Brushes.White, Brushes.Gray, Brushes.DarkGray
        };

        private List<FontWeight> fontWeights =
        [
            FontWeights.Light, FontWeights.Normal, 
            FontWeights.Bold, FontWeights.ExtraBold
        ];

        private Dictionary<string, string> hexColorToBrushNameDict = new Dictionary<string, string>();

        public UserOptions_UserControl()
        {
            dataManager = DisplayManager.Instance;
            InitializeComponent();
            DataContext = dataManager;
            initComboBoxes();
            updateKeyBindingStrings();
            dataManager.newCoordsReceived += OnNewCoordsReceived;
            dataManager.userOptions.PropertyChanged += OnPropertyChanged;
        }

        DisplayManager dataManager;

        private void initComboBoxes()
        {

            foreach (string artyName in ArtilleryProfiles.Instance.ArtyProfilesDict.Keys)
            {
                ArtyProfile_ComboBox.Items.Add(artyName);
            }
            ArtyProfile_ComboBox.SelectedItem = ArtyProfile_ComboBox.Items[0];
            LabelColor_ComboBox.SelectedValue = nameof(dataManager.userOptions.OverlayLabelsFontColor);
            FontWeight_ComboBox.SelectedValue = dataManager.userOptions.OverlayFontWeight.ToString();
            initColors();
            initSounds();
            initWeight();
        }

        private void initColors()
        {
            var values = typeof(Brushes).GetProperties().Select(p => new { Name = p.Name, Brush = p.GetValue(null) as Brush }).ToArray();

            for (int i = 0; i < brushes.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    if (values[j].Brush == brushes[i])
                    {
                        hexColorToBrushNameDict[brushes[i].ToString()] = values[j].Name;
                    }
                }
            }

            comboBoxColorOptions.Clear();
            foreach (string brushMapping in hexColorToBrushNameDict.Values)
            {
                TranslatableComboBoxItem colorItem = new TranslatableComboBoxItem
                {
                    Content = Application.Current.Resources["comboboxitem_color_" + brushMapping.ToLower()] as string,
                    Tag = brushMapping
                };
                comboBoxColorOptions.Add(colorItem);
            }

            LabelColor_ComboBox.ItemsSource = comboBoxColorOptions;
            ValueColor_ComboBox.ItemsSource = comboBoxColorOptions;
            OverlayBackgroundColor_ComboBox.ItemsSource = comboBoxColorOptions;
            OverlayAlertColor_ComboBox.ItemsSource = comboBoxColorOptions;

            OverlayBackgroundColor_ComboBox.SelectedValue = hexColorToBrushNameDict[dataManager.userOptions.OverlayBackgroundColor.ToString()];
            OverlayAlertColor_ComboBox.SelectedValue = hexColorToBrushNameDict[dataManager.userOptions.OverlayAlertBorderColor.ToString()];
            ValueColor_ComboBox.SelectedValue = hexColorToBrushNameDict[dataManager.userOptions.OverlayValuesFontColor.ToString()];
            LabelColor_ComboBox.SelectedValue = hexColorToBrushNameDict[dataManager.userOptions.OverlayLabelsFontColor.ToString()];
        }

        private void initSounds()
        {
            comboBoxAudioAlertOptions.Clear();
            AudioAlert_ComboBox.ItemsSource = comboBoxAudioAlertOptions;
            string[] filesInSoundDir = Directory.GetFiles("Sounds", "*.wav");

            for (int fileNum = 0; fileNum <= filesInSoundDir.Length-1; fileNum++)
            {

                string[] fileNameParts = filesInSoundDir[fileNum].Split("\\");
                string fileName = fileNameParts[fileNameParts.Length-1];
                fileName = fileName.Replace(".wav", "");
                filesInSoundDir[fileNum] = fileName;
                comboBoxAudioAlertOptions.Add(fileName);
            }
            //AudioAlert_ComboBox.SelectedIndex = 0;

            AudioAlert_ComboBox.SelectedValue = dataManager.userOptions.AudioAlertFile;

            
            AudioAlertVolume_Slider.DataContext = dataManager.userOptions.AudioAlertVolume;
            AudioAlertVolume_Slider.Value = dataManager.userOptions.AudioAlertVolume;
        }

        private void initWeight()
        {
            comboBoxWeightOptions.Clear();
            foreach (FontWeight weight in fontWeights)
            {
                TranslatableComboBoxItem weightItem = new TranslatableComboBoxItem
                {
                    Content = Application.Current.Resources["comboboxitem_font_weight_" + weight.ToString().ToLower()].ToString(),
                    Tag = weight.ToString()
                };
                comboBoxWeightOptions.Add(weightItem);
            }

            FontWeight_ComboBox.ItemsSource = comboBoxWeightOptions;
            FontWeight_ComboBox.SelectedValue = dataManager.userOptions.OverlayFontWeight.ToString();
        }

        public void CloseAllWindows()
        {
            if (overlayWindow != null)
            {
                dataManager.userOptions.OverlayOpacity = OverlayOpacity_Slider.Value;
                dataManager.userOptions.AudioAlertVolume = AudioAlertVolume_Slider.Value;
            }
            overlayWindow?.Close();
        }

        Overlay_Window? overlayWindow;
        private void ToggleOverlay_Button_Click(object sender, RoutedEventArgs e)
        {
            if (overlayWindow == null) // Opening Window
            {
                initColors();
                initSounds();
                initWeight();
                overlayWindow = new Overlay_Window();
                overlayWindow.BackGroundBrush.Color = dataManager.userOptions.OverlayBackgroundColor.Color;
                overlayWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                overlayWindow.ResizeMode = ResizeMode.CanResizeWithGrip;
                OverlayOpacity_Slider.IsEnabled = true;
                LabelColor_ComboBox.IsEnabled = true;
                ValueColor_ComboBox.IsEnabled = true;
                OverlayBackgroundColor_ComboBox.IsEnabled = true;
                OverlayAlertColor_ComboBox.IsEnabled = true;
                FontWeight_ComboBox.IsEnabled = true;
                FontSize_TextBox.IsEnabled = true;
                OverlayOpacity_Slider.DataContext = overlayWindow.BackGroundBrush;
                OverlayOpacity_TextBox.DataContext = overlayWindow.BackGroundBrush;
                AudioAlertVolume_Slider.DataContext = overlayWindow;
                UseFlashAlert_CheckBox.IsEnabled = true;
                UseFlashAlert_CheckBox.DataContext = overlayWindow;
                UseAudioAlert_CheckBox.IsEnabled = true;
                UseAudioAlert_CheckBox.DataContext = overlayWindow;
                AudioAlert_ComboBox.IsEnabled = true;
                AudioAlertVolume_Slider.IsEnabled = true;
                AudioAlertPlay_Button.IsEnabled = true;
                overlayWindow.AudioAlertFilePath = "\\Sounds\\" + dataManager.userOptions.AudioAlertFile + ".wav";
                overlayWindow.Closing += (o, ev) =>
                {
                    overlayWindow = null;
                };

                overlayWindow.Show();
                overlayWindow.BackGroundBrush.Opacity = dataManager.userOptions.OverlayOpacity;
                AudioAlertVolume_Slider.Value = dataManager.userOptions.AudioAlertVolume;
            }
            else // Closing Window
            {
                dataManager.userOptions.OverlayOpacity = OverlayOpacity_Slider.Value;
                dataManager.userOptions.AudioAlertVolume = AudioAlertVolume_Slider.Value;
                //opacity = OverlayOpacity_Slider.Value;
                OverlayOpacity_Slider.DataContext = null;
                AudioAlertVolume_Slider.DataContext = null;
                OverlayOpacity_TextBox.DataContext = null;
                OverlayOpacity_TextBox.Text = "N/A";
                OverlayOpacity_Slider.IsEnabled = false;
                LabelColor_ComboBox.IsEnabled = false;
                ValueColor_ComboBox.IsEnabled = false;
                OverlayBackgroundColor_ComboBox.IsEnabled = false;
                OverlayAlertColor_ComboBox.IsEnabled = false;
                FontWeight_ComboBox.IsEnabled = false;
                FontSize_TextBox.IsEnabled = false;
                UseFlashAlert_CheckBox.IsEnabled = false;
                UseFlashAlert_CheckBox.DataContext = null;
                UseAudioAlert_CheckBox.IsEnabled = false;
                UseAudioAlert_CheckBox.DataContext = null;
                AudioAlert_ComboBox.IsEnabled = false;
                AudioAlertVolume_Slider.IsEnabled = false;
                AudioAlertPlay_Button.IsEnabled = false;
                
                overlayWindow.Close();
            }
        }

        private bool m_FlashOnNewCoords = true;
        public bool FlashOnNewCoords
        {
            get { return m_FlashOnNewCoords; }
            set { m_FlashOnNewCoords = value; OnPropertyChanged(); }
        }

        private void OnNewCoordsReceived(object sender, bool args)
        {
            if (m_FlashOnNewCoords)
            {
                overlayWindow?.FlashOverlay();
                overlayWindow?.PlayAudioAlert();
            }
        }

        private void TextBox_UpdateOnEnter(object sender, KeyEventArgs args)
        {
            if (args.Key == Key.Enter)
            {
                TextBox textBox = sender as TextBox;
                if (textBox.GetBindingExpression(TextBox.TextProperty).ValidateWithoutUpdate())
                {
                    textBox.GetBindingExpression(TextBox.TextProperty).UpdateSource();
                }
            }
        }

        private void ArtyProfile_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ArtilleryProfiles.Instance.CurrentProfile = ArtilleryProfiles.Instance.ArtyProfilesDict[ArtyProfile_ComboBox.SelectedValue as string];
            dataManager.LatestDist = ArtilleryProfiles.Instance.CurrentProfile.MinDist;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }




        private void disableSetBindingButtons()
        {
            ChangeBindingFor_AzPlus1_Button.IsEnabled = false;
            ChangeBindingFor_AzMinus1_Button.IsEnabled = false;
            ChangeBindingFor_AzPlus10_Button.IsEnabled = false;
            ChangeBindingFor_AzMinus10_Button.IsEnabled = false;
            ChangeBindingFor_DistPlusOne_Button.IsEnabled = false;
            ChangeBindingFor_DistMinusOne_Button.IsEnabled = false;
            ChangeBindingFor_DistPlusMulti_Button.IsEnabled = false;
            ChangeBindingFor_DistMinusMulti_Button.IsEnabled = false;
            ChangeBindingFor_SendCoords_Button.IsEnabled = false;
        }

        private void enableSetBindingButtons()
        {
            ChangeBindingFor_AzPlus1_Button.IsEnabled = true;
            ChangeBindingFor_AzMinus1_Button.IsEnabled = true;
            ChangeBindingFor_AzPlus10_Button.IsEnabled = true;
            ChangeBindingFor_AzMinus10_Button.IsEnabled = true;
            ChangeBindingFor_DistPlusOne_Button.IsEnabled = true;
            ChangeBindingFor_DistMinusOne_Button.IsEnabled = true;
            ChangeBindingFor_DistPlusMulti_Button.IsEnabled = true;
            ChangeBindingFor_DistMinusMulti_Button.IsEnabled = true;
            ChangeBindingFor_SendCoords_Button.IsEnabled = true;
        }

        public void updateKeyBindingStrings()
        {
            Dictionary<string, KeyCombo> combosList = dataManager.userOptions.BindingDictionary;
            AzPlusOne_KeyBinding_TextBox.Text = dataManager.userOptions.BindingDictionary[UserOptions.AZ_UP_ONE_DEG_DICT_KEY].ToString();
            AzMinusOne_KeyBinding_TextBox.Text = dataManager.userOptions.BindingDictionary[UserOptions.AZ_DOWN_ONE_DEG_DICT_KEY].ToString();
            AzPlusTen_KeyBinding_TextBox.Text = dataManager.userOptions.BindingDictionary[UserOptions.AZ_UP_MULTI_DEG_DICT_KEY].ToString();
            AzMinusTen_KeyBinding_TextBox.Text = dataManager.userOptions.BindingDictionary[UserOptions.AZ_DOWN_MULTI_DEG_DICT_KEY].ToString();
            DistPlusOne_KeyBinding_TextBox.Text = dataManager.userOptions.BindingDictionary[UserOptions.DIST_UP_ONE_TICK_DICT_KEY].ToString();
            DistMinusOne_KeyBinding_TextBox.Text = dataManager.userOptions.BindingDictionary[UserOptions.DIST_DOWN_ONE_TICK_DICT_KEY].ToString();
            DistPlusMulti_KeyBinding_TextBox.Text = dataManager.userOptions.BindingDictionary[UserOptions.DIST_UP_MULTI_TICK_DICT_KEY].ToString();
            DistMinusMulti_KeyBinding_TextBox.Text = dataManager.userOptions.BindingDictionary[UserOptions.DIST_DOWN_MULTI_TICK_DICT_KEY].ToString();
            SendCoords_KeyBinding_TextBox.Text = dataManager.userOptions.BindingDictionary[UserOptions.SEND_ARTY_MSG_DICT_KEY].ToString();
        }

        private bool setBindingsActivated = false;
        private void ChangeBindingFor_AzPlus1_Button_Click(object sender, RoutedEventArgs e)
        {
            if (!setBindingsActivated) // Changing Binding.
            {
                setBindingsActivated = !setBindingsActivated;
                disableSetBindingButtons();
                ChangeBindingFor_AzPlus1_Button.IsEnabled = true;
                ChangeBindingFor_AzPlus1_Button.Content = "Finish";
                SpotterKeystrokeHandler.Instance.BeginSetKeyBinding(UserOptions.AZ_UP_ONE_DEG_DICT_KEY);
            }
            else // Done changing binding.
            {
                setBindingsActivated = !setBindingsActivated;
                enableSetBindingButtons();
                ChangeBindingFor_AzPlus1_Button.IsEnabled = true;
                ChangeBindingFor_AzPlus1_Button.Content = "Change Key(s)";
                SpotterKeystrokeHandler.Instance.EndSetKeyBinding();
                updateKeyBindingStrings();
            }
        }

        private void ChangeBindingFor_AzMinus1_Button_Click(object sender, RoutedEventArgs e)
        {
            if (!setBindingsActivated) // Changing Binding.
            {
                setBindingsActivated = !setBindingsActivated;
                disableSetBindingButtons();
                ChangeBindingFor_AzMinus1_Button.IsEnabled = true;
                ChangeBindingFor_AzMinus1_Button.Content = "Finish";
                SpotterKeystrokeHandler.Instance.BeginSetKeyBinding(UserOptions.AZ_DOWN_ONE_DEG_DICT_KEY);
            }
            else // Done changing binding.
            {
                setBindingsActivated = !setBindingsActivated;
                enableSetBindingButtons();
                ChangeBindingFor_AzMinus1_Button.IsEnabled = true;
                ChangeBindingFor_AzMinus1_Button.Content = "Change Key(s)";
                SpotterKeystrokeHandler.Instance.EndSetKeyBinding();
                updateKeyBindingStrings();
            }
        }

        private void ChangeBindingFor_AzPlus10_Button_Click(object sender, RoutedEventArgs e)
        {
            if (!setBindingsActivated) // Changing Binding.
            {
                setBindingsActivated = !setBindingsActivated;
                disableSetBindingButtons();
                ChangeBindingFor_AzPlus10_Button.IsEnabled = true;
                ChangeBindingFor_AzPlus10_Button.Content = "Finish";
                SpotterKeystrokeHandler.Instance.BeginSetKeyBinding(UserOptions.AZ_UP_MULTI_DEG_DICT_KEY);
            }
            else // Done changing binding.
            {
                setBindingsActivated = !setBindingsActivated;
                enableSetBindingButtons();
                ChangeBindingFor_AzPlus10_Button.IsEnabled = true;
                ChangeBindingFor_AzPlus10_Button.Content = "Change Key(s)";
                SpotterKeystrokeHandler.Instance.EndSetKeyBinding();
                updateKeyBindingStrings();
            }
        }

        private void ChangeBindingFor_AzMinus10_Button_Click(object sender, RoutedEventArgs e)
        {
            if (!setBindingsActivated) // Changing Binding.
            {
                setBindingsActivated = !setBindingsActivated;
                disableSetBindingButtons();
                ChangeBindingFor_AzMinus10_Button.IsEnabled = true;
                ChangeBindingFor_AzMinus10_Button.Content = "Finish";
                SpotterKeystrokeHandler.Instance.BeginSetKeyBinding(UserOptions.AZ_DOWN_MULTI_DEG_DICT_KEY);
            }
            else // Done changing binding.
            {
                setBindingsActivated = !setBindingsActivated;
                enableSetBindingButtons();
                ChangeBindingFor_AzMinus10_Button.IsEnabled = true;
                ChangeBindingFor_AzMinus10_Button.Content = "Change Key(s)";
                SpotterKeystrokeHandler.Instance.EndSetKeyBinding();
                updateKeyBindingStrings();
            }
        }

        private void ChangeBindingFor_DistPlusOne_Button_Click(object sender, RoutedEventArgs e)
        {
            if (!setBindingsActivated) // Changing Binding.
            {
                setBindingsActivated = !setBindingsActivated;
                disableSetBindingButtons();
                ChangeBindingFor_DistPlusOne_Button.IsEnabled = true;
                ChangeBindingFor_DistPlusOne_Button.Content = "Finish";
                SpotterKeystrokeHandler.Instance.BeginSetKeyBinding(UserOptions.DIST_UP_ONE_TICK_DICT_KEY);
            }
            else // Done changing binding.
            {
                setBindingsActivated = !setBindingsActivated;
                enableSetBindingButtons();
                ChangeBindingFor_DistPlusOne_Button.IsEnabled = true;
                ChangeBindingFor_DistPlusOne_Button.Content = "Change Key(s)";
                SpotterKeystrokeHandler.Instance.EndSetKeyBinding();
                updateKeyBindingStrings();
            }
        }

        private void ChangeBindingFor_DistMinusOne_Button_Click(object sender, RoutedEventArgs e)
        {
            if (!setBindingsActivated) // Changing Binding.
            {
                setBindingsActivated = !setBindingsActivated;
                disableSetBindingButtons();
                ChangeBindingFor_DistMinusOne_Button.IsEnabled = true;
                ChangeBindingFor_DistMinusOne_Button.Content = "Finish";
                SpotterKeystrokeHandler.Instance.BeginSetKeyBinding(UserOptions.DIST_DOWN_ONE_TICK_DICT_KEY);
            }
            else // Done changing binding.
            {
                setBindingsActivated = !setBindingsActivated;
                enableSetBindingButtons();
                ChangeBindingFor_DistMinusOne_Button.IsEnabled = true;
                ChangeBindingFor_DistMinusOne_Button.Content = "Change Key(s)";
                SpotterKeystrokeHandler.Instance.EndSetKeyBinding();
                updateKeyBindingStrings();
            }
        }

        private void ChangeBindingFor_DistPlusMulti_Button_Click(object sender, RoutedEventArgs e)
        {
            if (!setBindingsActivated) // Changing Binding.
            {
                setBindingsActivated = !setBindingsActivated;
                disableSetBindingButtons();
                ChangeBindingFor_DistPlusMulti_Button.IsEnabled = true;
                ChangeBindingFor_DistPlusMulti_Button.Content = "Finish";
                SpotterKeystrokeHandler.Instance.BeginSetKeyBinding(UserOptions.DIST_UP_MULTI_TICK_DICT_KEY);
            }
            else // Done changing binding.
            {
                setBindingsActivated = !setBindingsActivated;
                enableSetBindingButtons();
                ChangeBindingFor_DistPlusMulti_Button.IsEnabled = true;
                ChangeBindingFor_DistPlusMulti_Button.Content = "Change Key(s)";
                SpotterKeystrokeHandler.Instance.EndSetKeyBinding();
                updateKeyBindingStrings();
            }
        }

        private void ChangeBindingFor_DistMinusMulti_Button_Click(object sender, RoutedEventArgs e)
        {
            if (!setBindingsActivated) // Changing Binding.
            {
                setBindingsActivated = !setBindingsActivated;
                disableSetBindingButtons();
                ChangeBindingFor_DistMinusMulti_Button.IsEnabled = true;
                ChangeBindingFor_DistMinusMulti_Button.Content = "Finish";
                SpotterKeystrokeHandler.Instance.BeginSetKeyBinding(UserOptions.DIST_DOWN_MULTI_TICK_DICT_KEY);
            }
            else // Done changing binding.
            {
                setBindingsActivated = !setBindingsActivated;
                enableSetBindingButtons();
                ChangeBindingFor_DistMinusMulti_Button.IsEnabled = true;
                ChangeBindingFor_DistMinusMulti_Button.Content = "Change Key(s)";
                SpotterKeystrokeHandler.Instance.EndSetKeyBinding();
                updateKeyBindingStrings();
            }
        }

        private void ChangeBindingFor_SendCoords_Button_Click(object sender, RoutedEventArgs e)
        {
            if (!setBindingsActivated) // Changing Binding.
            {
                setBindingsActivated = !setBindingsActivated;
                disableSetBindingButtons();
                ChangeBindingFor_SendCoords_Button.IsEnabled = true;
                ChangeBindingFor_SendCoords_Button.Content = "Finish";
                SpotterKeystrokeHandler.Instance.BeginSetKeyBinding(UserOptions.SEND_ARTY_MSG_DICT_KEY);
            }
            else // Done changing binding.
            {
                setBindingsActivated = !setBindingsActivated;
                enableSetBindingButtons();
                ChangeBindingFor_SendCoords_Button.IsEnabled = true;
                ChangeBindingFor_SendCoords_Button.Content = "Change Key(s)";
                SpotterKeystrokeHandler.Instance.EndSetKeyBinding();
                updateKeyBindingStrings();
            }
        }

        private void Shortcuts_Button_Click(object sender, RoutedEventArgs e)
        {
            if (ShorcutsMenu.Visibility == Visibility.Visible)
            {
                ShorcutsMenu.Visibility = Visibility.Hidden;
                Shortcuts_Button.Content = Application.Current.Resources["button_show"] as string;
            }
            else
            {
                ShorcutsMenu.Visibility = Visibility.Visible;
                Shortcuts_Button.Content = Application.Current.Resources["button_hide"] as string;
            }
        }

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Language")
            {
                initColors();
                initWeight();
            }
        }

        private bool DidLanugageChanged()
        {
            var dictionary = Application.Current.Resources.MergedDictionaries.FirstOrDefault();

            if (dictionary != null && dictionary.Source != null)
            {
                if (checkedDictionaryUri != null && checkedDictionaryUri.Equals(dictionary.Source))
                {
                    //Console.WriteLine("Check");
                    return false;
                }

                checkedDictionaryUri = dictionary.Source;
                return true;
            }

            checkedDictionaryUri = null;
            return true;
        }

        private void OverlayBackgroundColor_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (overlayWindow != null)
            {
                //ComboBoxItem thingy = OverlayBackgroundColor_ComboBox.SelectedItem as ComboBoxItem;
                //overlayWindow.BackGroundBrush.Color = (Color)ColorConverter.ConvertFromString(thingy.Content as string);
                

                string? selectedColorName = OverlayBackgroundColor_ComboBox.SelectedValue as string;
                if (selectedColorName == null)
                {
                    return;
                }
                BrushConverter brushConverter = new BrushConverter();
                SolidColorBrush selectedColorBrush = (SolidColorBrush)brushConverter.ConvertFromString(selectedColorName);
                dataManager.userOptions.OverlayBackgroundColor = selectedColorBrush;
                overlayWindow.BackGroundBrush.Color = selectedColorBrush.Color;
            }
        }

        private void OverlayAlertColor_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string? selectedColorName = OverlayAlertColor_ComboBox.SelectedValue as string;
            if (selectedColorName != null)
            {
                BrushConverter brushConverter = new BrushConverter();
                SolidColorBrush selectedColorBrush = (SolidColorBrush)brushConverter.ConvertFromString(selectedColorName);
                dataManager.userOptions.OverlayAlertBorderColor = selectedColorBrush;

                overlayWindow?.FlashOverlay();
            }
        }

        private void LabelColor_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string? selectedColorName = LabelColor_ComboBox.SelectedValue as string;
            if (selectedColorName != null)
            {
                BrushConverter brushConverter = new BrushConverter();
                SolidColorBrush selectedColorBrush = (SolidColorBrush)brushConverter.ConvertFromString(selectedColorName);
                dataManager.userOptions.OverlayLabelsFontColor = selectedColorBrush;
            }
        }

        private void ValueColor_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string? selectedColorName = ValueColor_ComboBox.SelectedValue as string;
            if (selectedColorName != null)
            {
                BrushConverter brushConverter = new BrushConverter();
                SolidColorBrush selectedColorBrush = (SolidColorBrush)brushConverter.ConvertFromString(selectedColorName);
                dataManager.userOptions.OverlayValuesFontColor = selectedColorBrush;
            }
        }

        private void FontWeight_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FontWeight_ComboBox.SelectedItem is TranslatableComboBoxItem selectedItem)
            {
                string fontWeightString = selectedItem.Tag?.ToLower();

                FontWeight selectedFontWeight = fontWeightString switch
                {
                    "light" => FontWeights.Light,
                    "normal" => FontWeights.Normal,
                    "bold" => FontWeights.Bold,
                    "extrabold" => FontWeights.ExtraBold,
                    "heavy" => FontWeights.Heavy,
                    _ => FontWeights.Normal,
                };
                dataManager.userOptions.OverlayFontWeight = selectedFontWeight;
            }
        }

        public class TranslatableComboBoxItem
        {
            public TranslatableComboBoxItem()
            {

            }

            public TranslatableComboBoxItem(string _Content, string _Tag)
            {
                Content = _Content;
                Tag = _Tag;
            }

            public string Content {  get; set; }
            public string Tag { get; set; }
        }

        private void AudioAlert_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string? selectedSound = AudioAlert_ComboBox.SelectedValue as string;
            if (selectedSound == null)
            {
                return;
            }
            dataManager.userOptions.AudioAlertFile = selectedSound;
            selectedSound = "Sounds\\" + selectedSound + ".wav";
            if (overlayWindow != null)
            {
                overlayWindow.AudioAlertFilePath = selectedSound;
                
            }

        }

        private void AudioAlertPlay_Button_Click(object sender, RoutedEventArgs e)
        {
            overlayWindow?.PlayAudioAlert();
        }
    }
}
