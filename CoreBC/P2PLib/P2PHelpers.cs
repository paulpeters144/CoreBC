using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace CoreBC.P2PLib
{
    public static class P2PHelpers
    {
        public static Dictionary<string, TcpClient> ConnectedClients;
        public static string PrepMessage(string id, string header, string message = "")
        {
            return $"{id}<ID>{header}{message}<EOF>";
        }
        public static void SendMsgToSocket(string preppedMsg, TcpClient socket)
        {
            byte[] outStream = Encoding.ASCII.GetBytes(preppedMsg);
            var serverStream = socket.GetStream();
            serverStream.Write(outStream, 0, outStream.Length);
            serverStream.Flush();
        }
    }
}
