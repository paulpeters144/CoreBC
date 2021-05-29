using NetCoreServer;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CoreBC.P2PLib
{
   class Session : TcpSession
   {
      public Session(TcpServer server) : base(server) { }

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
         Server.FindSession(Id).SendAsync(message);

         // If the buffer starts with '!' the disconnect the current session
         if (message == "!")
            Disconnect();
      }

      protected override void OnError(SocketError error)
      {
         Console.WriteLine($"Chat TCP session caught an error with code {error}");
      }
   }

   class ChatServer : TcpServer
   {
      public ChatServer(IPAddress address, int port) : base(address, port) { }

      protected override TcpSession CreateSession() { return new Session(this); }

      protected override void OnError(SocketError error)
      {
         Console.WriteLine($"Chat TCP server caught an error with code {error}");
      }
   }
}
