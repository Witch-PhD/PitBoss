using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Comms_Core;
using Grpc.Core;
using System.Data;
using System.Diagnostics;

namespace DakkaDataLink_Dedicated
{
    class UdpHandler
    {

        private static UdpHandler? m_Instance;

        public static UdpHandler Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new UdpHandler();
                }
                return m_Instance;
            }
            protected set { }
        }
        private UdpHandler()
        {

        }
        private DataManager dataManager = DataManager.Instance;
        private UdpServerHandler serverHandler = UdpServerHandler.Instance;
        private UdpClientHandler clientHandler = UdpClientHandler.Instance;

        private bool m_RunningAsServer = false;

        public bool RunningAsServer
        {
            get { return m_RunningAsServer; }
            private set { }
        }

        /// <summary>
        /// Begin UDP operations.
        /// </summary>
        /// <param name="serverIpAddress">Leave this as null if running as the server, otherwise pass in the target server address.</param>
        public void Start(IPAddress? serverIpAddress = null)
        {
            if (serverIpAddress != null) // Starting as client
            {
                m_RunningAsServer = false;
                GlobalLogger.Log($"UdpHandler starting as client...");
                clientHandler.Start(serverIpAddress);
            }
            else // Starting as server
            {
                m_RunningAsServer = true;
                GlobalLogger.Log($"UdpHandler starting as server...");
                serverHandler.Start();
            }
            dataManager.UdpHandlerActive = true;
        }

        public void Stop()
        {
            //Console.WriteLine($"UdpHandler stopping...");
            GlobalLogger.Log($"UdpHandler stopping...");
            if (m_RunningAsServer)
            {
                serverHandler.Stop();
            }
            else
            {
                clientHandler.Stop();
            }
        }

        private int latestCoordsMsgId = 0;
        public void SendCoords(ArtyMsg msg)
        {
            if (msg.Coords == null)
            {
                // TODO: Report error.
                return;
            }

            if (m_RunningAsServer)
            {
                serverHandler.SendCoordsToAll(msg);
            }
            else
            {
                clientHandler.SendCoords(msg);
            }

        }

        internal class RemoteUserEntry
        {
            internal RemoteUserEntry(IPEndPoint remoteEndPoint, string callsign)
            {
                this.RemoteEndPoint = remoteEndPoint;
                this.CallSign = callsign;
                this.LastClientReport = new ClientReport();
                this.LastServerReport = new ServerReport();
                this.TimeLastSeen = DateTime.Now;
            }

            internal void Update(ArtyMsg newestMsg)
            {
                this.CallSign = newestMsg.Callsign;
                if (newestMsg.ServerReport != null)
                {
                    LastServerReport = newestMsg.ServerReport;
                }
                else if (newestMsg.ClientReport != null)
                {
                    LastClientReport = newestMsg.ClientReport;
                }
                TimeLastSeen = DateTime.Now;
            }
            IPEndPoint RemoteEndPoint { get; set; }
            internal string CallSign { get; set; }
            internal ServerReport LastServerReport { get; set; }
            internal ClientReport LastClientReport { get; set; }
            internal DateTime TimeLastSeen { get; set; }

            internal bool CanTimeOut { get; set; } = true;
        }
    }
}
