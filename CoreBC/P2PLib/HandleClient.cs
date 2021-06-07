using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace CoreBC.P2PLib
{
   class HandleClient
   {
      TcpClient clientSocket;
      public Guid ClientName;
      private int ByteSize = 256;
      Thread ctThread;
      private bool KeepRunning = true;

      public HandleClient()
      {
         ClientName = Guid.NewGuid();
      }
      public void startClient(TcpClient inClientSocket)
      {
         clientSocket = inClientSocket;
         ctThread = new Thread(listenForMessages);
         ctThread.Start();
      }

      private void listenForMessages()
      {
         byte[] bytesFrom = new byte[ByteSize];
         string dataFromClient = null;

         while (KeepRunning)
         {
            try
            {
               NetworkStream networkStream = clientSocket.GetStream();
               networkStream.Read(bytesFrom, 0, ByteSize);
               dataFromClient = Encoding.ASCII.GetString(bytesFrom);
               dataFromClient = dataFromClient.Substring(0, dataFromClient.IndexOf("$"));
               Console.WriteLine("From client - " + ClientName + " : " + dataFromClient);

            }
            catch (Exception ex)
            {
               string error = ex.ToString();
               if (error.Contains("forcibly closed by the remote host"))
               {
                  string message = ClientName + " disconected";
                  clientSocket.Close();
                  Console.WriteLine(message);
                  KeepRunning = false;
                  //Program.clientsList.Remove(ClientName);
                  ctThread.Abort();
               }
            }
         }
      }
   }
}
