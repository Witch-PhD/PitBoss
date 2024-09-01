﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PitBoss.UserControls
{
    /// <summary>
    /// Interaction logic for Overlay_Window.xaml
    /// </summary>
    public partial class Overlay_Window : Window, INotifyPropertyChanged
    {
        DataManager dataManager;
        public Overlay_Window()
        {
            dataManager = DataManager.Instance;
            InitializeComponent();
            //DataContext = DataManager.Instance;
            AzLabel_TextBlock.DataContext = dataManager.userOptions;
            DistLabel_TextBlock.DataContext = dataManager.userOptions;
            MainBorder.DataContext = this;

            AzValue_TextBlock.DataContext = dataManager;
            DistValue_TextBlock.DataContext = dataManager;
            
            m_CurrentBorderColor = Brushes.Transparent;
            Topmost = true;

            flashTimer = new System.Windows.Threading.DispatcherTimer();
            flashTimer.Tick += flashTask;
            flashTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
        }

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
        private SolidColorBrush m_AlertBorderColor;
        public SolidColorBrush AlertBorderColor
        {
            get
            {
                return m_AlertBorderColor;
            }
            set
            {
                if (m_AlertBorderColor != value)
                {
                    m_AlertBorderColor = value;
                    OnPropertyChanged();
                }
            }
        }

        private System.Windows.Threading.DispatcherTimer flashTimer;
        private byte flashIteration = 1;
        private byte flashIterationToComplete = 4; // One iteration does one color change. Full cycle must be a multiple of 2.
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

        private void flashTask(object sender, EventArgs e)
        {
            
            if (flashIteration % 2 == 0)
            {
                CurrentBorderColor = Brushes.Transparent;
            }
            else
            {
                CurrentBorderColor = Brushes.Red;
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
