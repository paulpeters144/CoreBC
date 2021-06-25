using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace CoreBC.P2PLib
{
   class PeerClient
   {
      private Dictionary<string, TcpClient> ServerDic;
      private int BuffSize { get; set; }
      private int MaxServers { get; set; }
      private string ID { get; set; }
      public PeerClient(string id, int buffSize, int maxServers)
      {
         ID = id;
         ServerDic = new Dictionary<string, TcpClient>();
         BuffSize = buffSize;
         MaxServers = maxServers;
      }

      public void Connect(string ip, int port)
      {
         string serverName = $"{ip}:{port}";

         if (ServerDic.Count > MaxServers)
         {
            Console.WriteLine("Servers connected to is maxed at " + MaxServers);
            return;
         }

         if (!ServerDic.ContainsKey(serverName))
         {
            ServerDic.Add(serverName, new TcpClient());
            ServerDic[serverName].Connect(ip, port);

            string preppedMsg = prepMessage("init connection");
            byte[] outStream = Encoding.ASCII.GetBytes(preppedMsg);
            var serverStream = ServerDic[serverName].GetStream();
            serverStream.Write(outStream, 0, outStream.Length);
            serverStream.Flush();

            Thread messageThread = new Thread(() => listenForMessages(serverName));
            messageThread.Start();
         }
         else
         {
            Console.WriteLine("Already connected to a server");
         }
      }

      public void SendMessage(string message)
      {
         if (ServerDic.Count == 0)
         {
            Console.WriteLine("Not connected to a server.");
            return;
         }
         foreach (var client in ServerDic.Values) // brodcast msg to all connected servers
         {
            string preppedMsg = prepMessage(message);
            byte[] outStream = Encoding.ASCII.GetBytes(preppedMsg);
            var serverStream = client.GetStream();
            serverStream.Write(outStream, 0, outStream.Length);
            serverStream.Flush();
         }
      }

      private void listenForMessages(string serverName)
      {
         MessageParser messageParser = new MessageParser();
         bool connected = true;
         while (connected)
         {
            try
            {
               var serverStream = ServerDic[serverName].GetStream();
               byte[] inStream = new byte[BuffSize];
               serverStream.Read(inStream, 0, BuffSize);
               messageParser.ConsumeBites(inStream);

               if (messageParser.EndOfFile)
                  handleMessage(messageParser.SenderId, messageParser.Message);
            }
            catch (Exception ex)
            {
               string error = ex.Message;
               if (error.ToLower().Contains("connection was forcibly closed by the remote host"))
               {
                  ServerDic.Remove(serverName);
                  connected = false;
                  Console.WriteLine(serverName + " disconnected");
               }
            }
         }
      }

      private void handleMessage(string serverId, string message)
      {
         // will need to handle message from server here
         Console.WriteLine($"{serverId}: {message}");
      }

      public string prepMessage(string msg) =>
         $"{ID}<ID>{msg}<EOF>";
   }
}
