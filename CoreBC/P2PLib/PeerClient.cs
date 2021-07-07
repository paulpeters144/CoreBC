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
        private string ID { get; set; }
        private DBAccess DB;
        public PeerClient(string id, int buffSize)
        {
            ID = id;
            ServerDic = new Dictionary<string, TcpClient>();
            BuffSize = buffSize;
            DB = new DBAccess();
        }

        public void Connect(string ip, int port)
        {
            string serverName = $"{ip}:{port}";

            if (!ServerDic.ContainsKey(serverName))
            {
                ServerDic.Add(serverName, new TcpClient());
                ServerDic[serverName].Connect(ip, port);

                string preppedMsg = prepMessage(MessageHeader.NeedBoostrap);
                byte[] outStream = Encoding.ASCII.GetBytes(preppedMsg);
                var serverStream = ServerDic[serverName].GetStream();
                serverStream.Write(outStream, 0, outStream.Length);
                serverStream.Flush();

                Thread messageThread = new Thread(() => listenForMessages(serverName));
                messageThread.Start();
                Console.WriteLine("Connected to: " + serverName);
            }
            else
            {
                Console.WriteLine("Already connected to a server");
            }
        }

        public void SendMessage(string message)
        {
            if (ServerDic.Count == 0)
                return;

            foreach (var client in ServerDic.Values) // brodcast msg to all connected servers
            {
                byte[] outStream = Encoding.ASCII.GetBytes(message);
                var serverStream = client.GetStream();
                serverStream.Write(outStream, 0, outStream.Length);
                serverStream.Flush();
            }
        }

        private void sendMsgTo(string preppedMsg, TcpClient serverSocket)
        {
            byte[] outStream = Encoding.ASCII.GetBytes(preppedMsg);
            var serverStream = serverSocket.GetStream();
            serverStream.Write(outStream, 0, outStream.Length);
            serverStream.Flush();
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
                        handleMessage(message, serverName);
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

        private void handleMessage(MessageParser message, string serverName)
        {
            if (message.Header == MessageHeader.BlockMined)
            {
                recordBlock(message);
            }
            else if (message.Header == MessageHeader.NewTransaction)
            {
                addNewTransaction(message);
            }
            else if (message.Header == MessageHeader.HeresMyBlockHeight)
            {
                checkForNewBlockHeight(message);
            }
            else if (message.Header == MessageHeader.HeresHeightRange)
            {
                addHeightRangeToBlocks(message);
            }
            else if (message.Header == MessageHeader.NeedBoostrap)
            {
                bootStrapServer(serverName);
            }
            else if (message.Header == MessageHeader.NeedHeightRange)
            {
                sendHeightRange(message, serverName);
            }
            else if (message.Header == MessageHeader.MyHastList)
            {
                assessHashList(message, serverName);
            }
        }

        private void assessHashList(MessageParser message, string serverName)
        {
            throw new NotImplementedException();
        }

        private void sendHeightRange(MessageParser message, string serverName)
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
                string preppedMsg = prepMessage($"{MessageHeader.HeresHeightRange}{json}");
                Thread.Sleep(250);
                sendMsgTo(preppedMsg, ServerDic[serverName]);
            }
        }

        private void sendBlockHashList(string serverName)
        {
            var blockHashList = DB.GetBlockHashList();
            if (blockHashList != null)
            {
                string hashes = string.Join("\t", blockHashList);
                string prepMsg = prepMessage($"{MessageHeader.MyHastList}{hashes}");
                var serverSocket = ServerDic[serverName];
                sendMsgTo(prepMsg, serverSocket);
            }
        }

        private void bootStrapServer(string serverName)
        {
            //TODO: need to update the boostrap protocol so that we request and recieve block hashes
            // and not block heights
            var allBlocks = DB.GetAllBlocks();
            if (allBlocks != null)
            {
                //TcpClient serverSocket = ServerDic[serverName];
                //string preppedMsg = prepMessage($"{MessageHeader.HeresMyBlockHeight}{allBlocks[0].Height}");
                //sendMsgTo(preppedMsg, serverSocket);
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
                    string preppedMsg = prepMessage($"{MessageHeader.BlockMined}{message.Message}");
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
                    string preppedMsg = prepMessage($"{MessageHeader.NewTransaction}" +
                       $"{message.Message}");
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
                        string range = $"{MessageHeader.NeedHeightRange}" +
                           $"{currentBlockHeight}:{blockHeightFromServer}";
                        string preppedMsg = prepMessage(range);
                        SendMessage(preppedMsg);
                    }
                }
                else
                {
                    long blockHeightFromServer = Convert.ToInt64(message.Message);
                    string range = $"{MessageHeader.NeedHeightRange}" +
                       $"{0}:{blockHeightFromServer}";
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
