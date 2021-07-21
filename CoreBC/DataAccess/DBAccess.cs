using CoreBC.BlockModels;
using CoreBC.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace CoreBC.DataAccess
{
    public class DBAccess
    {
        public string BlockchainPath { get; set; }
        public string MempoolPath { get; set; }
        public string AccountSetPath { get; set; }

        public DBAccess()
        {
            BlockchainPath = Helpers.GetBlockchainFilePath();
            MempoolPath = Helpers.GetMempooFile();
            AccountSetPath = Helpers.GetAcctSetFile();
        }
        public DBAccess(string blockchainPath, string mempoolPath, string accountSetPath)
        {
            BlockchainPath = blockchainPath;
            MempoolPath = mempoolPath;
            AccountSetPath = accountSetPath;
        }
        public void Save(BlockModel[] fullBlockChain)
        {
            string json = JsonConvert.SerializeObject(fullBlockChain);
            File.WriteAllText(BlockchainPath, json);
        }

        public BlockModel[] GetAllBlocks()
        {
            int tries = 5;
            int counter = 0;
            while (counter < tries)
            {
                try
                {
                    string blockchainJson = File.ReadAllText(BlockchainPath);
                    if (!String.IsNullOrEmpty(blockchainJson))
                    {
                        BlockModel[] result = JsonConvert
                        .DeserializeObject<BlockModel[]>(blockchainJson)
                        .OrderByDescending(b => b.Height).ToArray();
                        return result;
                    }
                    break;
                }
                catch (Exception)
                {
                    counter++;
                    Thread.Sleep(200);
                }
            }


            return null;
        }

        public List<TransactionModel> GetMempool()
        {
            string mempoolPath = Helpers.GetMempooFile();
            string mempoolFile = File.ReadAllText(mempoolPath);

            if (String.IsNullOrEmpty(mempoolFile) ||
                mempoolFile == "[]")
            {
                return new List<TransactionModel>();
            }
            else
            {
                return JsonConvert.DeserializeObject<TransactionModel[]>(mempoolFile).ToList();
            }
        }

        public BlockModel GetBlockByHash(string hash)
        {
            string blockchainJson = File.ReadAllText(BlockchainPath);

            if (String.IsNullOrEmpty(blockchainJson))
                return null;

            BlockModel[] blocks = JsonConvert
               .DeserializeObject<BlockModel[]>(blockchainJson);
            var result = blocks.Where(b => b.Hash == hash);

            if (result.Count() == 0)
                return null;
            else
                return result.ToArray()[0];
        }

        public decimal GetWalletBalanceFor(string publicKey)
        {
            decimal result = 0;
            string acctSet = File.ReadAllText(AccountSetPath);
            AccountModel[] accounts = JsonConvert.DeserializeObject<AccountModel[]>(acctSet);

            if (accounts == null)
                return result;

            foreach (var account in accounts)
            {
                if (account.Address == publicKey)
                {
                    result = Convert.ToDecimal(account.Amount);
                    break;
                }
            }

            string mempoolFile = File.ReadAllText(MempoolPath);

            if (String.IsNullOrEmpty(mempoolFile) || mempoolFile == "[]")
                return result;

            TransactionModel[] mempool = JsonConvert.DeserializeObject<TransactionModel[]>(mempoolFile);
            foreach (var tx in mempool)
            {
                if (tx.Input.FromAddress == publicKey)
                {
                    result -= Convert.ToDecimal(tx.Input.Amount);
                    result -= Convert.ToDecimal(tx.Fee);

                }
            }

            return result;
        }

        public BlockModel GetBlockHeight(int height)
        {
            BlockModel result = null;
            string blockchainJson = File.ReadAllText(BlockchainPath);
            BlockModel[] blocks = JsonConvert
               .DeserializeObject<BlockModel[]>(blockchainJson);
            for (int i = 0; i < blocks.Length; i++)
            {
                if (height == blocks[i].Height)
                {
                    result = blocks[i];
                    break;
                }
            }
            return result;
        }

        public BlockModel[] GetLastBlocks(int count)
        {
            BlockModel[] result = null;
            string blockchainJson = File.ReadAllText(BlockchainPath);
            BlockModel[] blocks = JsonConvert
               .DeserializeObject<BlockModel[]>(blockchainJson);
            BlockModel[] orderedBlocks = blocks.OrderByDescending(b => b.Height).ToArray();
            List<BlockModel> tempList = new List<BlockModel>();

            for (int i = 0; i < count; i++)
                tempList.Add(orderedBlocks[i]);

            result = tempList.ToArray();
            return result;
        }

        public bool SaveMinedBlock(BlockModel block)
        {
            bool result = true;
            block = getMinedTransactions(block);
            removeAllMinedTXs(block);
            string blockJson = getNewBlockChain(block, BlockchainPath);
            Helpers.WeHaveReceivedNewBlock = true;
            File.WriteAllText(BlockchainPath, blockJson);
            UpdateAccountBalances();
            return result;
        }

        public List<string> GetBlockHashList()
        {
            List<string> result = null;
            var allBlocks = GetAllBlocks();
            if (allBlocks != null)
            {
                result = allBlocks.Select(b => b.Hash).ToList();
            }
            return result;
        }

        public bool SaveRecievedBlock(BlockModel block)
        {
            bool result = true;
            int counter = 0;
            int maxTries = 5;
            while (counter < maxTries)
            {
                try
                {
                    counter++;
                    removeAllMinedTXs(block);
                    Helpers.WeHaveReceivedNewBlock = true;
                    string blockJson = getNewBlockChain(block, BlockchainPath);
                    File.WriteAllText(BlockchainPath, blockJson);
                    UpdateAccountBalances();
                    break;
                }
                catch (Exception ex)
                {
                    if (counter == maxTries)
                    {
                        Helpers.ReadException(ex);
                        break;
                    }
                }
            }
            return result;
        }

        public bool SaveToMempool(TransactionModel tx)
        {
            try
            {
                string oldMempoolFile = File.ReadAllText(MempoolPath);
                TransactionModel[] txs;
                if (!String.IsNullOrEmpty(oldMempoolFile) &&
                    oldMempoolFile != "[]")
                {
                    List<TransactionModel> txList =
                       JsonConvert
                       .DeserializeObject<TransactionModel[]>(oldMempoolFile)
                       .ToList();
                    List<string> savedTxHashes = txList.Select(e => e.TransactionId).ToList();
                    if (!savedTxHashes.Contains(tx.TransactionId))
                    {
                        txList.Add(tx);
                        txs = txList.ToArray();
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    txs = new TransactionModel[] { tx };
                }
                string mempool = JsonConvert.SerializeObject(txs, Formatting.Indented);
                File.WriteAllText(MempoolPath, mempool);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public bool UpdateAccountBalances()
        {
            try
            {
                string fileText = File.ReadAllText(BlockchainPath);
                BlockModel[] blocks = JsonConvert.DeserializeObject<BlockModel[]>(fileText);
                var acctDictionary = sumBlockActivity(blocks);
                saveToACCTSet(acctDictionary);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private Dictionary<string, string> sumBlockActivity(BlockModel[] blocks)
        {
            List<BlockModel> blockList = (from b in blocks
                                          orderby b.Height
                                          descending
                                          select b).ToList();

            Dictionary<string, BlockModel> blockDic = new Dictionary<string, BlockModel>();
            foreach (var b in blocks)
            {
                if (!blockDic.ContainsKey(b.Hash))
                    blockDic.Add(b.Hash, b);
            }

            BlockModel currentBlock = blockList[0];
            Dictionary<string, decimal> accountSet = new Dictionary<string, decimal>();
            while (true)
            {
                accountSet = addCoinbase(accountSet, currentBlock.Coinbase);
                accountSet = addTranactions(accountSet, currentBlock.Transactions);

                if (currentBlock.PreviousHash == null)
                    break;
                else currentBlock = blockDic[currentBlock.PreviousHash];
            }

            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (var acct in accountSet)
            {
                string amount = Helpers.FormatDigits(acct.Value);
                string address = acct.Key;
                result.Add(address, amount);
            }
            return result;
        }

        private Dictionary<string, decimal> addCoinbase(Dictionary<string, decimal> result, CoinbaseModel coinbase)
        {
            string address = coinbase.Output.ToAddress;
            decimal coinbaseReward = Convert.ToDecimal(coinbase.Output.Amount);
            decimal feeReward = Convert.ToDecimal(coinbase.FeeReward);
            decimal totalReward = coinbaseReward + feeReward;

            if (result.ContainsKey(address))
                result[address] += totalReward;
            else
                result.Add(address, totalReward);

            return result;
        }

        private Dictionary<string, decimal> addTranactions(
                Dictionary<string, decimal> result,
                TransactionModel[] transactions
            )
        {

            foreach (var tx in transactions)
            {
                string inputAddress = tx.Input.FromAddress;
                decimal inputAmount = Convert.ToDecimal(tx.Input.Amount);
                decimal fee = Convert.ToDecimal(tx.Fee);
                decimal totalInputDeduct = inputAmount + fee;
                if (result.ContainsKey(inputAddress))
                    result[inputAddress] -= totalInputDeduct;
                else
                    result.Add(inputAddress, totalInputDeduct * -1);


                string outputAddress = tx.Output.ToAddress;
                decimal outputAmount = Convert.ToDecimal(tx.Output.Amount);
                if (result.ContainsKey(outputAddress))
                    result[outputAddress] += outputAmount;
                else
                    result.Add(outputAddress, outputAmount);
            }

            return result;
        }

        private void saveToACCTSet(Dictionary<string, string> acctDictionary)
        {
            List<AccountModel> acctSet = new List<AccountModel>();
            foreach (var item in acctDictionary)
            {
                AccountModel account = new AccountModel()
                {
                    Address = item.Key,
                    Amount = item.Value
                };
                acctSet.Add(account);
            }

            AccountModel[] accountSetArray = acctSet.ToArray();
            string newFile = JsonConvert.SerializeObject(accountSetArray, Formatting.Indented);
            string filePath = Helpers.GetAcctSetFile();
            File.WriteAllText(filePath, newFile);
        }

        private string getNewBlockChain(BlockModel block, string blockchainPath)
        {
            string prevJson = File.ReadAllText(BlockchainPath);
            if (!String.IsNullOrEmpty(prevJson))
            {
                BlockModel[] prevBlockchain = JsonConvert
                .DeserializeObject<BlockModel[]>(prevJson);
                List<BlockModel> result = new List<BlockModel>();
                result.Add(block);

                foreach (var prevBlock in prevBlockchain)
                    result.Add(prevBlock);

                result = result.OrderByDescending(b => b.Height).ToList();
                return JsonConvert.SerializeObject(result.ToArray(), Formatting.Indented);
            }
            else
            {
                BlockModel[] result = new BlockModel[] { block };
                return JsonConvert.SerializeObject(result, Formatting.Indented);
            }
        }

        private void removeAllMinedTXs(BlockModel block)
        {
            string mempoolJson = File.ReadAllText(MempoolPath);

            if (block.Transactions == null)
                return;

            List<TransactionModel> mempoolList;

            if (String.IsNullOrEmpty(mempoolJson) ||
                mempoolJson == "[]")
                mempoolList = new List<TransactionModel>();
            else
                mempoolList = JsonConvert
                   .DeserializeObject<TransactionModel[]>(mempoolJson)
                   .ToList();

            List<string> blockTxIds = block.Transactions.Select(t => t.TransactionId).ToList();
            for (int i = 0; i < blockTxIds.Count; i++)
            {
                string bTxId = blockTxIds[i];

                var mempoolTx = mempoolList
                   .Where(t => String.Equals(t.TransactionId, bTxId))
                   .ToList();

                if (mempoolTx.Count == 1)
                    mempoolList.Remove(mempoolTx[0]);
            }

            string result = JsonConvert.SerializeObject(mempoolList.ToArray());
            File.WriteAllText(MempoolPath, result);
        }

        private BlockModel getMinedTransactions(BlockModel block)
        {
            string mempoolJson = File.ReadAllText(MempoolPath);
            List<TransactionModel> mempoolList;

            if (String.IsNullOrEmpty(mempoolJson) ||
                mempoolJson == "[]")
                mempoolList = new List<TransactionModel>();
            else
                mempoolList = JsonConvert
                   .DeserializeObject<TransactionModel[]>(mempoolJson)
                   .ToList();

            block.Transactions = mempoolList.ToArray();
            return block;
        }
    }
}
