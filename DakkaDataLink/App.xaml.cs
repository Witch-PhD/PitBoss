using System.Globalization;
using System.Windows;
using System.Windows.Markup;

namespace DakkaDataLink
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        MainWindow? mainWindow;

        private static GlobalLogger logger = GlobalLogger.Instance;
        public static KeyboardListener KListener = new KeyboardListener();
        private static SpotterKeystrokeHandler keystrokeHandler = SpotterKeystrokeHandler.Instance;
        DisplayManager dataManager = DisplayManager.Instance;
        private static RawKeyEventHandler? keyDownHandler;
        private static RawKeyEventHandler? keyUpHandler;
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

            //this.Dispatcher.UnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnDispatcherUnhandledException;

            DateTime startTime = DateTime.Now;
            //GlobalLogger.Log($"{startTime.Year}-{startTime.Month}-{startTime.Day} ({startTime.Hour}:{startTime.Minute}:{startTime.Second}.{startTime.Millisecond}) Application_Startup entered", false);
            keyDownHandler = new RawKeyEventHandler(KListener_KeyDown);
            keyUpHandler = new RawKeyEventHandler(KListener_KeyUp);
            KListener.KeyDown += keyDownHandler;
            KListener.KeyUp += keyUpHandler;
            mainWindow = new MainWindow();
            mainWindow.Show();
        }

        void OnDispatcherUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            GlobalLogger.Log($"*** Global unhandled exception: {ex.Message}\nStack Trace: {ex.StackTrace}");
            GlobalLogger.Shutdown();
            MessageBox.Show("Unhandled exception occurred: \n" + ex.Message + "\nStack Trace: \n" + ex.StackTrace, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            
        }

        static void KListener_KeyDown(object sender, RawKeyEventArgs args)
        {
            SpotterKeystrokeHandler.Instance.handleKeyDown(args);
            
            // I tried writing the data in file here also, to make sure the problem is not in //Console.WriteLine
        }

        static void KListener_KeyUp(object sender, RawKeyEventArgs args)
        {
            //if (!args.IsSysKey) // We only care about ctrl or shift.
            //{
            //    return;
            //}
            SpotterKeystrokeHandler.Instance.handleKeyUp(args);
            ////Console.WriteLine(args.Key.ToString());
            // I tried writing the data in file here also, to make sure the problem is not in //Console.WriteLine
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            //GlobalLogger.Log("Application exiting.");
            KListener.Dispose();
            //GlobalLogger.Shutdown();
        }

    }

}
