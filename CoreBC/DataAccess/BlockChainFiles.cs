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
   class BlockChainFiles : IDataAccess
   {
      public string BlockchainPath { get; set; }
      public string MempoolPath { get; set; }
      public string AccountSetPath { get; set; }

      public BlockChainFiles()
      {
         BlockchainPath = Helpers.GetBlockchainFilePath();
         MempoolPath = Helpers.GetMempooFile();
         AccountSetPath = Helpers.GetAcctSetFile();
      }
      public BlockChainFiles(string blockchainPath, string mempoolPath, string accountSetPath)
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
         BlockModel[] result = JsonConvert
            .DeserializeObject<BlockModel[]>(blockchainJson)
            .OrderByDescending(b => b.Height).ToArray();
         return result;
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

         foreach (var account in accounts)
         {
            if (account.Address == publicKey)
            {
               result = Convert.ToDecimal(account.Amount);
               break;
            }
         }

         if (!File.Exists(MempoolPath))
            File.Create(MempoolPath).Dispose();

         string mempoolFile = File.ReadAllText(MempoolPath);

         if (String.IsNullOrEmpty(mempoolFile) || mempoolFile == "[]")
            return result;

         TransactionModel[] mempool = JsonConvert.DeserializeObject<TransactionModel[]>(mempoolFile);
         foreach (var tx in mempool)
         {
            if (tx.Input.FromAddress == publicKey)
               result -= Convert.ToDecimal(tx.Input.Amount);
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

      public bool SaveBlock(BlockModel block)
      {
         bool result = true;
         if (block.PreviousHash == null)//this is for the genesis block
         {
            string json = new BlockChainModel().AddBlockToChain(block);
            File.WriteAllText(BlockchainPath, json);
            return result;
         }

         block = getMinedTransactions(block);
         removeAllMinedTXs(block);
         string blockJson = getNewBlockChain(block, BlockchainPath);
         File.WriteAllText(BlockchainPath, blockJson);
         return result;
      }

      public bool SaveToMempool(TransactionModel tx)
      {
         bool result = true;

         try
         {
            if (!File.Exists(MempoolPath))
               File.Create(MempoolPath).Dispose();

            string oldMempoolFile = File.ReadAllText(MempoolPath);
            TransactionModel[] txs;
            if (!String.IsNullOrEmpty(oldMempoolFile) &&
                oldMempoolFile != "[]")
            {
               List<TransactionModel> txList =
                  JsonConvert
                  .DeserializeObject<TransactionModel[]>(oldMempoolFile)
                  .ToList();
               txList.Add(tx);
               txs = txList.ToArray();
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
            result = false;
         }

         return result;
      }

      public bool UpdateAccountBalances()
      {
         // need to make sure we are getting the longest chain
         string fileText = File.ReadAllText(BlockchainPath);
         BlockModel[] blocks = JsonConvert.DeserializeObject<BlockModel[]>(fileText);
         var acctDictionary = sumBlockActivity(blocks);
         saveToACCTSet(acctDictionary);
         return true;
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
            string coinbaseAddress = blocks[i].Coinbase.Output.ToAddress;
            if (result.ContainsKey(coinbaseAddress))
            {
               decimal currentBalance = Convert.ToDecimal(result[coinbaseAddress]);
               string newBalance = Helpers.FormatDigits(currentBalance + coinbaseAmount);
               result[coinbaseAddress] = newBalance;
            }
            else
            {
               string newBalance = Helpers.FormatDigits(coinbaseAmount);
               result.Add(coinbaseAddress, newBalance);
            }

            if (blocks[i].Transactions == null)
               continue;

            for (int x = 0; x < blocks[i].Transactions.Length; x++)
            {
               TransactionModel tx = blocks[i].Transactions[x];
               string inputAddress = tx.Input.FromAddress;
               decimal inputAmount = Convert.ToDecimal(tx.Input.Amount);
               result[inputAddress] = Helpers.FormatDigits(
                     Convert.ToDecimal(result[inputAddress]) - inputAmount
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
         string dirPath = $"{Program.FilePath}\\Blockchain\\ACCTSet";

         if (!Directory.Exists(dirPath))
            Directory.CreateDirectory(dirPath);
         string filePath = dirPath + "\\ACCTSet.json";
         File.WriteAllText(filePath, newFile);
      }

      private string getNewBlockChain(BlockModel block, string blockchainPath)
      {
         BlockModel[] prevBlockchain = JsonConvert
            .DeserializeObject<BlockModel[]>(
               File.ReadAllText(BlockchainPath)
            );
         BlockModel[] result = new BlockModel[prevBlockchain.Length + 1];
         result[0] = block;

         for (int i = 1; i < prevBlockchain.Length + 1; i++)
            result[i] = prevBlockchain[i - 1];

         result = result.OrderByDescending(b => b.Height).ToArray();
         return JsonConvert.SerializeObject(result, Formatting.Indented);
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
