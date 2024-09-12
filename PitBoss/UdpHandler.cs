﻿using System;
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
using System.Windows.Interop;

namespace PitBoss
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
                    m_Instance.sendStatusTimer.Elapsed += m_Instance.sendStatusTimerElapsed;
                    m_Instance.sendStatusTimer.Interval = 1500;
                }
                return m_Instance;
            }
            protected set { }
        }
        private UdpHandler()
        { 
        
        }
        private DataManager dataManager = DataManager.Instance;
        public UdpClient udpClient = new UdpClient(PitBossConstants.SERVER_PORT);
        private Thread? m_udpReceiveThread;

        private Dictionary<IPEndPoint, RemoteUserEntry> m_RemoteUserEntries = new Dictionary<IPEndPoint, RemoteUserEntry>();
        private IPEndPoint? m_serverIpEndPoint;
        private bool m_RunningAsServer = false;

        private CancellationTokenSource? receiveTaskCancelSource;
        private System.Timers.Timer sendStatusTimer = new System.Timers.Timer(); 
        /// <summary>
        /// Begin UDP operations.
        /// </summary>
        /// <param name="serverIpAddress">Leave this as null if running as the server, otherwise pass in the target server address.</param>
        public void Start(IPAddress? serverIpAddress = null)
        {
            if (m_udpReceiveThread == null)
            {
                if (serverIpAddress != null) // Starting as client
                {
                    m_serverIpEndPoint = new IPEndPoint(serverIpAddress, PitBossConstants.SERVER_PORT);
                    m_RemoteUserEntries[m_serverIpEndPoint] = new RemoteUserEntry(m_serverIpEndPoint, "Server");
                    m_RemoteUserEntries[m_serverIpEndPoint].CanTimeOut = false;
                    m_RunningAsServer = false;
                    Console.WriteLine($"UdpHandler starting as client...");
                    GlobalLogger.Log($"UdpHandler starting as client...");
                }
                else // Starting as server
                {
                    m_RunningAsServer = true;
                    Console.WriteLine($"UdpHandler starting as server...");
                    GlobalLogger.Log($"UdpHandler starting as server...");
                }
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
            Console.WriteLine($"UdpHandler stopping...");
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
            m_RemoteUserEntries.Clear();
            receiveTaskCancelSource = null;
        }


        private int latestCoordsMsgId = 0;
        public void SendCoordsToAll(ArtyMsg msg)
        {
            if (msg.Coords == null)
            {
                // TODO: Report error.
                return;
            }
            Console.WriteLine($"UdpHandler sending new ArtyMsg: CallSign: {msg.Callsign} Az: {msg.Coords.Az}, Dist: {msg.Coords.Dist}");
            GlobalLogger.Log($"UdpHandler sending new ArtyMsg: CallSign: {msg.Callsign} Az: {msg.Coords.Az}, Dist: {msg.Coords.Dist}");
            try
            {
                latestCoordsMsgId++;
                msg.Coords.MsgId = latestCoordsMsgId;
                byte[] rawData = msg.ToByteArray();
                int dataLength = rawData.Length;
                //Stopwatch stopwatch = Stopwatch.StartNew();
                foreach (IPEndPoint endPoint in m_RemoteUserEntries.Keys)
                {
                    udpClient.SendAsync(rawData, dataLength, endPoint);
                }
                //stopwatch.Stop();
                //Console.WriteLine($"UdpHandler sending to {m_RemoteUserEntries.Keys.Count} clients took {stopwatch.ElapsedMilliseconds} milliseconds.");
                //GlobalLogger.Log($"UdpHandler sending to {m_RemoteUserEntries.Keys.Count} clients took {stopwatch.ElapsedMilliseconds} milliseconds.");

            }
            catch (RpcException ex)
            {
                Console.WriteLine($"*** UdpHandler.SendToAll RpcException: {ex.Message}");
                GlobalLogger.Log($"*** UdpHandler.SendToAll RpcException: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"*** UdpHandler.SendToAll Other Exception: {ex.Message}");
                GlobalLogger.Log($"*** UdpHandler.SendToAll Other Exception: {ex.Message}");
            }
        }

        private void resendCoordsToClient(IPEndPoint endPoint)
        {
            Console.WriteLine($"UdpHandler.resendCoordsToClient -> {endPoint}");
            GlobalLogger.Log($"UdpHandler.resendCoordsToClient -> {endPoint}");
            ArtyMsg msg = dataManager.getAssembledCoords();
            byte[] rawData = msg.ToByteArray();
            int dataLength = rawData.Length;
            try
            {
                udpClient.SendAsync(rawData, dataLength, endPoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"*** UdpHandler.resendCoordsToClient Other Exception: {ex.Message}");
                GlobalLogger.Log($"*** UdpHandler.resendCoordsToClient Other Exception: {ex.Message}");
            }
        }

        private void sendStatusTimerElapsed(Object source, System.Timers.ElapsedEventArgs e)
        {
            if (m_RunningAsServer == false) // Running as client
            {
                SendClientReport();
            }
            else // Running as server
            {
                SendServerReport();
            }
        }

        private void SendClientReport()
        {
            removeTimedOutUsers();
            ArtyMsg msg = new ArtyMsg();
            msg.Callsign = dataManager.MyCallsign;
            msg.ClientReport = new ClientReport();
            msg.ClientReport.ClientType = (int)dataManager.OperatingMode;
            msg.ClientReport.SpotterPassword = "";
            msg.ClientReport.LastCoordsIdReceived = latestCoordsMsgId;

            try
            {
                byte[] rawData = msg.ToByteArray();
                int dataLength = rawData.Length;
                foreach (IPEndPoint endPoint in m_RemoteUserEntries.Keys) // This should only have 1 entry (the server) if running as client.
                {
                    udpClient.SendAsync(rawData, dataLength, endPoint);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"*** UdpHandler.SendClientReport Other Exception: {ex.Message}");
                GlobalLogger.Log($"*** UdpHandler.SendClientReport Other Exception: {ex.Message}");
            }
        }

        private List<IPEndPoint> m_ClientsNeedingCoordsResentList = new List<IPEndPoint>();
        private void SendServerReport()
        {
            removeTimedOutUsers();
            ArtyMsg msg = new ArtyMsg();
            msg.Callsign = dataManager.MyCallsign;
            msg.ServerReport = new ServerReport();
            foreach (string activeCallsign in dataManager.ConnectedUsersCallsigns)
            {
                msg.ServerReport.ActiveCallsigns.Add(activeCallsign);
            }
            try
            {
                byte[] rawData = msg.ToByteArray();
                int dataLength = rawData.Length;
                //Stopwatch stopwatch = Stopwatch.StartNew();
                foreach (IPEndPoint endPoint in m_RemoteUserEntries.Keys) // This should only have 1 entry (the server) if running as client.
                {
                    udpClient.SendAsync(rawData, dataLength, endPoint);
                }
                //stopwatch.Stop();
                //Console.WriteLine($"UdpHandler.SendServerReport sent to {m_RemoteUserEntries.Keys.Count} clients in {stopwatch.ElapsedMilliseconds} milliseconds");
                //GlobalLogger.Log($"UdpHandler.SendServerReport sent to {m_RemoteUserEntries.Keys.Count} clients in {stopwatch.ElapsedMilliseconds} milliseconds");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"*** UdpHandler.SendServerReport Other Exception: {ex.Message}");
                GlobalLogger.Log($"*** UdpHandler.SendServerReport Other Exception: {ex.Message}");
            }
        }

        private bool m_RunReceivingTask = false;
        private async void receivingTask()
        {
            Console.WriteLine($"UdpHandler receive task starting.");
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
                    if (!m_RemoteUserEntries.Keys.Contains(remoteEndpoint))
                    {
                        m_RemoteUserEntries[remoteEndpoint] = new RemoteUserEntry(remoteEndpoint, theMsg.Callsign);
                        //dataManager.ConnectedUsersCallsigns.Add(theMsg.Callsign);

                        Console.WriteLine($"UdpHandler {m_RemoteUserEntries[remoteEndpoint].CallSign} ({remoteEndpoint}) is now an active user.");
                        GlobalLogger.Log($"UdpHandler {m_RemoteUserEntries[remoteEndpoint].CallSign} ({remoteEndpoint}) is now an active user.");

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

            Console.WriteLine($"UdpHandler receive task stopped.");
            GlobalLogger.Log($"UdpHandler receive task stopped.");
        }

        private void processMsg(ArtyMsg theMsg, IPEndPoint remoteEndPoint)
        {
            m_RemoteUserEntries[remoteEndPoint].Update(theMsg);
            if (theMsg.Coords != null)
            {
                latestCoordsMsgId = theMsg.Coords.MsgId;
                dataManager.NewArtyMsgReceived(theMsg);
            }
            else if (theMsg.ClientReport != null) // Received by the server.
            {
                if (theMsg.ClientReport.LastCoordsIdReceived < latestCoordsMsgId)
                {
                    resendCoordsToClient(remoteEndPoint);
                }
                Console.WriteLine($"UdpHandler.processMsg() [ClientStatus] CallSign: {theMsg.Callsign} Type: {theMsg.ClientReport.ClientType}");
                GlobalLogger.Log($"UdpHandler.processMsg() [ClientStatus] CallSign: {theMsg.Callsign} Type: {theMsg.ClientReport.ClientType}");
            }
            else if (theMsg.ServerReport != null) // Received by a client.
            {
                //dataManager.ConnectedUsersCallsigns.Clear(); // TODO: Selective add/remove rather than clear all?
                //foreach (string activeUserCallsign in theMsg.ServerReport.ActiveCallsigns)
                //{
                //    if (!dataManager.ConnectedUsersCallsigns.Contains(activeUserCallsign))
                //    {
                //        dataManager.ConnectedUsersCallsigns.Add(activeUserCallsign);
                //    }
                //}

                Console.WriteLine($"UdpHandler.processMsg() [ServerStatus] CallSign: {theMsg.Callsign} ActiveCallsigns: {theMsg.ServerReport.ActiveCallsigns}");
                GlobalLogger.Log($"UdpHandler.processMsg() [ServerStatus] CallSign: {theMsg.Callsign} ActiveCallsigns: {theMsg.ServerReport.ActiveCallsigns}");
            }
        }

        private void removeTimedOutUsers()
        {
            List<IPEndPoint> usersToRemove = new List<IPEndPoint>();
            List<string> userCallsignsToUpdate = new List<string>();
            foreach (KeyValuePair<IPEndPoint, RemoteUserEntry> activeUserEntry in m_RemoteUserEntries)
            {
                TimeSpan timeSinceLastSeen = DateTime.Now - activeUserEntry.Value.TimeLastSeen;
                if ((activeUserEntry.Value.CanTimeOut) && (timeSinceLastSeen.TotalMilliseconds > PitBossConstants.REMOTE_USER_TIMEOUT_MILLISECONDS))
                {
                    usersToRemove.Add(activeUserEntry.Key);
                    Console.WriteLine($"UdpHandler {activeUserEntry.Value.CallSign} ({activeUserEntry.Key}) timed out, removing from active users list.");
                    GlobalLogger.Log($"UdpHandler {activeUserEntry.Value.CallSign} ({activeUserEntry.Key}) timed out, removing from active users list.");
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

            dataManager.UpdateConnectedUsers(userCallsignsToUpdate);

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
