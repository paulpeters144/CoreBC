using CoreBC.BlockModels;
using CoreBC.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
            string blockchainJson = File.ReadAllText(BlockchainPath);
            if (!String.IsNullOrEmpty(blockchainJson))
            {
                BlockModel[] result = JsonConvert
                .DeserializeObject<BlockModel[]>(blockchainJson)
                .OrderByDescending(b => b.Height).ToArray();
                return result;
            }
            return null;
        }

        public BlockModel GetBlock(string hash)
        {
            BlockModel result = null;
            string blockchainJson = File.ReadAllText(BlockchainPath);
            BlockModel[] blocks = JsonConvert
               .DeserializeObject<BlockModel[]>(blockchainJson);
            for (int i = 0; i < blocks.Length; i++)
            {
                if (String.Equals(hash, blocks[i].Hash))
                {
                    result = blocks[i];
                    break;
                }
            }
            return result;
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
            if (block.PreviousHash == null) //this is for the genesis block
            {
                string json = new BlockChainModel().AddBlockToChain(block);
                File.WriteAllText(BlockchainPath, json);
                return result;
            }

            block = getMinedTransactions(block);
            block = getFeeReward(block);
            removeAllMinedTXs(block);
            string blockJson = getNewBlockChain(block, BlockchainPath);
            Helpers.WeHaveReceivedNewBlock = true;
            File.WriteAllText(BlockchainPath, blockJson);
            UpdateAccountBalances();
            return result;
        }

        private BlockModel getFeeReward(BlockModel block)
        {
            block.Coinbase.FeeReward = string.Empty;
            decimal totalFee = 0;
            foreach (var tx in block.Transactions)
            {
                try
                {
                    totalFee += Convert.ToDecimal(block.Coinbase.FeeReward);
                }
                catch (Exception)
                { }
            }
            block.Coinbase.FeeReward = Helpers.FormatDigits(totalFee);
            return block;
        }

        public List<string> GetBlockHashList() =>
            GetAllBlocks().Select(b => b.Hash).ToList();

        public bool SaveRecievedBlock(BlockModel block)
        {
            bool result = true;
            if (block.PreviousHash == null) //this is for the genesis block
            {
                string json = new BlockChainModel().AddBlockToChain(block);
                File.WriteAllText(BlockchainPath, json);
                return result;
            }

            removeAllMinedTXs(block);
            Helpers.WeHaveReceivedNewBlock = true;
            string blockJson = getNewBlockChain(block, BlockchainPath);
            File.WriteAllText(BlockchainPath, blockJson);
            UpdateAccountBalances();
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
            // need to make sure we are getting the longest chain
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


        //-=-=-=-=-=-=-=-=-=-Private Methods-=-=-=-=-=-=-=-=-=-=-=-

        private Dictionary<string, string> sumBlockActivity(BlockModel[] blocks)
        {
            blocks = (from b in blocks
                      orderby b.Height
                      ascending
                      select b).ToArray();

            Dictionary<string, string> result = new Dictionary<string, string>();
            for (int i = 0; i < blocks.Length; i++)
            {
                decimal coinbaseAmount = Convert.ToDecimal(blocks[i].Coinbase.Output.Amount);
                decimal feeReward = Convert.ToDecimal(blocks[i].Coinbase.FeeReward);
                decimal totalReward = coinbaseAmount + feeReward;
                string coinbaseAddress = blocks[i].Coinbase.Output.ToAddress;
                if (result.ContainsKey(coinbaseAddress))
                {
                    decimal currentBalance = Convert.ToDecimal(result[coinbaseAddress]);
                    string newBalance = Helpers.FormatDigits(currentBalance + totalReward);
                    result[coinbaseAddress] = newBalance;
                }
                else
                {
                    string newBalance = Helpers.FormatDigits(totalReward);
                    result.Add(coinbaseAddress, newBalance);
                }

                if (blocks[i].Transactions == null)
                    continue;

                for (int x = 0; x < blocks[i].Transactions.Length; x++)
                {
                    TransactionModel tx = blocks[i].Transactions[x];
                    string inputAddress = tx.Input.FromAddress;
                    decimal inputAmount = Convert.ToDecimal(tx.Input.Amount);
                    decimal feeAmount = Convert.ToDecimal(tx.Fee);
                    result[inputAddress] = Helpers.FormatDigits(
                          Convert.ToDecimal(result[inputAddress]) - (inputAmount + feeAmount)
                       );
                    string outputAddress = tx.Output.ToAddress;
                    decimal outputAmount = Convert.ToDecimal(tx.Output.Amount);
                    if (result.ContainsKey(outputAddress))
                    {
                        result[outputAddress] = Helpers.FormatDigits(
                              Convert.ToDecimal(result[outputAddress]) + outputAmount
                           );
                    }
                    else
                    {
                        result.Add(outputAddress, Helpers.FormatDigits(outputAmount));
                    }
                }
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
                BlockModel[] result = new BlockModel[prevBlockchain.Length + 1];
                result[0] = block;

                for (int i = 1; i < prevBlockchain.Length + 1; i++)
                    result[i] = prevBlockchain[i - 1];

                result = result.OrderByDescending(b => b.Height).ToArray();
                return JsonConvert.SerializeObject(result, Formatting.Indented);
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
