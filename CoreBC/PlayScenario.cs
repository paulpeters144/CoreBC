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

         //for (int i = 0; i < 500; i++)
         {
            int num = rand.Next(1000000);
            decimal amount = num * .000001M;
            var tx = senderKey.SendMoneyTo(recPubKey, 3);
            if (tx != null)
               new BlockchainRecord().SaveToMempool(tx);
            else
            {

            }
         }
      }

      public bool CheckBlock(int height)
      {
         string filePath = Program.FilePath + "\\Blockchain\\Blocks\\DactylBlocks_20210604.json";
         string allText = File.ReadAllText(filePath);
         BlockerChecker blockerChecker = new BlockerChecker(allText);
         blockerChecker.FullExamination();
         return false;
      }

      public void MineMempool()
      {
         string filePath = Helpers.GetBlockDir() + Helpers.GetNexBlockFileName();
         new BlockchainRecord().UpdateFileMetaData(filePath);
         var minerKey = new DactylKey("paulp");

         string mostRecentFile = Directory
            .GetFiles($"{Program.FilePath}\\Blockchain\\Blocks")
            .OrderByDescending(a => a).ToList()[0];

         BlockModel previousBlock = getLastestBlockFrom(mostRecentFile);

         long currentTime = Helpers.GetCurrentUTC();
         Int64 height = previousBlock.Height + 1;

         CoinbaseModel coinbase = new CoinbaseModel
         {
            TransactionId = Helpers.GetSHAStringFromString($"{height}_belongs_to_{minerKey.GetPubKeyString()}"),
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
            Time = 1622766012,
            Difficulty = Helpers.GetDifficulty(),
            Coinbase = coinbase
         };

         Miner miner = new Miner();
         nextBlock = miner.Mine(nextBlock);
         nextBlock.Coinbase.BlockHash = nextBlock.Hash;
         nextBlock.Coinbase.TransactionId = 
            Helpers.GetSHAStringFromString($"{nextBlock.Height}{nextBlock.MerkleRoot}");
         new BlockchainRecord().SaveNewBlock(nextBlock);
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
         long blockHeight = -1;
         foreach (var block in blockchainObj)
         {
            blockHash = block.Key;
            long currentBlockHeight = 0;
            if (blockHeight < currentBlockHeight)
            {
               blockHash = block.Key;
               blockHeight = currentBlockHeight;
            }
         }
         BlockModel resultBlock = new BlockModel()
         {
            Hash = blockHash,
            Confirmations = Convert.ToInt64(blockchainObj["Confirmation"]),
            TransactionCount = Convert.ToInt64(blockchainObj["TransactionCount"]),
         };
         return resultBlock;
      }
      public void CreateGenesisBlock()
      {
         DactylKey dactylKey = new DactylKey("paulp");
         string pubKey = dactylKey.GetPubKeyString();
         CoinbaseModel coinbase = new CoinbaseModel
         {
            Output = new Output
            {
               ToAddress = pubKey,
               Amount = Helpers.FormatDactylDigits(300)
            }
         };

         string genesisCoinbase = $"genesis_coinbase_{coinbase.Output.ToAddress}_gets_{coinbase.Output.Amount}";
         byte[] genesisCBBytes = Encoding.UTF8.GetBytes(genesisCoinbase);
         byte[] shawedCoinbase = SHA256.Create().ComputeHash(genesisCBBytes);
         string coinbaseTxId = Helpers.GetSHAStringFromBytes(shawedCoinbase);

         coinbase.TransactionId = coinbaseTxId;

         byte[] coinbaseShawed = Encoding.UTF8.GetBytes(coinbaseTxId);
         string merkelRoot = Helpers.GetSHAStringFromBytes(SHA256.Create().ComputeHash(coinbaseShawed));

         var dateTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
         GenesisBlockModel genesisBlock = new GenesisBlockModel
         {
            Confirmations = 0,
            TransactionCount = 1,
            Height = 0,
            Difficulty = "0000000",
            TXs = new string[] { coinbaseTxId },
            MerkleRoot = merkelRoot,
         };

         Miner miner = new Miner();
         var answer = miner.Mine(genesisBlock);
         genesisBlock.Nonce = Convert.ToInt64(answer["Nonce"]);
         genesisBlock.Time = Convert.ToInt64(answer["Time"]);
         string checkAnswer =
            genesisBlock.MerkleRoot +
            genesisBlock.Time +
            genesisBlock.Difficulty +
            genesisBlock.Nonce;
         byte[] answerBytes = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(checkAnswer));

         string officialGenesisBlockHash = Helpers.GetSHAStringFromBytes(answerBytes);
         genesisBlock.Hash = officialGenesisBlockHash;

         if (officialGenesisBlockHash.StartsWith(genesisBlock.Difficulty))
         {
            coinbase.BlockHash = officialGenesisBlockHash;
            genesisBlock.Coinbase = coinbase;
            new BlockchainRecord().SaveNewBlock(genesisBlock);
         }

         updateACCTs();
      }

      private static void updateACCTs()
      {
         AccountUpdater acctUpdater = new AccountUpdater();
         acctUpdater.RunUpdate();
      }
   }
}
