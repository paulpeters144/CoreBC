using CoreBC.BlockModels;
using CoreBC.DataAccess;
using CoreBC.P2PLib;
using CoreBC.Utils;
using System;
using System.Collections.Generic;

namespace CoreBC
{
   class GenesisBlock
   {
      public DBAccess DB;
      public P2PNetwork P2PNetwork;
      public GenesisBlock(P2PNetwork p2pNetwork)
      {
         P2PNetwork = p2pNetwork;

         DB = new DBAccess(
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
            },
            FeeReward = Helpers.FormatDigits(0)
        };

         string genesisCoinbase = $"{0}_belongs_to_{pubKey}";
         string coinbaseTxId = Helpers.GetSHAStringFromString(genesisCoinbase);
         coinbase.TransactionId = coinbaseTxId;
         string merkelRoot = Helpers.GetSHAStringFromString($"{coinbaseTxId}");

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

         genesisBlock = mine(genesisBlock);
         string checkAnswer =
            genesisBlock.MerkleRoot +
            genesisBlock.Time +
            genesisBlock.Difficulty +
            genesisBlock.Nonce;

         string officialGenesisBlockHash = Helpers.GetSHAStringFromString(checkAnswer);
         genesisBlock.Hash = officialGenesisBlockHash;
         if (officialGenesisBlockHash.StartsWith(genesisBlock.Difficulty))
         {
            coinbase.BlockHash = officialGenesisBlockHash;
            genesisBlock.Coinbase = coinbase;
            DB.SaveMinedBlock(genesisBlock);
         }
      }

        private BlockModel mine(BlockModel genesisBlock)
        {
            string mRoot = genesisBlock.MerkleRoot;
            string difficulty = genesisBlock.Difficulty;
            string time = genesisBlock.Time.ToString();
            Int64 nonce = 0;
            for (; ; )
            {
                string attempt = $"{mRoot}{time}{difficulty}{nonce}";
                string hashAttemp = Helpers.GetSHAStringFromString(attempt);

                if (hashAttemp.StartsWith(genesisBlock.Difficulty))
                {
                    genesisBlock.Nonce = nonce;
                    break;
                }
                nonce++;
            }
            Console.WriteLine($"Genesis block mined: {genesisBlock.Hash}");
            return genesisBlock;
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
