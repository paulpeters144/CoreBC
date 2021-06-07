using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace CoreBC.P2PLib
{
   class PeerClient
   {
      TcpClient clientSocket = new TcpClient();
      NetworkStream serverStream = default(NetworkStream);
      public string ServerIP { get; set; }
      public int ServerPort { get; set; }
      public int BufferSize = 256;
      public Guid ID { get; set; }
      public PeerClient(string serverIP, int serverPort)
      {
         ServerIP = serverIP;
         ServerPort = serverPort;
         ID = Guid.NewGuid();
      }

      public void sendMessage(string message)
      {
         byte[] outStream = Encoding.ASCII.GetBytes(message);
         serverStream.Write(outStream, 0, outStream.Length);
         serverStream.Flush();
      }

      public void ConnectToServer()
      {
         clientSocket.Connect(ServerIP, ServerPort);
         serverStream = clientSocket.GetStream();

         byte[] outStream = Encoding.ASCII.GetBytes("connecting" + "$");
         serverStream.Write(outStream, 0, outStream.Length);
         serverStream.Flush();

         Thread ctThread = new Thread(getMessage);
         ctThread.Start();
      }

      private void getMessage()
      {
         while (true)
         {
            serverStream = clientSocket.GetStream();
            byte[] inStream = new byte[BufferSize];
            serverStream.Read(inStream, 0, BufferSize);
            string messageFromServer = Encoding.ASCII.GetString(inStream);
         }
      }
   }
}
