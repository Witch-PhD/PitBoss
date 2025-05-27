using System.Windows.Controls;

namespace DakkaDataLink.UserControls
{
    /// <summary>
    /// Interaction logic for GunBattery_UserControl.xaml
    /// </summary>
    public partial class GunBattery_UserControl : UserControl
    {
        public GunBattery_UserControl()
        {
            dataManager = DisplayManager.Instance;
            InitializeComponent();
            
            DataContext = dataManager;
            PreviousCoords_DataGrid.ItemsSource = dataManager.PreviousCoords;
        }
        DisplayManager dataManager;

        

        
    }
}
