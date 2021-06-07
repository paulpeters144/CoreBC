using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CoreBC.P2PLib
{
   public class PeerServer
   {
      public static Hashtable clientsList = new Hashtable();
      private static int ByteSize = 256;
      public int LocalPort { get; set; }
      public PeerServer(int localPort)
      {
         LocalPort = localPort;
      }
      public void StartServer()
      {
         //IPAddress localAddr = IPAddress.Parse("127.0.0.1");
         TcpListener serverSocket = new TcpListener(IPAddress.Any, LocalPort);
         TcpClient clientSocket = default(TcpClient);

         serverSocket.Start();
         Console.WriteLine("Chat Server Started ....");
         while (true)
         {
            clientSocket = serverSocket.AcceptTcpClient();

            byte[] bytesFrom = new byte[ByteSize];
            string dataFromClient = null;

            NetworkStream networkStream = clientSocket.GetStream();
            networkStream.Read(bytesFrom, 0, ByteSize);
            dataFromClient = Encoding.ASCII.GetString(bytesFrom);
            dataFromClient = dataFromClient.Substring(0, dataFromClient.IndexOf("$"));

            HandleClient client = new HandleClient();
            Console.WriteLine($"client {client.ClientName} connected");
            clientsList.Add(client.ClientName, clientSocket);
            client.startClient(clientSocket);
         }

         clientSocket.Close();
         serverSocket.Stop();
         Console.WriteLine("exit");
         Console.ReadLine();
      }
   }
}
