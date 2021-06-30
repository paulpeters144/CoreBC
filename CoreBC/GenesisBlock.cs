using CoreBC.BlockModels;
using CoreBC.DataAccess;
using CoreBC.Utils;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace CoreBC
{
   class GenesisBlock
   {
      public IDataAccess DB;
      public GenesisBlock()
      {
         DB = new BlockChainFiles(
               Helpers.GetBlockchainFilePath(),
               Helpers.GetMempooFile(),
               Helpers.GetAcctSetFile()
            );
      }
      public void Generate()
      {
         ChainKeys dactylKey = new ChainKeys("paulp");
         string pubKey = dactylKey.GetPubKeyString();
         CoinbaseModel coinbase = new CoinbaseModel
         {
            Output = new Output
            {
               ToAddress = pubKey,
               Amount = Helpers.FormatDigits(Helpers.GetMineReward(0))
            }
         };

         string genesisCoinbase = $"{0}_belongs_to_{pubKey}";
         string coinbaseTxId = Helpers.GetSHAStringFromString(genesisCoinbase);
         coinbase.TransactionId = coinbaseTxId;
         string merkelRoot = Helpers.GetSHAStringFromString($"{coinbaseTxId}{coinbaseTxId}");

         BlockModel genesisBlock = new BlockModel
         {
            Confirmations = 0,
            TransactionCount = 1,
            Height = 0,
            Difficulty = Helpers.GetDifficulty(),
            TXs = new string[] { coinbaseTxId },
            MerkleRoot = merkelRoot,
            Time = Helpers.GetCurrentUTC()
         };

         Miner miner = new Miner();
         genesisBlock = miner.MineGBlock(genesisBlock);
         string checkAnswer =
            genesisBlock.MerkleRoot +
            genesisBlock.Time +
            genesisBlock.Difficulty +
            genesisBlock.Nonce;
         byte[] answerBytes = SHA256.Create().ComputeHash(Encoding.ASCII.GetBytes(checkAnswer));

         string officialGenesisBlockHash = Helpers.GetSHAStringFromBytes(answerBytes);
         genesisBlock.Hash = officialGenesisBlockHash;

         if (officialGenesisBlockHash.StartsWith(genesisBlock.Difficulty))
         {
            coinbase.BlockHash = officialGenesisBlockHash;
            genesisBlock.Coinbase = coinbase;
            DB.SaveBlock(genesisBlock);
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
   }
}
