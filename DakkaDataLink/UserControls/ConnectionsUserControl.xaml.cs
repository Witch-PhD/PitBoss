using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;

namespace DakkaDataLink.UserControls
{
    /// <summary>
    /// Interaction logic for ConnectionsUserControl.xaml
    /// </summary>
    public partial class ConnectionsUserControl : UserControl
    {
        public ConnectionsUserControl()
        {
            displayManager = DisplayManager.Instance;
            InitializeComponent();
            displayManager.userOptions.PropertyChanged += UserOptions_PropertyChanged;
            MyCallsign_Textbox.DataContext = displayManager;
            ActiveUsers_DataGrid.ItemsSource = displayManager.ConnectedUsersCallsigns;
            displayManager.sessionPasswordRefused += HandlePasswordRefused;
#if DEBUG
            serverIp_TextBox.Text = "127.0.0.1";
#endif

        }

        private void UserOptions_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            serverIp_TextBox.Text = displayManager.userOptions.LastServerIp;
        }

        private DisplayManager displayManager;

        private void HandlePasswordRefused(object? sender, bool arg)
        {
            displayManager.StopUdp();
            Dispatcher.BeginInvoke(() => passwordRefused());
        }

        private void passwordRefused()
        {
            MainWindow mainWindow = Window.GetWindow(this) as MainWindow;
            
            mainWindow.SetOperatingMode(DisplayManager.ProgramOperatingMode.eIdle);
            StartStopUdpClient_Button.SetResourceReference(ContentProperty, "button_connect");

            serverIp_TextBox.IsEnabled = true;
            gunnerMode_RadioButton.IsEnabled = true;
            spotterMode_RadioButton.IsEnabled = true;
            StartStopUdpServer_Button.IsEnabled = true;
            MyCallsign_Textbox.IsEnabled = true;

            string messageBoxText = "Session password refused.";
            string caption = "Incorrect password.";
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxImage icon = MessageBoxImage.Asterisk;
            MessageBoxResult result;

            result = MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
        }

        private void StartStopUdpClient_Button_Click(object sender, RoutedEventArgs e)
        {
            if (displayManager.UdpHandlerActive) // Stopping
            {
                displayManager.StopUdp();
                //StartStopUdpClient_Button.Content = "Connect to Server";
                StartStopUdpClient_Button.SetResourceReference(ContentProperty, "button_connect");

                MainWindow mainWindow = Window.GetWindow(this) as MainWindow;
                mainWindow.SetOperatingMode(DisplayManager.ProgramOperatingMode.eIdle);

                serverIp_TextBox.IsEnabled = true;
                gunnerMode_RadioButton.IsEnabled = true;
                spotterMode_RadioButton.IsEnabled = true;
                StartStopUdpServer_Button.IsEnabled = true;
                MyCallsign_Textbox.IsEnabled = true;
                //userIp_stackPanel.Visibility = Visibility.Hidden;
            }
            else // Starting
            {
                if (serverIp_TextBox.Text.Length == 0)
                {
                    return;
                }
                serverIp_TextBox.Text = serverIp_TextBox.Text.Trim();
                string targetIpString;
                bool validUrl = false;
                
                
                bool validIP = IPAddress.TryParse(serverIp_TextBox.Text, out _);
                if (validIP)
                {
                    targetIpString = serverIp_TextBox.Text;
                }
                else
                {
                    IPAddress[] addresses;
                    try
                    {
                        addresses = Dns.GetHostAddresses(serverIp_TextBox.Text);
                        validUrl = true;
                        targetIpString = addresses[0].ToString();
                    }
                    catch (SocketException ex)
                    {
                        MessageBox.Show(Window.GetWindow(this), "Failed to start: Invalid or unknown URL.", "", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }
                    catch (HttpRequestException ex)
                    {
                        MessageBox.Show(Window.GetWindow(this), "Failed to start: Invalid or unknown URL.", "", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }
                }

                displayManager.userOptions.LastSessionId = sessionId_TextBox.Text.Trim();
                displayManager.userOptions.LastSessionPassword = sessionPassword_TextBox.Text.Trim();

                setOperatingModes();
                displayManager.userOptions.LastServerIp = serverIp_TextBox.Text;
                displayManager.StartUdpClient(targetIpString);
                //StartStopUdpClient_Button.Content = "Disconnect from Server";
                StartStopUdpClient_Button.SetResourceReference(ContentProperty, "button_disconnect");

                StartStopUdpServer_Button.IsEnabled = false;
                serverIp_TextBox.IsEnabled = false;
                MyCallsign_Textbox.IsEnabled = false;
                //userIp_stackPanel.Visibility = Visibility.Visible;
            }
        }

        private void StartStopUdpServer_Button_Click(Object sender, RoutedEventArgs e)
        {
            if (displayManager.UdpHandlerActive) // Stopping
            {
                DisplayManager.Instance.StopUdp();
                StartStopUdpServer_Button.Content = "Start as Server";

                MainWindow mainWindow = Window.GetWindow(this) as MainWindow;
                mainWindow.SetOperatingMode(DisplayManager.ProgramOperatingMode.eIdle);

                serverIp_TextBox.IsEnabled = true;
                gunnerMode_RadioButton.IsEnabled = true;
                spotterMode_RadioButton.IsEnabled = true;
                StartStopUdpClient_Button.IsEnabled = true;
                MyCallsign_Textbox.IsEnabled = true;
                userIp_stackPanel.Visibility = Visibility.Collapsed;
                sessionId_TextBox.IsEnabled = true;
                sessionPassword_TextBox.IsEnabled = true;
            }
            else // Starting
            {
                displayManager.userOptions.LastSessionId = sessionId_TextBox.Text.Trim();
                displayManager.userOptions.LastSessionPassword = sessionPassword_TextBox.Text.Trim();
                setOperatingModes();
                bool gotIp = ShowExternalIp();
                DisplayManager.Instance.StartUdpServer();
                StartStopUdpServer_Button.Content = "Stop Server";
                if (gotIp)
                {
                    userIp_stackPanel.Visibility = Visibility.Visible;
                }
                StartStopUdpClient_Button.IsEnabled = false;
                serverIp_TextBox.IsEnabled = false;
                MyCallsign_Textbox.IsEnabled = false;
                sessionId_TextBox.IsEnabled = false;
                sessionPassword_TextBox.IsEnabled = false;
            }
            
        }

        private void setOperatingModes()
        {
            MainWindow mainWindow = Window.GetWindow(this) as MainWindow;
            DisplayManager.ProgramOperatingMode newMode;

            if ((bool)gunnerMode_RadioButton.IsChecked)
            {
                newMode = DisplayManager.ProgramOperatingMode.eGunner;
            }
            else
            {
                newMode = DisplayManager.ProgramOperatingMode.eSpotter;
            }

            gunnerMode_RadioButton.IsEnabled = false;
            spotterMode_RadioButton.IsEnabled = false;

            displayManager.OperatingMode = newMode;
            mainWindow.SetOperatingMode(newMode);

        }

        /// <summary>
        /// Show the external IP address used by this PC.
        /// </summary>
        /// <returns>True if success, False if failed.</returns>
        private bool ShowExternalIp()
        {
            bool success = true;
            try
            {
                userIp_textBox.Text = new HttpClient().GetStringAsync("https://checkip.amazonaws.com/").GetAwaiter().GetResult();
                //userIp_textBox.Text = new HttpClient().GetStringAsync("https://www.hgeay45yhshrtynfn.com/").GetAwaiter().GetResult();
            }
            catch (HttpRequestException ex)
            {
                userIp_textBox.Text = "Could not resolve.";
                success = false;
            }
            return success;
        }

        private void CopyIp_Button_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(userIp_textBox.Text);
        }
    }
}
