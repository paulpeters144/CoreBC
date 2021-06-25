using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;

namespace CoreBC.P2PLib
{
   public class PeerServer
   {
      private TcpListener Listener { get; set; }
      public bool Running { get; set; }
      public Hashtable ClientHT { get; set; }
      public int BuffSize { get; set; }
      private int MaxClientCount { get; set; }
      public string ID { get; set; }
      public PeerServer(string id, int buffSize, int maxClientCount)
      {
         ID = id;
         ClientHT = new Hashtable();
         BuffSize = buffSize;
         MaxClientCount = maxClientCount;
      }
      public async void ListenOn(int port)
      {
         if (Running)
         {
            Console.WriteLine("Already connected.");
            return;
         }

         Listener = new TcpListener(IPAddress.Any, port);
         Listener.Start();
         Console.WriteLine("Listening on port: " + port);
         MessageParser messageParser = new MessageParser();
         Running = true;
         while (Running)
         {
            try
            {
               var clientSocket = await Listener.AcceptTcpClientAsync();
               byte[] bytesFrom = new byte[BuffSize];
               NetworkStream networkStream = clientSocket.GetStream();
               await networkStream.ReadAsync(bytesFrom, 0, BuffSize);
               messageParser.ConsumeBites(bytesFrom);

               if (messageParser.EndOfFile)
               {
                  string clientId = messageParser.SenderId;
                  handleMessage(clientId, messageParser.Message);
                  handleClient(clientId, clientSocket);
               }
            }
            catch (Exception ex)
            {
               string error = ex.Message;
               if (error.ToLower().Contains("connection was forcibly closed by the remote host"))
               {
                  ClientHT.Remove(messageParser.SenderId);
                  Console.WriteLine(messageParser.SenderId + " disconnected");
               }
            }
         }
      }
      private async void handleClient(string clientId, TcpClient clientSocket)
      {
         if (!ClientHT.ContainsKey(clientId))
            ClientHT.Add(clientId, clientSocket);
         MessageParser messageParser = new MessageParser();
         while (true)
         {
            byte[] bytesFrom = new byte[BuffSize];
            NetworkStream networkStream = clientSocket.GetStream();
            await networkStream.ReadAsync(bytesFrom, 0, BuffSize);
            messageParser.ConsumeBites(bytesFrom);
            if (messageParser.EndOfFile)
               handleMessage(messageParser.SenderId, messageParser.Message);
         }
      }

      private void handleMessage(string clientId, string message)
      {
         // will need to handle messages from clients to serve from here
         Console.WriteLine($"{clientId}: {message}");
      }

      public string prepMessage(string msg) =>
         $"{ID}<ID>{msg}<EOF>";
   }
}
