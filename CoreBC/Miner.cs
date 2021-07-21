using CoreBC.BlockModels;
using CoreBC.DataAccess;
using CoreBC.P2PLib;
using CoreBC.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CoreBC
{
    public class Miner
    {
        public bool IsMining = true;
        private DBAccess DB;
        private P2PNetwork P2PNetwork;
        public Miner(P2PNetwork p2pNetwork)
        {
            DB = new DBAccess();
            P2PNetwork = p2pNetwork;
        }

        public void Mining()
        {
            var minerKey = new Wallet(Program.UserName);

            while (IsMining)
            {
                BlockModel previousBlock = DB.GetAllBlocks()[0];

                Int64 height = previousBlock.Height + 1;
                decimal reward = Helpers.GetMineReward(height);
                CoinbaseModel coinbase = new CoinbaseModel
                {
                    TransactionId = Helpers.GetSHAStringFromString(
                         $"{height}_belongs_to_{minerKey.GetPubKeyString()}"
                      ),
                    Output = new Output()
                    {
                        Amount = Helpers.FormatDigits(reward),
                        ToAddress = minerKey.GetPubKeyString()
                    },
                };

                List<TransactionModel> memPool = DB.GetMempool();

                string[] allTransactions =
                   memPool.Select(x => x.TransactionId).Prepend(coinbase.TransactionId).ToArray();

                string merkleRoot = string.Empty;
                if (allTransactions.Length > 1)
                    merkleRoot = getMerkleFrom(allTransactions);
                else
                    merkleRoot = Helpers.GetSHAStringFromString(coinbase.TransactionId);

                decimal feeReward = 0;
                if (memPool.Count > 0)
                    feeReward = memPool.Select(t => Convert.ToDecimal(t.Fee)).Sum();

                var allBlocks = DB.GetAllBlocks();
                var nextBlock = new BlockModel()
                {
                    PreviousHash = previousBlock.Hash,
                    Confirmations = 0,
                    TransactionCount = allTransactions.Length,
                    Height = height,
                    MerkleRoot = merkleRoot,
                    TXs = allTransactions,
                    Difficulty = Helpers.GetDifficulty(allBlocks),
                    Coinbase = coinbase
                };
                nextBlock.Coinbase.FeeReward = Helpers.FormatDigits(feeReward);

                nextBlock = Mine(nextBlock, 50000000);
                if (nextBlock != null)
                    save(nextBlock);

            }
        }

        private void save(BlockModel nextBlock)
        {
            nextBlock.Coinbase.BlockHash = nextBlock.Hash;
            BlockChecker blockChecker = new BlockChecker();
            if (blockChecker.ConfirmEntireBlock(nextBlock))
            {
                blockChecker.ConfirmPriorBlocks();
                bool blockSaved = DB.SaveMinedBlock(nextBlock);
                if (blockSaved)
                {
                    string json = JsonConvert.SerializeObject(nextBlock, Formatting.None);
                    string prepMsg = P2PHelpers.PrepMessage(P2PNetwork.ID, MessageHeader.BlockMined, json);
                    P2PNetwork.BroadCast(prepMsg);
                    Console.WriteLine($"Blockmined: {nextBlock.Hash}");
                }
            }
        }

        private string getMerkleFrom(string[] mempoolTransactions)
        {
            if (mempoolTransactions.Length == 1)
                return mempoolTransactions[0];

            List<string> hashList = new List<string>();
            for (int i = 0; i < mempoolTransactions.Length - 1; i += 2)
            {
                string hash1 = mempoolTransactions[i];
                string hash2 = mempoolTransactions[i + 1];
                string hashedSet = Helpers.GetSHAStringFromString($"{hash1}{hash2}");
                hashList.Add(hashedSet);
            }
            if (mempoolTransactions.Length % 2 == 1)
            {
                string lastHashInArray = mempoolTransactions[mempoolTransactions.Length - 1];
                string lastHash = Helpers.GetSHAStringFromString($"{lastHashInArray}{lastHashInArray}");
                hashList.Add(lastHash);
            }

            string[] hashArray = hashList.ToArray();
            return getMerkleFrom(hashArray);
        }

        public BlockModel Mine(BlockModel block, int maxLoopCount)
        {
            string prevHash = block.PreviousHash;
            string mRoot = block.MerkleRoot;
            string difficulty = block.Difficulty;
            block.Time = Helpers.GetCurrentUTC();
            Int64 nonce = 0;
            for (int i = 0; i < maxLoopCount; i++)
            {
                if (!IsMining)
                    break;

                if (Helpers.WeHaveReceivedNewBlock)
                {
                    Helpers.WeHaveReceivedNewBlock = false;
                    break;
                }

                string attempt = $"{prevHash}{mRoot}{block.Time}{difficulty}{nonce}";
                string hashAttemp = Helpers.GetSHAStringFromString(attempt);

                if (hashAttemp.StartsWith(block.Difficulty))
                {
                    block.Hash = hashAttemp;
                    block.Nonce = nonce;
                    return block;
                }
                else
                {
                    nonce++;
                }
            }
            return null;
        }
    }
}
