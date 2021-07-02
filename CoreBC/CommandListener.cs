﻿using CoreBC.BlockModels;
using CoreBC.P2PLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace CoreBC
{
   class CommandListener
   {
      public P2PNetwork P2PNetwork { get; set; }
      public CommandListener()
      {
         int bufferSize = 2058;
         int maxClientCount = 10;
         int maxConnectCount = 10;
         P2PNetwork = new P2PNetwork(bufferSize, maxClientCount, maxConnectCount);
      }
      public void ProcessCommand(string mainCmd)
      {
         string subCmd = string.Empty;
         if (mainCmd.Contains(" "))
         {
            string[] cmdArr = mainCmd.Split(' ');
            mainCmd = cmdArr[0];
            subCmd = string.Join(" ", cmdArr.Where((c, i) => i != 0));
         }
         switch (mainCmd)
         {
            case "help": showAllCommands(); break;
            case "clr": Console.Clear(); break;
            case "l": listenForConnections(subCmd); break;
            case "cto": connectTo(subCmd); break;
            case "sign-in": signinAs(subCmd); break;
            case "send-to": sendCurrencyTo(subCmd); break;
            default: Console.WriteLine("Not a valid command"); break;
         }
      }

      private void sendCurrencyTo(string subCmd)
      {
         try
         {
            string address = subCmd.Split(' ')[0];
            string amount = subCmd.Split(' ')[1];
            ChainKeys chainKeys = new ChainKeys(Program.UserName);
            TransactionModel tx = chainKeys.SendMoneyTo(address, Convert.ToDecimal(amount));
            if (tx != null)
            {
               string json = JsonConvert.SerializeObject(tx, Formatting.None);
               string message = $"<newtransaction>{json}";
               P2PNetwork.SendMessage(message);
            }
         }
         catch (Exception)
         {
            Console.WriteLine("Error processing command");
         }
      }

      private void signinAs(string subCmd)
      {
         Program.UserName = subCmd;
         Console.WriteLine("Hello " + subCmd);
      }

      private void connectTo(string subCmd)
      {
         try
         {
            int port = Convert.ToInt32(subCmd.Split(' ')[1]);
            string ip = subCmd.Split(' ')[0];
            P2PNetwork.ConnectTo(ip, port);
         }
         catch (Exception)
         {
            Console.WriteLine("Error processing command");
         }
      }

      private void listenForConnections(string portString)
      {
         int port = Convert.ToInt32(portString);
         P2PNetwork.ListenOn(port);
         Console.WriteLine($"Now listen for connections on port: {portString}");
      }

      private void showAllCommands()
      {
         List<string> availableCommands = new List<string> 
         {
            "'clr' to clear console",
            "'l' to start listtening for connections",
            "'cto <ipaddress>:<port>' to connect to a another node",
            "'sign-in <username>' to connect to your Blockchain Wallet",
            "'balances' to see your currency balance",
            "send-to <wallet-address> amount"
         };
       
         foreach (var cmd in availableCommands)
            Console.WriteLine(cmd);
      }
   }
}
