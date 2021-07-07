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
    public class MessageHandler
    {
        private DBAccess DB;
        private string ID;
        public MessageHandler(string id)
        {
            DB = new DBAccess();
            ID = id;
        }
        public void Handle(MessageParser message, TcpClient socket)
        {
            try
            {
                if (message.Header == MessageHeader.NewTransaction)
                {
                    addNewTransaction(message);
                }
                else if (message.Header == MessageHeader.BlockMined)
                {
                    recordBlock(message);
                }
                else if (message.Header == MessageHeader.NeedBoostrap)
                {
                    bootstrap(socket);
                }
                else if (message.Header == MessageHeader.NeedHeightRange)
                {
                    sendHeightRange(message, socket);
                }
                else if (message.Header == MessageHeader.HeresMyBlockHeight)
                {
                    checkForNewBlockHeight(message, socket);
                }
                else if (message.Header == MessageHeader.HeresHeightRange)
                {
                    addHeightRangeToBlocks(message);
                }
                else if (message.Header == MessageHeader.MyHastList)
                {
                    assessHashList(message, socket);
                }
                else if (message.Header == MessageHeader.NeedHash)
                {
                    sendBlockFromHash(message, socket);
                }
                else if (message.Header == MessageHeader.HeresBlockHash)
                {
                    recordBlock(message);
                }
            }
            catch (Exception ex)
            {
                Helpers.ReadException(ex);
            }
        }
        public void SendMessageToSocket(string preppedMsg, TcpClient socket)
        {
            byte[] outStream = Encoding.ASCII.GetBytes(preppedMsg);
            var serverStream = socket.GetStream();
            serverStream.Write(outStream, 0, outStream.Length);
            serverStream.Flush();
        }
        public void BroadcastExcept(string preppedMsg, string senderId)
        {
            foreach (var socket in P2PHelpers.ConnectedClients)
            {
                if (socket.Key == senderId || socket.Key == ID)
                    continue;

                SendMessageToSocket(preppedMsg, socket.Value);
            }
        }
        public void Broadcast(string preppedMsg)
        {
            foreach (var socket in P2PHelpers.ConnectedClients)
            {
                if (socket.Key == ID)
                    continue;

                SendMessageToSocket(preppedMsg, socket.Value);
            }
        }

        private void sendBlockFromHash(MessageParser message, TcpClient socket)
        {
            BlockModel requestedBlock = DB.GetBlockByHash(message.Message);
            string json = JsonConvert.SerializeObject(requestedBlock, Formatting.None);
            string prepMsg = P2PHelpers.PrepMessage(ID, MessageHeader.HeresBlockHash, json);
            SendMessageToSocket(prepMsg, socket);
        }

        private void assessHashList(MessageParser message, TcpClient socket)
        {
            string[] recievedHashArray = message.Message.Split(" - ");
            List<string> ownedHashList = DB.GetBlockHashList();
            if (ownedHashList != null)
            {
                List<string> missingHashes = new List<string>();
                foreach (var hash in recievedHashArray)
                {
                    if (!ownedHashList.Contains(hash))
                        missingHashes.Add(hash);
                }
                foreach (var hash in missingHashes)
                {
                    string prepMsg = P2PHelpers.PrepMessage(ID, MessageHeader.NeedHash, hash);
                    SendMessageToSocket(prepMsg, socket);
                }
            }
            else
            {
                foreach (var hash in recievedHashArray)
                {
                    string prepMsg = P2PHelpers.PrepMessage(ID, MessageHeader.NeedHash, hash);
                    SendMessageToSocket(prepMsg, socket);
                }
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

        private void checkForNewBlockHeight(MessageParser message, TcpClient socket)
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
                        SendMessageToSocket(preppedMsg, socket);
                    }
                }
                else
                {
                    long blockHeightFromServer = Convert.ToInt64(message.Message);
                    string range = $"{MessageHeader.NeedHeightRange}" +
                       $"{0}:{blockHeightFromServer}";
                    string preppedMsg = P2PHelpers.PrepMessage(
                            ID, MessageHeader.NeedHeightRange, $"{0}:{blockHeightFromServer}"
                        );
                    SendMessageToSocket(preppedMsg, socket);
                }
            }
            catch (Exception ex)
            {
                Helpers.ReadException(ex);
            }
        }

        private string prepMessage(string msg) =>
           $"{ID}<ID>{msg}<EOF>";

        private void recordBlock(MessageParser message)
        {
            try
            {
                var block = JsonConvert.DeserializeObject<BlockModel>(message.Message);
                BlockModel ownedBlock = DB.GetBlockByHash(block.Hash);

                if (ownedBlock != null)
                    return;

                BlockChecker blockChecker = new BlockChecker();
                bool blockChecksOut = blockChecker.ConfirmEntireBlock(block);

                if (blockChecksOut)
                {
                    DB.SaveRecievedBlock(block);
                    string preppedMsg = prepMessage($"{MessageHeader.BlockMined}{message.Message}");
                    string senderId = message.SenderId;
                    BroadcastExcept(preppedMsg, senderId);
                }
                else
                {
                    Console.WriteLine($"Block {block.Hash} was not confirmed.");
                }

            }
            catch (Exception ex)
            {
                Helpers.ReadException(ex);
            }
        }

        private void addNewTransaction(MessageParser message)
        {
            try
            {
                var tx = JsonConvert.DeserializeObject<TransactionModel>(message.Message);
                bool txSaved = DB.SaveToMempool(tx);
                if (txSaved)
                {
                    string preppedMsg = prepMessage($"{MessageHeader.NewTransaction}{message.Message}");
                    string senderId = message.SenderId;
                    BroadcastExcept(preppedMsg, senderId);
                }
            }
            catch (Exception ex)
            {
                Helpers.ReadException(ex);
            }
        }
        private void bootstrap(TcpClient socket)
        {
            var allBlocks = DB.GetAllBlocks();
            if (allBlocks != null)
            {
                var hashList = allBlocks.Select(b => b.Hash).ToList();
                string hashListMessage = string.Join(" - ", hashList);
                string prepMsg = prepMessage($"{MessageHeader.MyHastList}{hashListMessage}");
                SendMessageToSocket(prepMsg, socket);
            }
        }

        private void sendHeightRange(MessageParser message, TcpClient socket)
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
                SendMessageToSocket(preppedMsg, socket);
            }
        }
    }
}
