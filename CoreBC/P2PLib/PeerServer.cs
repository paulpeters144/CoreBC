using CoreBC.DataAccess;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

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
            }
         }
      }

      private async void handleClient(MessageParser messageParser, TcpClient clientSocket)
      {
         while (true)
         {
            byte[] bytesFrom = new byte[BuffSize];
            NetworkStream networkStream = clientSocket.GetStream();
            await networkStream.ReadAsync(bytesFrom, 0, BuffSize);
            messageParser.ConsumeBites(bytesFrom);
            if (messageParser.EndOfFile)
               handleMessage(messageParser, clientSocket);
         }
      }

      private void handleMessage(MessageParser message, TcpClient clientSocket)
      {
         switch (message.FromClient)
         {
            case MsgFromClient.NewTransaction:
               break;
            case MsgFromClient.MinedBlockFound:
               break;
            case MsgFromClient.NeedBlockHash:
               break;
            case MsgFromClient.NeedBoostrap:
               bootstrap(clientSocket);
               break;
            case MsgFromClient.NeedConnections:
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
      private void bootstrap(TcpClient clientSocket)
      {
         // eventually, we need to send the client ip addresses of other servers that could be connected to.
         var allBlocks = DB.GetAllBlocks();
         string preppedMsg = prepMessage($"<myblockheight>{allBlocks[0].Height}");
         sendMsgToClient(preppedMsg, clientSocket);
      }

      private void sendHeightRange(MessageParser message, TcpClient clientSocket)
      {
         int startingHeight = Convert.ToInt32(message.Message.Split(":")[0]);
         int endingHeight = Convert.ToInt32(message.Message.Split(":")[1]);
         var allBlocks = DB.GetAllBlocks().Where(b => b.Height >= startingHeight && b.Height <= endingHeight);
         string json = JsonConvert.SerializeObject(allBlocks, Formatting.None);
         string preppedMsg = prepMessage($"<heresheightrange>{json}");
         sendMsgToClient(preppedMsg, clientSocket);
      }
      #endregion

   }
}
