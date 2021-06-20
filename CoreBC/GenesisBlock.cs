using CoreBC.BlockModels;
using CoreBC.Utils;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace CoreBC
{
   class GenesisBlock
   {
      public void Generate()
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
         string merkelRoot = getMerkleFrom(new string[] { coinbaseTxId });

         GenesisBlockModel genesisBlock = new GenesisBlockModel
         {
            Confirmations = 0,
            TransactionCount = 1,
            Height = 0,
            Difficulty = Helpers.GetDifficulty(),
            TXs = new string[] { coinbaseTxId },
            MerkleRoot = merkelRoot,
            Time = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()
         };

         Miner miner = new Miner();
         genesisBlock = miner.Mine(genesisBlock);
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

      private static void updateACCTs()
      {
         AccountUpdater acctUpdater = new AccountUpdater();
         acctUpdater.RunUpdate();
      }
   }
}
