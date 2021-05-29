using CoreBC.P2PLib;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace CoreBC
{
   class CommandListener
   {
      public P2PNetwork P2PNetwork { get; set; }
      private Thread ServerThread { get; set; }
      private Thread ClientThread { get; set; }
      public void ProcessCommand(string mainCmd)
      {
         string subCmd = "";
         if (mainCmd.Contains(" "))
         {
            subCmd = mainCmd.Split(' ')[1];
            mainCmd = mainCmd.Split(' ')[0];
         }
         switch (mainCmd)
         {
            case "help": showAllCommands(); break;
            case "clear": Console.Clear(); break;
            case "listen": listenForConnections(subCmd); break;
            case "connect-to": connectTo(subCmd); break;
            default: Console.WriteLine("Not a valid command"); break;
         }
      }

      private void connectTo(string subCmd)
      {
         string ipAddres = subCmd.Split(':')[0];
         int port = Convert.ToInt32(subCmd.Split(':')[1]);

         //string ipAddres = subCmd;
         //var port = Convert.ToInt32(Program.Configuration.GetSection("Server:Port").Value);

         if (P2PNetwork != null)
         {
            P2PNetwork.ConnectTo(ipAddres, port);
         }
      }

      private void listenForConnections(string portString)
      {
         int port = Convert.ToInt32(portString);
         if (P2PNetwork == null)
         {
            //var port = Convert.ToInt32(Program.Configuration.GetSection("Server:Port").Value);
            
            P2PNetwork = new P2PNetwork(port);
            ServerThread = new Thread(P2PNetwork.StartListeningOn);
            ServerThread.Start();
         }
         else
         {

         }
      }

      private void showAllCommands()
      {
         List<string> availableCommands = new List<string> 
         {
            "'clear' to clear console",
            "'list' to start listtening for connections",
            "'connect-to <ipaddress>:<port>' to connect to a another node",
         };
       
         foreach (var cmd in availableCommands)
            Console.WriteLine(cmd);
      }
   }
}
