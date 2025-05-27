using Comms_Core;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace DakkaDataLink_Dedicated
{
    public class DataManager : INotifyPropertyChanged
    {
        private static DataManager? m_Instance;
        public static DataManager Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new DataManager();
                    m_Instance.udpHandler = UdpHandler.Instance;
                    m_Instance.m_ArtyProfiles = ArtilleryProfiles.Instance;

                    //m_Instance.ConnectedGunsCallsigns = new ObservableCollection<string>();
                }
                return m_Instance;
            }
            private set { }
        }

        
        private DataManager()
        {
            m_userOptions = new UserOptions();
            
        }

        private UdpHandler udpHandler;
        
        private ArtilleryProfiles m_ArtyProfiles;




        private readonly object PrevCoordsCollectionLock = new object();
        public ObservableCollection<FiringHistoryEntry> PreviousCoords = new ObservableCollection<FiringHistoryEntry>();
        public ObservableCollection<FiringHistoryEntry> SavedCoords = new ObservableCollection<FiringHistoryEntry>();
        private readonly object connectedUsersCollectionLock = new object();
        public ObservableCollection<string> ConnectedUsersCallsigns = new ObservableCollection<string>();

        private UserOptions m_userOptions;
        public UserOptions userOptions
        {
            get
            {
                return m_userOptions;
            }
            set
            {
                if (m_userOptions != value)
                {
                    m_userOptions = value;
                    MyCallsign = m_userOptions.MyCallSign;
                    OnPropertyChanged();
                }
            }
        }

        
        public enum ProgramOperatingMode
        {
            eIdle,
            eSpotter,
            eGunner
        }
        private ProgramOperatingMode m_operatingMode;
        public ProgramOperatingMode OperatingMode
        {
            get
            { return m_operatingMode; }
            set
            {
                if (value != m_operatingMode)
                {
                    m_operatingMode = value;
                    OnPropertyChanged();
                }
            }
        }

        private string m_MyCallsign = "None";
        public string MyCallsign
        {
            get
            {
                return m_MyCallsign;
            }
            set
            {
                if (m_MyCallsign != value)
                {
                    m_MyCallsign = value;
#if !DEDICATED_SERVER
                    userOptions.MyCallSign = MyCallsign; // So it can be saved.
#endif
                    OnPropertyChanged();
                }
            }
        }



        private double m_LatestAz = 0.0;
        public double LatestAz
        {
            get
            {
                return m_LatestAz;
            }
            set
            {
                m_LatestAz = value;
                if (m_LatestAz >= 360.0)
                {
                    while (m_LatestAz >= 360.0)
                    {
                        m_LatestAz -= 360.0;
                    }
                }
                else if (m_LatestAz < 0.0)
                {
                    while (m_LatestAz < 0.0)
                    {
                        m_LatestAz += 360.0;
                    }
                }
                OnPropertyChanged();
            }
        }


        private double m_LatestDist = 100.0;
        public double LatestDist
        {
            get
            {
                return m_LatestDist;
            }
            set
            {
                if (OperatingMode == ProgramOperatingMode.eSpotter)
                {
                    // Order of these checks is important.
                    if (value < m_ArtyProfiles.CurrentProfile.MinDist)
                    {
                        m_LatestDist = m_ArtyProfiles.CurrentProfile.MinDist;
                    }
                    else if (value <= m_ArtyProfiles.CurrentProfile.MaxDist)
                    {
                        m_LatestDist = value;
                    }
                    else
                    {
                        m_LatestDist = m_ArtyProfiles.CurrentProfile.MaxDist;
                    }
                }
                else
                {
                    m_LatestDist = value;
                }
                
                OnPropertyChanged();
            }
        }

        private bool m_UdpHandlerActive = false;
        public bool UdpHandlerActive
        {
            get
            {
                return m_UdpHandlerActive;
            }
            set
            {
                if (value != m_UdpHandlerActive)
                {
                    m_UdpHandlerActive = value;

                    OnPropertyChanged();
                }
            }
        }

        private int m_ServerActiveListeningPort = 50082;
        public int ServerActiveListeningPort
        {
            get
            {
                return m_ServerActiveListeningPort;
            }
            set
            {
                if (value != m_ServerActiveListeningPort)
                {
                    m_ServerActiveListeningPort = value;

                    OnPropertyChanged();
                }
            }
        }

        private int m_ConnectedClients = 0;
        public int ConnectedClients
        {
            get
            {
                return m_ConnectedClients;
            }
            set
            {
                if (m_ConnectedClients != value)
                {
                    m_ConnectedClients = value;
                    OnPropertyChanged();
                }
            }
        }



        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public ArtyMsg getAssembledCoords()
        {
            ArtyMsg artyMsg = new ArtyMsg();
            artyMsg.Coords = new Coords();
            artyMsg.Coords.Az = LatestAz;
            artyMsg.Coords.Dist = LatestDist;
            artyMsg.Callsign = MyCallsign;

            return artyMsg;
        }

        public void unpackIncomingCoords(ArtyMsg msg)
        {
            LatestAz = msg.Coords.Az;
            LatestDist = msg.Coords.Dist;
        }

        public void StartUdpServer()
        {
            m_operatingMode = ProgramOperatingMode.eGunner; // Force this dedicated server to be a relay by telling it it is a gunner.
            MyCallsign = "DDL";
            ConnectedUsersCallsigns.Add(MyCallsign);
            udpHandler.Start();
        }

        public void StopUdp()
        {
            if (m_UdpHandlerActive)
            {
                udpHandler.Stop();
            }
            MyCallsign = MyCallsign.Replace(" (Spotter)", "");
            MyCallsign = MyCallsign.Replace(" (Gunner)", "");
            ConnectedUsersCallsigns.Clear();
            ConnectedClients = 0;
        }

        public void StartUdpClient(string ipAddress)
        {
            MyCallsign = MyCallsign + (m_operatingMode == ProgramOperatingMode.eSpotter ? " (Spotter)" : " (Gunner)");
            ConnectedUsersCallsigns.Add(MyCallsign);
            IPAddress ip = IPAddress.Parse(ipAddress);
            udpHandler.Start(ip);
        }

        public void StopUdpClient()
        {
            udpHandler.Stop();
            ConnectedUsersCallsigns.Clear();
            ConnectedClients = 0;
        }

        public void SendCoords()
        {
            ArtyMsg artyMsg = getAssembledCoords();
            if ((OperatingMode == ProgramOperatingMode.eSpotter) && udpHandler.RunningAsServer)
            {
                addPrevCoordsEntry(artyMsg);
            }
            udpHandler.SendCoords(artyMsg);
        }

        private void addPrevCoordsEntry(ArtyMsg artyMsg)
        {
            if (PreviousCoords.Count >= 20)
            {
                PreviousCoords.RemoveAt(PreviousCoords.Count - 1);
            }
            FiringHistoryEntry thisFiringEntry = new FiringHistoryEntry();
            PreviousCoords.Insert(0, thisFiringEntry);
            thisFiringEntry.Dist = artyMsg.Coords.Dist;
            thisFiringEntry.Az = artyMsg.Coords.Az;
        }

        public event EventHandler<bool> newCoordsReceived;
        public void NewArtyMsgReceived(ArtyMsg theMsg, IPEndPoint? remoteEndPoint = null)
        {
            if (theMsg.Coords != null)
            {
                unpackIncomingCoords(theMsg);
                newCoordsReceived?.Invoke(this, true);
                addPrevCoordsEntry(theMsg);
                //Console.WriteLine($"DataManager.NewArtyMsgReceived() [Coords] CallSign: {theMsg.Callsign} Az: {theMsg.Coords.Az}, Dist: {theMsg.Coords.Dist}");
                GlobalLogger.Log($"New [Coords] received from {theMsg.Callsign}, Az: {theMsg.Coords.Az}, Dist: {theMsg.Coords.Dist}, MsgId: {theMsg.Coords.MsgId}");
            }
            else
            {
                // TODO: Report warning: empty message.
            }
        }

        public void UpdateConnectedUsers(List<string> activeUsers)
        {
            ConnectedUsersCallsigns.Clear();
            foreach (string user in activeUsers)
            {
                ConnectedUsersCallsigns.Add(user);
            }
            ConnectedClients = ConnectedUsersCallsigns.Count;
        }


        public void ActivateKeyboardListener()
        {
#if !DEDICATED_SERVER
            SpotterKeystrokeHandler.Instance.Activate();
#endif
        }

        public void DeactivateKeyboardListener()
        {
#if !DEDICATED_SERVER
            SpotterKeystrokeHandler.Instance.Deactivate();
#endif
        }


    }
}
