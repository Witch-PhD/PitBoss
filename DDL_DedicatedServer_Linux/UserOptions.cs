using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Xml.Serialization;

namespace DakkaDataLink_Dedicated
{
    public class UserOptions : INotifyPropertyChanged
    {
        public UserOptions()
        {

        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        protected string m_MyCallSign = "";
        public string MyCallSign
        {
            get
            {
                return m_MyCallSign;
            }
            set
            {
                if (m_MyCallSign != value)
                {
                    m_MyCallSign = value;
                    m_MyCallSign = m_MyCallSign.Replace(" (Spotter)", "");
                    m_MyCallSign = m_MyCallSign.Replace(" (Gunner)", "");
                    OnPropertyChanged();
                }
            }
        }


#region Logger
        protected string m_LoggerFilePath = "./Log.txt";
        public string LoggerFilePath
        {
            get
            {
                return m_LoggerFilePath;
            }
            set
            {
                m_LoggerFilePath = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Language
        protected string m_Language = "English";
        public string Language
        {
            get
            {
                return m_Language;
            }
            set
            {
                m_Language = value;
                OnPropertyChanged();
            }
        }

        protected bool m_SaveSelectedLanguage = false;
        public bool SaveSelectedLanguage
        {
            get
            {
                return m_SaveSelectedLanguage;
            }
            set
            {
                m_SaveSelectedLanguage = value;
                OnPropertyChanged();
            }
        }
        #endregion
    }
}
