using CoreBC.BlockModels;
using CoreBC.DataAccess;
using CoreBC.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace CoreBC.P2PLib
{
   public class PeerClient
   {
      private Dictionary<string, TcpClient> ServerDic;
      private int BuffSize { get; set; }
      private int MaxServers { get; set; }
      private string ID { get; set; }
      private IDataAccess DB;
      public PeerClient(string id, int buffSize, int maxServers)
      {
         ID = id;
         ServerDic = new Dictionary<string, TcpClient>();
         BuffSize = buffSize;
         MaxServers = maxServers;
         DB = new BlockChainFiles();
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

            string preppedMsg = prepMessage("<bootstrap>");
            byte[] outStream = Encoding.ASCII.GetBytes(preppedMsg);
            var serverStream = ServerDic[serverName].GetStream();
            serverStream.Write(outStream, 0, outStream.Length);
            serverStream.Flush();

            Thread messageThread = new Thread(() => listenForMessages(serverName));
            messageThread.Start();
            Thread connectThread = new Thread(() => askForConnections());
            connectThread.Start();
            Console.WriteLine("Connected to: " + serverName );
         }
         else
         {
            Console.WriteLine("Already connected to a server");
         }
      }

      private void askForConnections()
      {
         bool connected = true;
         int fifteenMinSleep = 900000;
         while (connected)
         {
            try
            {
               if (MaxServers > ServerDic.Count)
               {
                  string message = prepMessage("<needconnections>");
                  foreach (var client in ServerDic.Values) // brodcast msg to all connected servers
                  {
                     byte[] outStream = Encoding.ASCII.GetBytes(message);
                     var serverStream = client.GetStream();
                     serverStream.Write(outStream, 0, outStream.Length);
                     serverStream.Flush();
                  }
               }

               Thread.Sleep(fifteenMinSleep);
            }
            catch (Exception ex)
            {
               string error = ex.Message;
            }
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
            byte[] outStream = Encoding.ASCII.GetBytes(message);
            var serverStream = client.GetStream();
            serverStream.Write(outStream, 0, outStream.Length);
            serverStream.Flush();
         }
      }

      private void broadcastExcept(string preppedMsg, string senderId)
      {
         foreach (var server in ServerDic)
         {
            if (server.Key == senderId)
               continue;

            var serverSocket = server.Value;
            byte[] outStream = Encoding.ASCII.GetBytes(preppedMsg);
            var serverStream = serverSocket.GetStream();
            serverStream.Write(outStream, 0, outStream.Length);
            serverStream.Flush();
         }
      }

      private void listenForMessages(string serverName)
      {
         MessageParser message = new MessageParser();
         bool connected = true;
         while (connected)
         {
            try
            {
               var serverStream = ServerDic[serverName].GetStream();
               byte[] inStream = new byte[BuffSize];
               serverStream.Read(inStream, 0, BuffSize);
               message.ConsumeBites(inStream);

               if (message.EndOfFile)
                  handleMessage(message);
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

      private void handleMessage(MessageParser message)
      {
         switch (message.FromServer)
         {
            case MsgFromServer.ABlockWasMined:
               recordBlock(message);
               break;
            case MsgFromServer.NewTransaction:
               addNewTransaction(message);
               break;
            case MsgFromServer.HeresMyBlockHeight:
               checkForNewBlockHeight(message);
               break;
            case MsgFromServer.HeresSomeConnections:
               addConnections(message);
               break;
            case MsgFromServer.HeresHeightRange:
               addHeightRangeToBlocks(message);
               break;
            case MsgFromServer.PretendIsNull:
               break;
            default: throw new Exception($"unknown message header from client {message.SenderId}");
         }
      }

      private void addConnections(MessageParser message)
      {

      }

      private void recordBlock(MessageParser message)
      {
         try
         {
            var block = JsonConvert.DeserializeObject<BlockModel>(message.Message);
            List<string> blockChainHashes = DB.GetAllBlocks().Select(e => e.Hash).ToList();

            if (blockChainHashes.Contains(block.Hash))
               return;

            BlockChecker blockChecker = new BlockChecker();
            bool blockChecksOut = blockChecker.ConfirmEntireBlock(block);

            if (blockChecksOut)
            {
               DB.SaveRecievedBlock(block);
               string preppedMsg = prepMessage($"<blockmined>{message.Message}");
               string senderId = message.SenderId;
               broadcastExcept(preppedMsg, senderId);
            }
            else
            {
               Console.WriteLine($"Block {block.Hash} was not confirmed.");
            }

         }
         catch (Exception)
         { }
      }


      private void addNewTransaction(MessageParser message)
      {
         try
         {
            var tx = JsonConvert.DeserializeObject<TransactionModel>(message.Message);
            bool txSaved = DB.SaveToMempool(tx);
            if (txSaved)
            {
               string preppedMsg = prepMessage($"<newtransaction>{message.Message}");
               string senderId = message.SenderId;
               broadcastExcept(preppedMsg, senderId);
            }
         }
         catch (Exception)
         { }
      }

      public string prepMessage(string msg) =>
         $"{ID}<ID>{msg}<EOF>";

      #region
      private void checkForNewBlockHeight(MessageParser message)
      {
         try
         {
            var blocks = DB.GetAllBlocks();
            if (blocks != null)
            {
               long blockHeightFromServer = Convert.ToInt64(message.Message);
               long currentBlockHeight = blocks[0].Height;
               if (0 < blockHeightFromServer)
               {
                  string range = $"<needheightrange>{currentBlockHeight}:{blockHeightFromServer}";
                  string preppedMsg = prepMessage(range);
                  SendMessage(preppedMsg);
               }
            }
            else
            {
               long blockHeightFromServer = Convert.ToInt64(message.Message);
               string range = $"<needheightrange>{0}:{blockHeightFromServer}";
               string preppedMsg = prepMessage(range);
               SendMessage(preppedMsg);
            }
         }
         catch (Exception)
         {

         }
      }

      private void addHeightRangeToBlocks(MessageParser message)
      {
         try
         {
            var blocks = DB.GetAllBlocks();
            if (blocks != null)
            {
               List<string> currentBlockHashes = blocks.Select(b => b.Hash).ToList();
               BlockModel blockFromMessage = JsonConvert.DeserializeObject<BlockModel>(message.Message);
               if (!currentBlockHashes.Contains(blockFromMessage.Hash))
                  DB.SaveRecievedBlock(blockFromMessage);
            }
            else
            {
               BlockModel blockFromMessage = JsonConvert.DeserializeObject<BlockModel>(message.Message);
               DB.SaveRecievedBlock(blockFromMessage);
            }
         }
         catch (Exception ex)
         {
            Helpers.ReadException(ex);
         }
      }
      #endregion
   }
}
