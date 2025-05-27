using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DakkaDataLink
{
    internal class ServerSession
    {
        internal ServerSession(string _sessionId, string _sessionPassword)
        {
            SessionId = _sessionId;
            SessionPassword = _sessionPassword;
        }

        internal bool ValidateAndAddUser(UdpHandler.RemoteUserEntry user)
        {
            bool userValidated = true;
            if (!ActiveUserEntries.ContainsKey(user.RemoteEndPoint))
            {
                if (user.LastClientReport.SessionPassword == SessionPassword)
                {
                    ActiveUserEntries.Add(user.RemoteEndPoint, user);
                    GlobalLogger.Log($"User {user.CallSign} added to session {SessionId}.");
                    //userValidated = true;
                }
                else
                {
                    GlobalLogger.Log($"User {user.CallSign} supplied wrong password for session {SessionId}.");
                    userValidated = false;
                }
            }
            return userValidated;
        }

        internal void RemoveUser(UdpHandler.RemoteUserEntry user)
        {
            if (ActiveUserEntries.ContainsKey(user.RemoteEndPoint))
            {
                ActiveUserEntries.Remove(user.RemoteEndPoint);
                GlobalLogger.Log($"User {user.CallSign} removed from session {SessionId}.");
            }
        }

        internal bool IsUserInSession(IPEndPoint user)
        {
            return ActiveUserEntries.ContainsKey(user);
        }

        internal double LatestDist = 0.0;
        internal double LatestAz = 0.0;
        internal int LatestCoordsMsgIdSent = 0;
        internal int LatestCoordsMsgIdReceived = 0;
        internal string SessionId = "";
        internal string SessionPassword = "";
        internal Dictionary<IPEndPoint, UdpHandler.RemoteUserEntry> ActiveUserEntries = new Dictionary<IPEndPoint, UdpHandler.RemoteUserEntry>();
    }
}
