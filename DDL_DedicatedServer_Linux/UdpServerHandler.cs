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
    internal class UdpServerHandler
    {
        private DataManager dataManager = DataManager.Instance;
        public UdpClient? udpClient;// = new UdpClient(DdlConstants.SERVER_PORT);
        private Thread? m_udpReceiveThread;

        private bool m_RunReceivingTask = false;
        private CancellationTokenSource? receiveTaskCancelSource;
        private System.Timers.Timer sendStatusTimer = new System.Timers.Timer();

        private Dictionary<IPEndPoint, UdpHandler.RemoteUserEntry> m_RemoteUserEntries = new Dictionary<IPEndPoint, UdpHandler.RemoteUserEntry>();
        private static UdpServerHandler? m_Instance;

        private int latestCoordsMsgIdSent = 0;
        private int latestCoordsMsgIdRecvd = 0;

        public static UdpServerHandler Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new UdpServerHandler();
                    m_Instance.sendStatusTimer.Elapsed += m_Instance.sendStatusTimerElapsed;
                    m_Instance.sendStatusTimer.Interval = 1500;
                }
                return m_Instance;
            }
            protected set { }
        }

        UdpServerHandler()
        {

        }

        private void sendStatusTimerElapsed(Object source, System.Timers.ElapsedEventArgs e)
        {
            SendServerReport();
        }

        public void Start()
        {
            if (m_udpReceiveThread == null)
            {
                //IPAddress[] addresses =  Dns.GetHostAddresses("vitchdebitch.hopto.org");
                //Console.WriteLine($"UdpServerHandler starting...");
                GlobalLogger.Log($"UdpServerHandler starting...");
                udpClient = new UdpClient(DdlConstants.SERVER_PORT);
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
            GlobalLogger.Log($"UdpServerHandler stopping...");
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
            receiveTaskCancelSource = null;
            udpClient?.Client?.Close();
            udpClient?.Close();
            latestCoordsMsgIdSent = 0;
            latestCoordsMsgIdRecvd = 0;
        }

        private async void receivingTask()
        {
            //Console.WriteLine($"UdpServerHandler receive task starting.");
            GlobalLogger.Log($"UdpServerHandler receive task starting.");

            CancellationToken cancelToken = receiveTaskCancelSource.Token;

            IPEndPoint remoteEndpoint;
            while (m_RunReceivingTask)
            {
                try
                {
                    UdpReceiveResult result = await udpClient.ReceiveAsync(cancelToken);
                    remoteEndpoint = result.RemoteEndPoint;
                    ArtyMsg theMsg = ArtyMsg.Parser.ParseFrom(result.Buffer);
                    if (!m_RemoteUserEntries.Keys.Contains(remoteEndpoint))
                    {
                        m_RemoteUserEntries[remoteEndpoint] = new UdpHandler.RemoteUserEntry(remoteEndpoint, theMsg.Callsign);
                        //dataManager.ConnectedUsersCallsigns.Add(theMsg.Callsign);

                        //Console.WriteLine($"UdpServerHandler {m_RemoteUserEntries[remoteEndpoint].CallSign} ({remoteEndpoint}) is now an active user.");
                        GlobalLogger.Log($"UdpServerHandler {m_RemoteUserEntries[remoteEndpoint].CallSign} ({remoteEndpoint}) is now an active user. {m_RemoteUserEntries.Count} Users active.");

                    }
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

            //Console.WriteLine($"UdpServerHandler receive task stopped.");
            GlobalLogger.Log($"UdpServerHandler receive task stopped.");
        }

        private void processMsg(ArtyMsg theMsg, IPEndPoint remoteEndPoint)
        {
            m_RemoteUserEntries[remoteEndPoint].Update(theMsg);
            if (theMsg.Coords != null)
            {
                SendCoordsAck(theMsg.Coords.MsgId, remoteEndPoint);
                latestCoordsMsgIdRecvd = theMsg.Coords.MsgId;
                dataManager.NewArtyMsgReceived(theMsg);
                if (dataManager.OperatingMode == DataManager.ProgramOperatingMode.eGunner)
                {
                    latestCoordsMsgIdSent = latestCoordsMsgIdRecvd;
                    SendCoordsToAll(theMsg);
                }
            }
            else if (theMsg.ClientReport != null) // Received by the server.
            {
                //GlobalLogger.Log($"New [ClientStatus] received from CallSign: {theMsg.Callsign} Type: {theMsg.ClientReport.ClientType}, LastCoordsIdRecvd: {theMsg.ClientReport.LastCoordsIdReceived}, LastCoordsIdSent: {theMsg.ClientReport.LastCoordsIdSent}");
                // If this is a gunner client, and they did not receive the latest coords...
                if ((theMsg.ClientReport.LastCoordsIdReceived != latestCoordsMsgIdSent) && (theMsg.ClientReport.ClientType == 2))
                {
                    GlobalLogger.Log($"UdpServerHandler.resendCoordsToClient -> {theMsg.Callsign}. Client last received msgId: {theMsg.ClientReport.LastCoordsIdReceived}, Server last sent msgId: {latestCoordsMsgIdSent} ");
                    resendCoordsToClient(remoteEndPoint);
                }
                //Console.WriteLine($"UdpServerHandler.processMsg() [ClientStatus] CallSign: {theMsg.Callsign} Type: {theMsg.ClientReport.ClientType}");
                
            }
        }

        public void SendCoordsAck(int coordsId, IPEndPoint remoteEndPoint)
        {
            ArtyMsg ackMsg = new ArtyMsg();
            ackMsg.Ack = new AckMsgId();
            ackMsg.Callsign = dataManager.MyCallsign;
            ackMsg.Ack.MsgId = coordsId;

            byte[] rawData = ackMsg.ToByteArray();
            int dataLength = rawData.Length;
            udpClient.SendAsync(rawData, dataLength, remoteEndPoint);
        }

        public void SendCoordsToAll(ArtyMsg msg)
        {
            if (msg.Coords == null)
            {
                GlobalLogger.Log($"*** UdpServerHandler.SendToAll null parameter");
                return;
            }
            try
            {
                if (dataManager.OperatingMode == DataManager.ProgramOperatingMode.eSpotter)
                {
                    latestCoordsMsgIdSent++;
                }
                msg.Coords.MsgId = latestCoordsMsgIdSent;
                GlobalLogger.Log($"UdpServerHandler sending new ArtyMsg: CallSign: {msg.Callsign} Az: {msg.Coords.Az}, Dist: {msg.Coords.Dist}, MsgId: {msg.Coords.MsgId}");
                byte[] rawData = msg.ToByteArray();
                int dataLength = rawData.Length;
                //Stopwatch stopwatch = Stopwatch.StartNew();
                foreach (IPEndPoint endPoint in m_RemoteUserEntries.Keys)
                {
                    udpClient.SendAsync(rawData, dataLength, endPoint);
                }

                //stopwatch.Stop();
                ////Console.WriteLine($"UdpServerHandler sending to {m_RemoteUserEntries.Keys.Count} clients took {stopwatch.ElapsedMilliseconds} milliseconds.");
                //GlobalLogger.Log($"UdpServerHandler sending to {m_RemoteUserEntries.Keys.Count} clients took {stopwatch.ElapsedMilliseconds} milliseconds.");

            }
            catch (RpcException ex)
            {
                //Console.WriteLine($"*** UdpServerHandler.SendToAll RpcException: {ex.Message}");
                GlobalLogger.Log($"*** UdpServerHandler.SendToAll RpcException: {ex.Message}");
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"*** UdpServerHandler.SendToAll Other Exception: {ex.Message}");
                GlobalLogger.Log($"*** UdpServerHandler.SendToAll Other Exception: {ex.Message}");
            }
        }

        private void SendServerReport()
        {
            removeTimedOutUsers();
            ArtyMsg msg = new ArtyMsg();
            msg.Callsign = dataManager.MyCallsign;
            msg.ServerReport = new ServerReport();
            msg.ServerReport.LastCoordsIdReceived = latestCoordsMsgIdRecvd;
            msg.ServerReport.LastCoordsIdSent = latestCoordsMsgIdSent;

            msg.ServerReport.ActiveCallsigns.Add(dataManager.MyCallsign + " (Server)");
            foreach (UdpHandler.RemoteUserEntry users in m_RemoteUserEntries.Values)
            {
                msg.ServerReport.ActiveCallsigns.Add(users.CallSign);
            }
            dataManager.UpdateConnectedUsers(msg.ServerReport.ActiveCallsigns.ToList<string>());
            try
            {
                byte[] rawData = msg.ToByteArray();
                int dataLength = rawData.Length;
                //Stopwatch stopwatch = Stopwatch.StartNew();
                foreach (IPEndPoint endPoint in m_RemoteUserEntries.Keys)
                {
                    udpClient.SendAsync(rawData, dataLength, endPoint);
                }
                //stopwatch.Stop();
                ////Console.WriteLine($"UdpServerHandler.SendServerReport sent to {m_RemoteUserEntries.Keys.Count} clients in {stopwatch.ElapsedMilliseconds} milliseconds");
                //GlobalLogger.Log($"UdpServerHandler.SendServerReport sent to {m_RemoteUserEntries.Keys.Count} clients in {stopwatch.ElapsedMilliseconds} milliseconds");

            }
            catch (Exception ex)
            {
                //Console.WriteLine($"*** UdpServerHandler.SendServerReport Other Exception: {ex.Message}");
                GlobalLogger.Log($"*** UdpServerHandler.SendServerReport Other Exception: {ex.Message}");
            }
        }

        private void resendCoordsToClient(IPEndPoint endPoint)
        {
            //Console.WriteLine($"UdpServerHandler.resendCoordsToClient -> {endPoint}");
            
            ArtyMsg msg = dataManager.getAssembledCoords();
            msg.Coords.MsgId = latestCoordsMsgIdSent;
            byte[] rawData = msg.ToByteArray();
            int dataLength = rawData.Length;
            try
            {
                udpClient.SendAsync(rawData, dataLength, endPoint);
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"*** UdpServerHandler.resendCoordsToClient Other Exception: {ex.Message}");
                GlobalLogger.Log($"*** UdpServerHandler.resendCoordsToClient Other Exception: {ex.Message}");
            }
        }

        private void removeTimedOutUsers()
        {
            List<IPEndPoint> usersToRemove = new List<IPEndPoint>();
            List<string> userCallsignsToUpdate = new List<string>();
            foreach (KeyValuePair<IPEndPoint, UdpHandler.RemoteUserEntry> activeUserEntry in m_RemoteUserEntries)
            {
                TimeSpan timeSinceLastSeen = DateTime.Now - activeUserEntry.Value.TimeLastSeen;
                if ((activeUserEntry.Value.CanTimeOut) && (timeSinceLastSeen.TotalMilliseconds > DdlConstants.REMOTE_USER_TIMEOUT_MILLISECONDS))
                {
                    usersToRemove.Add(activeUserEntry.Key);
                    dataManager.ConnectedUsersCallsigns.Remove(activeUserEntry.Value.CallSign);
                    //Console.WriteLine($"UdpServerHandler {activeUserEntry.Value.CallSign} ({activeUserEntry.Key}) timed out, removing from active users list.");
                    GlobalLogger.Log($"UdpServerHandler {activeUserEntry.Value.CallSign} ({activeUserEntry.Key}) timed out.");
                }
                else
                {
                    userCallsignsToUpdate.Add(activeUserEntry.Value.CallSign);
                }
            }
            foreach (IPEndPoint timedOutUser in usersToRemove)
            {
                m_RemoteUserEntries.Remove(timedOutUser);
            }
            if (usersToRemove.Count > 0)
            {
                GlobalLogger.Log($"{m_RemoteUserEntries.Count} active users.");
            }
            //dataManager.UpdateConnectedUsers(userCallsignsToUpdate);

        }





        
    }
}
