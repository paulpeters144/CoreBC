using CoreBC.BlockModels;
using CoreBC.DataAccess;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
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
            byte[] outStream = Encoding.ASCII.GetBytes(message);
            var serverStream = client.GetStream();
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
               break;
            case MsgFromServer.NewTransaction:
               break;
            case MsgFromServer.HeresMyBlockHeight:
               checkForNewBlockHeight(message);
               break;
            case MsgFromServer.HeresSomeConnections:
               break;
            case MsgFromServer.HeresHeightRange:
               addHeightRangeToBlocks(message);
               break;
            case MsgFromServer.PretendIsNull:
               break;
            default: throw new Exception($"unknown message header from client {message.SenderId}");
         }
      }

      public string prepMessage(string msg) =>
         $"{ID}<ID>{msg}<EOF>";

      #region
      private void checkForNewBlockHeight(MessageParser message)
      {
         try
         {
            var blocks = DB.GetAllBlocks();
            long blockHeightFromServer = Convert.ToInt64(message.Message);
            long currentBlockHeight = blocks[0].Height;
            if (currentBlockHeight < blockHeightFromServer)
            {
               string range = $"<needheightrange>{currentBlockHeight}:{blockHeightFromServer}";
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
            List<string> currentBlockHashes = DB.GetAllBlocks().Select(b => b.Hash).ToList();
            BlockModel[] blocksFromMessage = JsonConvert.DeserializeObject<BlockModel[]>(message.Message);
            foreach (var block in blocksFromMessage)
            {
               if (!currentBlockHashes.Contains(block.Hash))
                  DB.SaveBlock(block);
            }
         }
         catch (Exception)
         {

            throw;
         }
      }
      #endregion
   }
}
