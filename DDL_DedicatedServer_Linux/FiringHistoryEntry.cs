using Comms_Core;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DakkaDataLink_Dedicated
{
    public class FiringHistoryEntry : INotifyPropertyChanged
    {
        public FiringHistoryEntry()
        {
            DateTime now = DateTime.Now;
            string nowHour = string.Format("{0:D2}", now.Hour);
            string nowMinute = string.Format("{0:D2}", now.Minute);
            string nowSecond = string.Format("{0:D2}", now.Second);
            TimeStampHourMinSec = $"{nowHour}:{nowMinute}:{nowSecond}"; 
        }

        public FiringHistoryEntry(ArtyMsg artyMsg)
        {
            Dist = artyMsg.Coords.Dist;
            Az = artyMsg.Coords.Az;
        }

        private double m_Az = 0.0;
        public double Az
        {
            get
            {
                return m_Az;
            }
            set
            {
                m_Az = value;
                OnPropertyChanged();
            }
        }

        private double m_Dist = 0.0;
        public double Dist
        {
            get
            {
                return m_Dist;
            }
            set
            {
                m_Dist = value;
                OnPropertyChanged();
            }
        }

        private string m_TimeStampHourMinSec;
        public string TimeStampHourMinSec
        {
            get
            {
                return m_TimeStampHourMinSec;
            }
            set
            {
                m_TimeStampHourMinSec = value;
                OnPropertyChanged();
            }
        }

        private string m_EntryName = "";
        public string EntryName
        {
            get
            {
                return this.m_EntryName;
            }
            set
            {
                m_EntryName = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
