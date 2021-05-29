﻿using NetCoreServer;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace CoreBC
{
   class p2p: TcpSession
   {
      public p2p(TcpServer server) : base(server) { }

      protected override void OnConnected()
      {
         Console.WriteLine($"Chat TCP session with Id {Id} connected!");

         // Send invite message
         string message = "Hello from TCP chat! Please send a message or '!' to disconnect the client!";
         SendAsync(message);
      }

      protected override void OnDisconnected()
      {
         Console.WriteLine($"Chat TCP session with Id {Id} disconnected!");
      }

      protected override void OnReceived(byte[] buffer, long offset, long size)
      {
         string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
         Console.WriteLine("Incoming: " + message);

         // Multicast message to all connected sessions
         Server.Multicast(message);

         // If the buffer starts with '!' the disconnect the current session
         if (message == "!")
            Disconnect();
      }
   }
}
