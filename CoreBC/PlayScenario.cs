using CoreBC.BlockModels;
using CoreBC.DataAccess;
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
   public class PlayScenario
   {
      public string Path { get; set; }
      public DBAccess DB { get; set; }
      public PlayScenario()
      {
         Path = Program.FilePath;
         DB = new DBAccess();
      }

      public void CreateTx()
      {
         var senderKey = new ChainKeys("paulp");
         var recKey = new ChainKeys("paulp1");
         string recPubKey = recKey.GetPubKeyString();
         Random rand = new Random();

         for (int i = 0; i < 5; i++)
         {
            int num = rand.Next(10000);
            decimal amount = num * .0001M;
            var tx = senderKey.SendMoneyTo(recPubKey, amount);
            if (tx != null)
               DB.SaveToMempool(tx);
         }
      }

      public void MineMempool()
      {
         string filePath = Helpers.GetBlockchainFilePath();
         var minerKey = new ChainKeys("paulp");

         BlockModel previousBlock = getLastestBlockFrom(filePath);

         long currentTime = Helpers.GetCurrentUTC();
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

         List<TransactionModel> memPool = getMemPool();

         string[] allTransactions = 
            memPool.Select(x => x.TransactionId).Prepend(coinbase.TransactionId).ToArray();
         
         string merkleRoot = getMerkleFrom(allTransactions);
         var nextBlock = new BlockModel()
         {
            PreviousHash = previousBlock.Hash,
            Confirmations = 0,
            TransactionCount = allTransactions.Length,
            Height = height,
            MerkleRoot = merkleRoot,
            TXs = allTransactions,
            Time = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
            Difficulty = Helpers.GetDifficulty(),
            Coinbase = coinbase
         };

         //Miner miner = new Miner();
         //nextBlock = miner.Mine(nextBlock, 10000000);
         //nextBlock.Coinbase.BlockHash = nextBlock.Hash;
         //BlockChecker blockChecker = new BlockChecker();
         //blockChecker.ConfirmPriorBlocks();
         //DB.SaveMinedBlock(nextBlock);
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

      private BlockModel getLastestBlockFrom(string mostRecentFile)
      {
         string json = File.ReadAllText(mostRecentFile);
         BlockModel[] blocks = JsonConvert.DeserializeObject<BlockModel[]>(json);
         blocks = (from b in blocks
                   orderby b.Height
                   descending
                   select b).ToArray();
         BlockModel topBlock = blocks[0];
         return topBlock;
      }
   }
}
