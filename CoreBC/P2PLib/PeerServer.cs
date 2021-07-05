using CoreBC.BlockModels;
using CoreBC.DataAccess;
using CoreBC.Utils;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace CoreBC.P2PLib
{
   public class PeerServer
   {
      private TcpListener Listener { get; set; }
      public bool Running { get; set; }
      public Hashtable ClientHT { get; set; }
      public int BuffSize { get; set; }
      private int MaxClientCount { get; set; }
      private IDataAccess DB { get; set; }
      public string ID { get; set; }
      public PeerServer(string id, int buffSize, int maxClientCount)
      {
         ID = id;
         ClientHT = new Hashtable();
         BuffSize = buffSize;
         MaxClientCount = maxClientCount;
         DB = new BlockChainFiles();
      }
      public async void ListenOn(int port)
      {
         if (Running)
         {
            Console.WriteLine("Already connected.");
            return;
         }

         Listener = new TcpListener(IPAddress.Any, port);
         Listener.Start();
         Console.WriteLine("Listening on port: " + port);
         MessageParser messageParser = new MessageParser();
         Running = true;
         while (Running)
         {
            try
            {
               var clientSocket = await Listener.AcceptTcpClientAsync();
               Console.WriteLine(clientSocket.Client.RemoteEndPoint);
               byte[] bytesFrom = new byte[BuffSize];
               NetworkStream networkStream = clientSocket.GetStream();
               await networkStream.ReadAsync(bytesFrom, 0, BuffSize);
               messageParser.ConsumeBites(bytesFrom);
               if (messageParser.EndOfFile)
               {
                  string clientId = messageParser.SenderId;

                  if (!ClientHT.ContainsKey(clientId))
                     ClientHT.Add(clientId, clientSocket);

                  handleMessage(messageParser, clientSocket);
                  handleClient(messageParser, clientSocket);
               }
            }
            catch (Exception ex)
            {
               string error = ex.Message;
               if (error.ToLower().Contains("connection was forcibly closed by the remote host"))
               {
                  ClientHT.Remove(messageParser.SenderId);
                  Console.WriteLine(messageParser.SenderId + " disconnected");
               }
               else
               {
                  string er = $"{ex.Message}{Environment.NewLine}{ex.StackTrace}";
                  Console.WriteLine(er);
               }
            }
         }
      }

      private async void handleClient(MessageParser messageParser, TcpClient clientSocket)
      {
         while (true)
         {
            try
            {
               byte[] bytesFrom = new byte[BuffSize];
               NetworkStream networkStream = clientSocket.GetStream();
               await networkStream.ReadAsync(bytesFrom, 0, BuffSize);
               messageParser.ConsumeBites(bytesFrom);
               if (messageParser.EndOfFile)
                  handleMessage(messageParser, clientSocket);
            }
            catch (Exception ex)
            {
               string error = ex.Message;
               if (error.ToLower().Contains("connection was forcibly closed by the remote host"))
               {
                  ClientHT.Remove(messageParser.SenderId);
                  Console.WriteLine(messageParser.SenderId + " disconnected");
               }
               else
               {
                  string er = $"{ex.Message}{Environment.NewLine}{ex.StackTrace}";
                  Console.WriteLine(er);
               }
            }
         }
      }

      public async void BroadcastExcept(string preppedMsg, string clientId)
      {
         foreach (DictionaryEntry client in ClientHT)
         {
            if (client.Key.ToString() == clientId)
               continue;

            TcpClient clientSocket = (TcpClient)client.Value;
            byte[] outStream = Encoding.ASCII.GetBytes(preppedMsg);
            var serverStream = clientSocket.GetStream();
            await serverStream.WriteAsync(outStream, 0, outStream.Length);
            serverStream.Flush();
         }
      }

      private void handleMessage(MessageParser message, TcpClient clientSocket)
      {
         switch (message.FromClient)
         {
            case MsgFromClient.NewTransaction:
               addNewTransaction(message);
               break;
            case MsgFromClient.MinedBlockFound:
               recordBlock(message);
               break;
            case MsgFromClient.NeedBlockHash:
               break;
            case MsgFromClient.NeedBoostrap:
               bootstrap(clientSocket);
               break;
            case MsgFromClient.NeedConnections:
               sendConnections(message, clientSocket);
               break;
            case MsgFromClient.NeedHeightRange:
               sendHeightRange(message, clientSocket);
               break;
            default: throw new Exception($"unknown message header from client {message.SenderId}");
         }
      }

      private void sendMsgToClient(string preppedMsg, TcpClient clientSocket)
      {
         byte[] outStream = Encoding.ASCII.GetBytes(preppedMsg);
         var serverStream = clientSocket.GetStream();
         serverStream.Write(outStream, 0, outStream.Length);
         serverStream.Flush();
      }

      private string prepMessage(string msg) =>
         $"{ID}<ID>{msg}<EOF>";

      //Response to client requests
      #region
      private void sendConnections(MessageParser message, TcpClient senderSocket)
      {
         string senderId = message.SenderId;
         string msg = string.Empty;
         foreach (DictionaryEntry item in ClientHT)
         {
            if (item.Key.ToString() == senderId)
               continue;

            TcpClient clientSocket = (TcpClient)item.Value;
            msg += $"{clientSocket.Client.RemoteEndPoint},";
         }
         if (!String.IsNullOrEmpty(msg))
         {
            string preppedMsg = prepMessage($"<gotconnections>{msg}");
            sendMsgToClient(preppedMsg, senderSocket);
         }
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
               string preppedMsg = prepMessage($"<ablockwasmined>{message.Message}");
               string senderId = message.SenderId;
               BroadcastExcept(preppedMsg, senderId);
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
               string preppedMsg = prepMessage($"<gottransaction>{message.Message}");
               string senderId = message.SenderId;
               BroadcastExcept(preppedMsg, senderId);
            }
         }
         catch (Exception ex)
         {
            string error = $"{ex.Message}{Environment.NewLine}{ex.StackTrace}";
            Console.WriteLine("Error connecting: " + error);
         }
      }
      private void bootstrap(TcpClient clientSocket)
      {
         // eventually, we need to send the client ip addresses of other servers that could be connected to.
         var allBlocks = DB.GetAllBlocks();
         if (allBlocks != null)
         {
            string preppedMsg = prepMessage($"<myblockheight>{allBlocks[0].Height}");
            sendMsgToClient(preppedMsg, clientSocket);
         }
      }

      private void sendHeightRange(MessageParser message, TcpClient clientSocket)
      {
         int startingHeight = Convert.ToInt32(message.Message.Split(":")[0]);
         int endingHeight = Convert.ToInt32(message.Message.Split(":")[1]);
         var allBlocks = DB.GetAllBlocks()
            .Where(b => b.Height >= startingHeight && b.Height <= endingHeight)
            .OrderBy(b => b.Height)
            .ToArray();
         for (int i = 0; i < allBlocks.Length; i++)
         {
            string json = JsonConvert.SerializeObject(allBlocks[i], Formatting.None);
            string preppedMsg = prepMessage($"<heresheightrange>{json}");
            Thread.Sleep(250);
            sendMsgToClient(preppedMsg, clientSocket);
         }
      }
      #endregion

   }
}
