using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace CoreBC.P2PLib
{
   class P2PNetwork
   {
      private Hashtable ConnectedServers;
      private PeerServer PeerServer;
      private Thread ServerThread;
      
      public P2PNetwork()
      {
         ConnectedServers = new Hashtable();
      }

      public void ConnectToServer(string ipAddres, int serverPort)
      {
         PeerClient peerClient = new PeerClient(ipAddres, serverPort);
         ConnectedServers.Add(ipAddres, peerClient);
         peerClient.ConnectToServer();
      }

      public void ListenForClientsOn(int localPort)
      {
         PeerServer = new PeerServer(localPort);
         ServerThread = new Thread(PeerServer.StartServer);
         ServerThread.Start();
      }
   }
}
