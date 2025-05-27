using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Comms_Core;
using Google.Protobuf;
using Grpc.Core;

namespace DakkaDataLink_Dedicated
{
    internal class UdpClientHandler
    {
        private DataManager dataManager = DataManager.Instance;
        public UdpClient udpClient;// = new UdpClient(DdlConstants.SERVER_PORT);
        private Thread? m_udpReceiveThread;

        //private Dictionary<IPEndPoint, RemoteUserEntry> m_RemoteUserEntries = new Dictionary<IPEndPoint, RemoteUserEntry>();
        private IPEndPoint? m_serverIpEndPoint;
        private CancellationTokenSource? receiveTaskCancelSource;
        private System.Timers.Timer sendStatusTimer = new System.Timers.Timer();
        private bool m_RunReceivingTask = false;

        private int latestCoordsMsgIdSent = 0;
        private int latestCoordsMsgIdRecvd = 0;

        private static UdpClientHandler? m_Instance;

        private DateTime m_TimeServerLastSeen;

        public static UdpClientHandler Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new UdpClientHandler();
                    m_Instance.sendStatusTimer.Elapsed += m_Instance.sendStatusTimerElapsed;
                    m_Instance.sendStatusTimer.Interval = 1500;
                    //m_Instance.m_TimeServerLastSeen = DateTime.Now;
                }
                return m_Instance;
            }
            protected set { }
        }
        private UdpClientHandler()
        {

        }

        private void sendStatusTimerElapsed(Object source, System.Timers.ElapsedEventArgs e)
        {
            SendClientReport();
        }

        public void Start(IPAddress serverIpAddress = null)
        {
            if (m_udpReceiveThread == null)
            {
                udpClient = new UdpClient(DdlConstants.SERVER_PORT);

                m_serverIpEndPoint = new IPEndPoint(serverIpAddress, DdlConstants.SERVER_PORT);
                udpClient.Connect(m_serverIpEndPoint);
                GlobalLogger.Log($"UdpHandler starting as client...");
                m_TimeServerLastSeen = DateTime.Now;
                receiveTaskCancelSource = new CancellationTokenSource();
                dataManager.UdpHandlerActive = true;
                m_udpReceiveThread = new Thread(receivingTask);
                m_udpReceiveThread.Name = "UdpReceiveThread";
                m_udpReceiveThread.IsBackground = true;
                m_RunReceivingTask = true;
                m_udpReceiveThread.Start();
                sendStatusTimer.Start();
            }
            else
            {
                // TODO: Report warning.
            }
        }

        public void Stop()
        {
            //Console.WriteLine($"UdpHandler stopping...");
            GlobalLogger.Log($"UdpHandler stopping...");
            sendStatusTimer.Stop();
            m_RunReceivingTask = false;
            receiveTaskCancelSource?.Cancel();
            if (m_udpReceiveThread != null)
            {
                bool stoppedSuccessfully = m_udpReceiveThread.Join(3000);
                if (stoppedSuccessfully)
                {
                    // TODO: Report in log.
                }
                else
                {
                    // TODO: Report error. Retry or kill.
                }
            }
            dataManager.UdpHandlerActive = false; // TODO: Move this somewhere else.
            m_udpReceiveThread = null; // TODO: move this up a block or two?
            m_serverIpEndPoint = null; // TODO: maybe move this to the receive task?
            receiveTaskCancelSource = null;
            udpClient?.Client?.Close();
            udpClient?.Close();
            latestCoordsMsgIdSent = 0;
            latestCoordsMsgIdRecvd = 0;
        }

        private void SendClientReport()
        {
            DateTime now = DateTime.Now;
            TimeSpan timeSinceServerReport = now - m_TimeServerLastSeen;
            if (timeSinceServerReport.TotalMilliseconds > DdlConstants.REMOTE_USER_TIMEOUT_MILLISECONDS)
            {
                dataManager.UpdateConnectedUsers(new List<string>());
            }
            ArtyMsg msg = new ArtyMsg();
            msg.Callsign = dataManager.MyCallsign;
            msg.ClientReport = new ClientReport();
            msg.ClientReport.ClientType = (int)dataManager.OperatingMode;
            msg.ClientReport.SessionPassword = "";
            msg.ClientReport.LastCoordsIdReceived = latestCoordsMsgIdRecvd;
            msg.ClientReport.LastCoordsIdSent = latestCoordsMsgIdSent;

            try
            {
                byte[] rawData = msg.ToByteArray();
                int dataLength = rawData.Length;
                udpClient.SendAsync(rawData, dataLength);
                //foreach (IPEndPoint endPoint in m_RemoteUserEntries.Keys) // This should only have 1 entry (the server) if running as client.
                //{
                //    udpClient.SendAsync(rawData, dataLength, endPoint);
                //}
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"*** UdpHandler.SendClientReport Other Exception: {ex.Message}");
                GlobalLogger.Log($"*** UdpHandler.SendClientReport Other Exception: {ex.Message}");
            }
        }

        public void SendCoords(ArtyMsg msg)
        {
            if (msg.Coords == null)
            {
                // TODO: Report error.
                return;
            }
            //Console.WriteLine($"UdpHandler sending new ArtyMsg: CallSign: {msg.Callsign} Az: {msg.Coords.Az}, Dist: {msg.Coords.Dist}");

            try
            {

                latestCoordsMsgIdSent++;
                msg.Coords.MsgId = latestCoordsMsgIdSent;
                byte[] rawData = msg.ToByteArray();
                int dataLength = rawData.Length;
                //Stopwatch stopwatch = Stopwatch.StartNew();
                GlobalLogger.Log($"UdpClientHandler sending new ArtyMsg: CallSign: {msg.Callsign} Az: {msg.Coords.Az}, Dist: {msg.Coords.Dist}, MsgId: {msg.Coords.MsgId}");
                lastCoordsSendAcknowledged = false;
                udpClient.SendAsync(rawData, dataLength);
                //stopwatch.Stop();
                ////Console.WriteLine($"UdpHandler sending to {m_RemoteUserEntries.Keys.Count} clients took {stopwatch.ElapsedMilliseconds} milliseconds.");
                //GlobalLogger.Log($"UdpHandler sending to {m_RemoteUserEntries.Keys.Count} clients took {stopwatch.ElapsedMilliseconds} milliseconds.");

            }
            catch (Exception ex)
            {
                //Console.WriteLine($"*** UdpHandler.SendToAll Other Exception: {ex.Message}");
                GlobalLogger.Log($"*** UdpHandler.SendToAll Other Exception: {ex.Message}");
            }
        }

        private async void receivingTask()
        {
            //Console.WriteLine($"UdpHandler receive task starting.");
            GlobalLogger.Log($"UdpHandler receive task starting.");

            CancellationToken cancelToken = receiveTaskCancelSource.Token;

            IPEndPoint remoteEndpoint;
            while (m_RunReceivingTask)
            {
                try
                {
                    UdpReceiveResult result = await udpClient.ReceiveAsync(cancelToken);
                    remoteEndpoint = result.RemoteEndPoint;
                    ArtyMsg theMsg = ArtyMsg.Parser.ParseFrom(result.Buffer);

                    // TODO: Update endpoint's timeout timer.
                    processMsg(theMsg, remoteEndpoint);
                }
                catch (InvalidProtocolBufferException ex)
                {
                    // TODO: Report malformed data.
                }
                catch (OperationCanceledException ex)
                {
                    // Do nothing. This is supposed to happen when the cancelToken is cancelled during Stop().
                }
                //Thread.Yield();
            }

            //Console.WriteLine($"UdpHandler receive task stopped.");
            GlobalLogger.Log($"UdpHandler receive task stopped.");
        }

        bool lastCoordsSendAcknowledged = true;
        private void processMsg(ArtyMsg theMsg, IPEndPoint remoteEndPoint)
        {
            if (theMsg.Coords != null)
            {
                latestCoordsMsgIdRecvd = theMsg.Coords.MsgId;
                dataManager.NewArtyMsgReceived(theMsg);
            }
            if (theMsg.Ack != null)
            {
                if (theMsg.Ack.MsgId == latestCoordsMsgIdSent)
                {
                    lastCoordsSendAcknowledged = true;
                }
            }
            else if (theMsg.ServerReport != null) // Received by a client.
            {
                m_TimeServerLastSeen = DateTime.Now;
                List<string> connectedUsersList = theMsg.ServerReport.ActiveCallsigns.ToList<string>();
                dataManager.UpdateConnectedUsers(connectedUsersList);

                //Console.WriteLine($"UdpHandler.processMsg() [ServerStatus] CallSign: {theMsg.Callsign} ActiveCallsigns: {theMsg.ServerReport.ActiveCallsigns}");
                //GlobalLogger.Log($"New [ServerStatus] from CallSign: {theMsg.Callsign}, LastCoordsIdRecvd: {theMsg.ServerReport.LastCoordsIdReceived}, LastCoordsIdSent: {theMsg.ServerReport.LastCoordsIdSent}, ActiveCallsigns: {theMsg.ServerReport.ActiveCallsigns}");

                if ((lastCoordsSendAcknowledged == false) && (dataManager.OperatingMode == DataManager.ProgramOperatingMode.eSpotter))
                {
                    GlobalLogger.Log($"UdpClientHandler resending coords to server -> {m_serverIpEndPoint}");
                    ArtyMsg msg = dataManager.getAssembledCoords();
                    msg.Coords.MsgId = latestCoordsMsgIdSent;
                    byte[] rawData = msg.ToByteArray();
                    int dataLength = rawData.Length;
                    try
                    {
                        udpClient.SendAsync(rawData, dataLength);
                    }
                    catch (Exception ex)
                    {
                        //Console.WriteLine($"*** UdpServerHandler.resendCoordsToClient Other Exception: {ex.Message}");
                        GlobalLogger.Log($"*** UdpClientHandler resend coords Other Exception: {ex.Message}");
                    }
                }
            }
        }
    }
}
