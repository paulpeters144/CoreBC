using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace CoreBC.P2PLib
{
   public class P2PNetwork
   {
      public string ID { get; set; }
      public PeerClient Client { get; set; }
      public PeerServer Server { get; set; }

      public int BuffSize { get; set; }
      public P2PNetwork(int buffSize, int maxClientCount, int maxConnectCount)
      {
         ID = Guid.NewGuid().ToString("N").ToUpper();
         BuffSize = buffSize;
         Client = new PeerClient(ID, BuffSize, maxConnectCount);
         Server = new PeerServer(ID, BuffSize, maxClientCount);
      }

      public void ListenOn(int port)
      {
         Server.ListenOn(port);
      }

      public void ConnectTo(string ip, int port)
      {
         Client.Connect(ip, port);
      }

      public void SendMessage(string msg)
      {
         msg = $"{ID}<ID>{msg}<EOF>";
         Client.SendMessage(msg);
         Server.BroadcastExcept(msg, ID);
      }
   }
}
