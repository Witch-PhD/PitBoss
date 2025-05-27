using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace DakkaDataLink.UserControls
{
    /// <summary>
    /// Interaction logic for Overlay_Window.xaml
    /// </summary>
    public partial class Overlay_Window : Window, INotifyPropertyChanged
    {
        DisplayManager dataManager;
        public Overlay_Window()
        {
            dataManager = DisplayManager.Instance;
            InitializeComponent();
            m_MediaPlayer = new MediaPlayer();
            //DataContext = DisplayManager.Instance;
            this.SizeToContent = SizeToContent.WidthAndHeight;
            AzLabel_TextBlock.DataContext = dataManager.userOptions;
            DistLabel_TextBlock.DataContext = dataManager.userOptions;
            MainBorder.DataContext = this;

            AzValue_TextBlock.DataContext = dataManager;
            DistValue_TextBlock.DataContext = dataManager;
            
            m_CurrentBorderColor = Brushes.Transparent;
            Topmost = true;

            flashTimer = new System.Windows.Threading.DispatcherTimer();
            flashTimer.Tick += flashTask;
            flashTimer.Interval = new TimeSpan(0, 0, 0, 0, 350);
        }
        private MediaPlayer m_MediaPlayer;
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private SolidColorBrush m_CurrentBorderColor;
        public SolidColorBrush CurrentBorderColor
        {
            get
            {
                return m_CurrentBorderColor;
            }
            set
            {
                if (m_CurrentBorderColor != value)
                {
                    m_CurrentBorderColor = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Not used yet. To be used for user customization of alert flash.
        /// TODO: Plug this into a user selection in the Options tab.
        /// </summary>
        



        private bool m_UseFlashAlert = true;
        public bool UseFlashAlert
        {
            get
            {
                return m_UseFlashAlert;
            }
            set
            {
                if (m_UseFlashAlert != value)
                {
                    m_UseFlashAlert = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool m_UseAudioAlert = true;
        public bool UseAudioAlert
        {
            get
            {
                return m_UseAudioAlert;
            }
            set
            {
                if (m_UseAudioAlert != value)
                {
                    m_UseAudioAlert = value;
                    OnPropertyChanged();
                }
            }
        }

        private string m_AudioAlertFilePath = "";
        public string AudioAlertFilePath
        {
            get
            {
                return m_AudioAlertFilePath;
            }
            set
            {
                m_AudioAlertFilePath = Environment.CurrentDirectory + "\\" + value;
                if (m_AudioAlertFilePath != value)
                {
                    OnPropertyChanged();
                }
            }
        }

        private double m_AudioAlertVolume = 0.5;
        public double AudioAlertVolume
        {
            get
            {
                return m_AudioAlertVolume;
            }
            set
            {
                if (m_AudioAlertVolume != value)
                {
                    m_AudioAlertVolume = value;
                    //m_MediaPlayer.Volume = m_AudioAlertVolume;
                    OnPropertyChanged();
                }
            }
        }

        private System.Windows.Threading.DispatcherTimer flashTimer;
        private byte flashIteration = 1;
        private byte flashIterationToComplete = 6; // One iteration does one color change. Full cycle must be a multiple of 2.
        internal void FlashOverlay()
        {
            //Thickness originalThickness = borderThickness;
            //borderThickness = new Thickness(3, 3, 3, 3);
            //Thread.Sleep(400);
            //borderThickness = originalThickness;

            if (flashTimer.IsEnabled == false)
            {
                flashTimer.IsEnabled = true;
            }
        }

        
        internal void PlayAudioAlert()
        {
            if (m_UseAudioAlert)
            {
                //AudioAlertFilePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DakkaDataLink/Sounds/Defcon.wav");
                //AudioAlertFilePath = "C:/Users/Witch Doctor/source/repos/PitBoss/DakkaDataLink/Resources/Sounds/Defcon.wav";
                //m_MediaPlayer.Open(new Uri(m_AudioAlertFilePath));
                //m_MediaPlayer.Open(new Uri(AudioAlertFilePath));
                m_MediaPlayer = new MediaPlayer();
                m_MediaPlayer.Open(new Uri(m_AudioAlertFilePath));
                m_MediaPlayer.Volume = AudioAlertVolume;
                m_MediaPlayer.Play();
            }
        }

        private void flashTask(object sender, EventArgs e)
        {
            
            if (flashIteration % 2 == 0)
            {
                CurrentBorderColor = Brushes.Transparent;
            }
            else
            {
                CurrentBorderColor = (SolidColorBrush)dataManager.userOptions.OverlayAlertBorderColor;
            }
            if (flashIteration == flashIterationToComplete)
            {
                flashTimer.IsEnabled = false;
                flashIteration = 1;
                return;
            }
            flashIteration++;
        }

        private void MainBorder_MouseLeftButtonDown(Object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
