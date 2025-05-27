using Comms_Core;
using Google.Protobuf;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;

namespace DakkaDataLink
{
    internal class UdpServerHandler
    {
        private DisplayManager displayManager = DisplayManager.Instance;
        public UdpClient? udpClient;// = new UdpClient(DdlConstants.SERVER_PORT);
        private Thread? m_udpReceiveThread;

        private bool m_RunReceivingTask = false;
        private CancellationTokenSource? receiveTaskCancelSource;
        private System.Timers.Timer sendStatusTimer = new System.Timers.Timer();

        //private Dictionary<IPEndPoint, UdpHandler.RemoteUserEntry> m_RemoteUserEntries = new Dictionary<IPEndPoint, UdpHandler.RemoteUserEntry>();
        private Dictionary<string, ServerSession?> m_SessionIdMap = new Dictionary<string, ServerSession?>();
        private static UdpServerHandler? m_Instance;

        //private int latestCoordsMsgIdSent = 0;
        //private int latestCoordsMsgIdRecvd = 0;

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
            SendServerReports();
        }

        public void Start()
        {
            if (m_udpReceiveThread == null)
            {
                //IPAddress[] addresses =  Dns.GetHostAddresses("vitchdebitch.hopto.org");
                //Console.WriteLine($"UdpServerHandler starting...");
                GlobalLogger.Log($"UdpServerHandler starting...");
                m_SessionIdMap[displayManager.userOptions.LastSessionId] = new ServerSession(displayManager.userOptions.LastSessionId, displayManager.userOptions.LastSessionPassword);
                udpClient = new UdpClient(DdlConstants.SERVER_PORT);
                receiveTaskCancelSource = new CancellationTokenSource();
                displayManager.UdpHandlerActive = true;
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
            displayManager.UdpHandlerActive = false; // TODO: Move this somewhere else.
            m_udpReceiveThread = null; // TODO: move this up a block or two?
            receiveTaskCancelSource = null;
            udpClient?.Client?.Close();
            udpClient?.Close();
            m_SessionIdMap.Clear();
            //latestCoordsMsgIdSent = 0;
            //latestCoordsMsgIdRecvd = 0;
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
            ServerSession? theSession;
            if (!m_SessionIdMap.ContainsKey(theMsg.SessionId))
            {
                m_SessionIdMap[theMsg.SessionId] = new ServerSession(theMsg.SessionId, theMsg.ClientReport.SessionPassword);
            }
            theSession = m_SessionIdMap[theMsg.SessionId];
            //m_RemoteUserEntries[remoteEndPoint].Update(theMsg);
            if (theMsg.Coords != null)
            {
                if (theSession == null)
                {
                    GlobalLogger.Log($"UdpServerHandler.processMsg() Coords - SessionId {theMsg.SessionId} not found.");
                    return;
                }
                if (!theSession.IsUserInSession(remoteEndPoint))
                {
                    return;
                }

                SendCoordsAck(theMsg.Coords.MsgId, remoteEndPoint);

                theSession.LatestCoordsMsgIdReceived = theMsg.Coords.MsgId;
                //latestCoordsMsgIdRecvd = theMsg.Coords.MsgId;
                if (displayManager.userOptions.LastSessionId == theMsg.SessionId)
                {
                    displayManager.NewArtyMsgReceived(theMsg);
                }
                
                if (displayManager.OperatingMode == DisplayManager.ProgramOperatingMode.eGunner)
                {
                    SendCoordsToAllInSession(theMsg);
                }
            }
            else if (theMsg.ClientReport != null) // Received by the server.
            {
                if (!theSession.IsUserInSession(remoteEndPoint))
                {
                    UdpHandler.RemoteUserEntry userEntry = new UdpHandler.RemoteUserEntry(remoteEndPoint, theMsg.Callsign);
                    userEntry.Update(theMsg);
                    //userEntry.LastClientReport = theMsg.ClientReport;
                    bool userValidated = theSession.ValidateAndAddUser(userEntry);
                    if (!userValidated)
                    {
                        SendPasswordRefused(remoteEndPoint);
                        GlobalLogger.Log($"UdpServerHandler.processMsg() ClientReport - User {theMsg.Callsign} not validated for session {theMsg.SessionId}. Ignoring.");
                        return;
                    }
                }
                else
                {
                    UdpHandler.RemoteUserEntry userEntry = theSession.ActiveUserEntries[remoteEndPoint];
                    userEntry.Update(theMsg);
                }

                //GlobalLogger.Log($"New [ClientStatus] received from CallSign: {theMsg.Callsign} Type: {theMsg.ClientReport.ClientType}, LastCoordsIdRecvd: {theMsg.ClientReport.LastCoordsIdReceived}, LastCoordsIdSent: {theMsg.ClientReport.LastCoordsIdSent}");
                // If this is a gunner client, and they did not receive the latest coords...
                if ((theMsg.ClientReport.LastCoordsIdReceived != theSession.LatestCoordsMsgIdSent) && (theMsg.ClientReport.ClientType == 2))
                {
                    GlobalLogger.Log($"UdpServerHandler.resendCoordsToClient -> {theMsg.Callsign}. Client last received msgId: {theMsg.ClientReport.LastCoordsIdReceived}, Server last sent msgId: {theSession.LatestCoordsMsgIdSent} ");
                    resendCoordsToClient(theSession, remoteEndPoint);
                }
                //Console.WriteLine($"UdpServerHandler.processMsg() [ClientStatus] CallSign: {theMsg.Callsign} Type: {theMsg.ClientReport.ClientType}");
                
            }
        }

        public void SendPasswordRefused(IPEndPoint remoteEndPoint)
        {
            ArtyMsg passwordRefusedMsg = new ArtyMsg();
            passwordRefusedMsg.ServerCommand  = new ServerCommand();
            passwordRefusedMsg.ServerCommand.CommandType = 1;
            byte[] rawData = passwordRefusedMsg.ToByteArray();
            int dataLength = rawData.Length;
            udpClient.SendAsync(rawData, dataLength, remoteEndPoint);
        }

        public void SendCoordsAck(int coordsId, IPEndPoint remoteEndPoint)
        {
            ArtyMsg ackMsg = new ArtyMsg();
            ackMsg.Ack = new AckMsgId();
            ackMsg.Callsign = displayManager.GetMyDisplayableCallsign();
            ackMsg.Ack.MsgId = coordsId;

            byte[] rawData = ackMsg.ToByteArray();
            int dataLength = rawData.Length;
            udpClient.SendAsync(rawData, dataLength, remoteEndPoint);
        }

        public void SendCoordsToAllInSession(ArtyMsg msg)
        {
            if (msg.Coords == null)
            {
                GlobalLogger.Log($"*** UdpServerHandler.SendCoordsToAllInSession null parameter");
                return;
            }
            try
            {
                ServerSession? theSession = m_SessionIdMap[msg.SessionId];
                if (theSession == null)
                {
                    GlobalLogger.Log($"UdpServerHandler.SendCoordsToAllInSession SessionId {msg.SessionId} not found.");
                    return;
                }

                if (displayManager.OperatingMode == DisplayManager.ProgramOperatingMode.eSpotter)
                {
                    theSession.LatestCoordsMsgIdSent++;
                }
                msg.Coords.MsgId = theSession.LatestCoordsMsgIdSent;
                GlobalLogger.Log($"UdpServerHandler sending new ArtyMsg: CallSign: {msg.Callsign} Az: {msg.Coords.Az}, Dist: {msg.Coords.Dist}, MsgId: {msg.Coords.MsgId}");
                byte[] rawData = msg.ToByteArray();
                int dataLength = rawData.Length;
                //Stopwatch stopwatch = Stopwatch.StartNew();
                foreach (IPEndPoint endPoint in theSession.ActiveUserEntries.Keys)
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

        private void SendServerReports()
        {
            removeTimedOutUsers();

            foreach (ServerSession session in m_SessionIdMap.Values)
            {
                ArtyMsg msg = new ArtyMsg();
                string myDisplayableCallsign = displayManager.GetMyDisplayableCallsign();
                msg.Callsign = myDisplayableCallsign;
                msg.ServerReport = new ServerReport();
                msg.ServerReport.LastCoordsIdReceived = session.LatestCoordsMsgIdReceived;
                msg.ServerReport.LastCoordsIdSent = session.LatestCoordsMsgIdSent;

                msg.ServerReport.ActiveCallsigns.Add(myDisplayableCallsign + " (Server)");
                foreach (UdpHandler.RemoteUserEntry users in session.ActiveUserEntries.Values)
                {
                    msg.ServerReport.ActiveCallsigns.Add(users.CallSign);
                }
                if (session.SessionId == displayManager.userOptions.LastSessionId)
                {
                    displayManager.UpdateConnectedUsers(msg.ServerReport.ActiveCallsigns.ToList<string>());
                }

                try
                {
                    byte[] rawData = msg.ToByteArray();
                    int dataLength = rawData.Length;
                    //Stopwatch stopwatch = Stopwatch.StartNew();
                    foreach (IPEndPoint endPoint in session.ActiveUserEntries.Keys)
                    {
                        udpClient.SendAsync(rawData, dataLength, endPoint);
                    }
                    //stopwatch.Stop();
                    ////Console.WriteLine($"UdpServerHandler.SendServerReports sent to {m_RemoteUserEntries.Keys.Count} clients in {stopwatch.ElapsedMilliseconds} milliseconds");
                    //GlobalLogger.Log($"UdpServerHandler.SendServerReports sent to {m_RemoteUserEntries.Keys.Count} clients in {stopwatch.ElapsedMilliseconds} milliseconds");

                }
                catch (Exception ex)
                {
                    //Console.WriteLine($"*** UdpServerHandler.SendServerReports Other Exception: {ex.Message}");
                    GlobalLogger.Log($"*** UdpServerHandler.SendServerReports Other Exception: {ex.Message}");
                }
            }
        }

        private void resendCoordsToClient(ServerSession session, IPEndPoint endPoint)
        {
            //Console.WriteLine($"UdpServerHandler.resendCoordsToClient -> {endPoint}");

            ArtyMsg artyMsg = new ArtyMsg();
            artyMsg.Coords = new Coords();
            artyMsg.Coords.MsgId = session.LatestCoordsMsgIdReceived;
            artyMsg.Coords.Az = session.LatestAz;
            artyMsg.Coords.Dist = session.LatestDist;
            artyMsg.Callsign = displayManager.GetMyDisplayableCallsign();

            byte[] rawData = artyMsg.ToByteArray();
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
            foreach (ServerSession session in m_SessionIdMap.Values)
            {
                List<IPEndPoint> usersToRemove = new List<IPEndPoint>();
                List<string> userCallsignsToUpdate = new List<string>();
                foreach (KeyValuePair<IPEndPoint, UdpHandler.RemoteUserEntry> activeUserEntry in session.ActiveUserEntries)
                {
                    TimeSpan timeSinceLastSeen = DateTime.Now - activeUserEntry.Value.TimeLastSeen;
                    if ((activeUserEntry.Value.CanTimeOut) && (timeSinceLastSeen.TotalMilliseconds > DdlConstants.REMOTE_USER_TIMEOUT_MILLISECONDS))
                    {
                        usersToRemove.Add(activeUserEntry.Key);
                        if (session.SessionId == displayManager.userOptions.LastSessionId)
                        {
                            displayManager.ConnectedUsersCallsigns.Remove(activeUserEntry.Value.CallSign);
                        }
                        //Console.WriteLine($"UdpServerHandler {activeUserEntry.Value.CallSign} ({activeUserEntry.Key}) timed out, removing from active users list.");
                        GlobalLogger.Log($"UdpServerHandler {activeUserEntry.Value.CallSign} ({activeUserEntry.Key}) timed out.");
                    }
                    else
                    {
                        //userCallsignsToUpdate.Add(activeUserEntry.Value.CallSign);
                    }
                }
                foreach (IPEndPoint timedOutUser in usersToRemove)
                {
                    session.ActiveUserEntries.Remove(timedOutUser);
                }
                if (usersToRemove.Count > 0)
                {
                    GlobalLogger.Log($"{session.ActiveUserEntries.Count} active users in session {session.SessionId}");
                }
                
                //displayManager.UpdateConnectedUsers(userCallsignsToUpdate);
            }
        }





        
    }
}
