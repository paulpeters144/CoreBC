using CoreBC.BlockModels;
using CoreBC.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace CoreBC
{
   class PlayScenario
   {
      public string Path { get; set; }
      public void Play()
      {
         Path = Program.FilePath;
         CreateTx();
      }

      public void CreateTx()
      {
         var senderKey = new DactylKey("paulp");
         var recKey = new DactylKey("paulp1");
         string recPubKey = recKey.GetPubKeyString();
         Random rand = new Random();

         for (int i = 0; i < 5; i++)
         {
            int num = rand.Next(1000000);
            decimal amount = num * .0001M;
            var tx = senderKey.SendMoneyTo(recPubKey, amount);
            if (tx != null)
               new BlockchainRecord().SaveToMempool(tx);
         }
      }

      public void MineMempool()
      {
         string filePath = Helpers.GetBlockDir() + Helpers.GetNexBlockFileName();
         var minerKey = new DactylKey("paulp");

         string mostRecentFile = Directory
            .GetFiles($"{Program.FilePath}\\Blockchain\\Blocks")
            .OrderByDescending(a => a).ToList()[0];

         BlockModel previousBlock = getLastestBlockFrom(mostRecentFile);

         long currentTime = Helpers.GetCurrentUTC();
         Int64 height = previousBlock.Height + 1;

         CoinbaseModel coinbase = new CoinbaseModel
         {
            TransactionId = Helpers.GetSHAStringFromString(
                  $"{height}_belongs_to_{minerKey.GetPubKeyString()}"
               ),
            Output = new Output() 
            { 
               Amount = Helpers.FormatDactylDigits(300), 
               ToAddress = minerKey.GetPubKeyString()
            },
         };

         List<TransactionModel> memPool = getMemPool();

         string[] mempoolTransactions = 
            memPool.Select(x => x.TransactionId).Prepend(coinbase.TransactionId).ToArray();
         
         string merkleRoot = getMerkleFrom(mempoolTransactions);
         var nextBlock = new BlockModel()
         {
            PreviousHash = previousBlock.Hash,
            Confirmations = 0,
            TransactionCount = memPool.Count,
            Height = height,
            MerkleRoot = merkleRoot,
            TXs = mempoolTransactions,
            Time = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
            Difficulty = Helpers.GetDifficulty(),
            Coinbase = coinbase
         };

         Miner miner = new Miner();
         nextBlock = miner.Mine(nextBlock);
         nextBlock.Coinbase.BlockHash = nextBlock.Hash;

         BlockchainRecord blockchainRecord = new BlockchainRecord();
         blockchainRecord.SaveNewBlock(nextBlock);
         //blockchainRecord.UpdateFileMetaData(filePath);
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

      private List<TransactionModel> getMemPool()
      {
         List<TransactionModel> result = new List<TransactionModel>();
         string mempoolPath = $"{Program.FilePath}\\Blockchain\\Mempool\\mempool.json";
         
         if (!File.Exists(mempoolPath))
            return result;
         
         string mempoolJson = File.ReadAllText(mempoolPath);
         JObject mempoolObj = JObject.Parse(mempoolJson);
         foreach (var tx in mempoolObj)
         {
            TransactionModel txModel = new TransactionModel()
            {
               TransactionId = tx.Key,
               Signature = tx.Value["Signature"].ToString(),
               Locktime = Convert.ToInt64(tx.Value["Locktime"]),
               Input = new Input() 
               {
                  FromAddress = tx.Value["Input"]["FromAddress"].ToString(),
                  Amount = tx.Value["Input"]["Amount"].ToString()
               },
               Output = new Output() 
               {
                  ToAddress = tx.Value["Output"]["ToAddress"].ToString(),
                  Amount = tx.Value["Output"]["Amount"].ToString()
               },
               Fee = tx.Value["Fee"].ToString()
            };
            result.Add(txModel);
         }

         return result;
      }

      private BlockModel getLastestBlockFrom(string mostRecentFile)
      {
         string json = File.ReadAllText(mostRecentFile);
         JObject blockchainObj = JObject.Parse(json);
         string blockHash = string.Empty;
         BlockModel resultBlock = new BlockModel();
         resultBlock.Height = -1;
         foreach (var block in blockchainObj)
         {
            if (block.Key.ToLower() == "heightrange")
               continue;
            
            blockHash = block.Key;
            int currentBlockHeight = Convert.ToInt32(block.Value["Height"]);
            if (resultBlock.Height < currentBlockHeight)
            {
               resultBlock.Hash = block.Key;
               resultBlock.Height = currentBlockHeight;
            }
         }
         return resultBlock;
      }
   }
}
