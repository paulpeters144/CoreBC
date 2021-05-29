using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace CoreBC.P2PLib
{
   class P2PNetwork
   {
      private int Port { get; set; }
      public Guid ServerId { get; set; }
      private List<string> ServersConnectedTo;
      public P2PNetwork(int port)
      {
         Port = port;
         ServersConnectedTo = new List<string>();
      }
      public void StartListeningOn()
      { 
         Console.WriteLine($"TCP server port: {Port}");
         Console.WriteLine();
         var server = new ChatServer(IPAddress.Any, Port);
         ServerId = server.Id;
         Console.Write("Server starting...");
         server.Start();
         Console.WriteLine("Done!");
         Console.WriteLine("Press Enter to stop the server or '!' to restart the server...");

         while (true)
         {
            string line = Console.ReadLine();
            if (string.IsNullOrEmpty(line))
               break;

            // Restart the server
            if (line == "!")
            {
               Console.Write("Server restarting...");
               server.Restart();
               Console.WriteLine("Done!");
               continue;
            }

            line = "(admin) " + line;
            server.Multicast(line);
         }

         // Stop the server
         Console.Write("Server stopping...");
         server.Stop();
         Console.WriteLine("Done!");
      }

      
      public void ConnectTo(string ipAddress, int port)
      {
         string serverAddress = $"{ipAddress}:{port}";
         
         if (ServersConnectedTo.Contains(serverAddress))
            return;
         
         ServersConnectedTo.Add(serverAddress);
         //address = "127.0.0.1";

         Console.WriteLine($"TCP server address: {ipAddress}");
         Console.WriteLine($"TCP server port: {port}");

         Console.WriteLine();

         var client = new Messanger(ipAddress, port);
         Console.Write("Client connecting...");
         client.ConnectAsync();
         Console.WriteLine("Done!");

         Console.WriteLine("Press Enter to stop the client or '!' to reconnect the client...");

         // Perform text input
         while (true)
         {
            string line = Console.ReadLine();
            if (string.IsNullOrEmpty(line))
               break;

            // Disconnect the client
            if (line == "!")
            {
               Console.Write("Client disconnecting...");
               client.DisconnectAsync();
               Console.WriteLine("Done!");
               continue;
            }

            client.SendAsync(line);
         }

         Console.Write("Client disconnecting...");
         client.DisconnectAndStop();
         Console.WriteLine("Done!");
      }
   }
}
