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
      public CommandListener()
      {
         P2PNetwork = new P2PNetwork(2058, 10, 10);
      }
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

      }

      private void listenForConnections(string portString)
      {

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
