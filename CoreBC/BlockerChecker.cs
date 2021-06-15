using CoreBC.BlockModels;
using CoreBC.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreBC
{

   class BlockerChecker
   {
      public string BlockReport { get; set; }
      private JObject BlockObj { get; set; }
      private DactylKey DactylKey { get; set; }
      public BlockerChecker(string fullBlock)
      {
         BlockObj = JObject.Parse(fullBlock);
         DactylKey = new DactylKey(Program.UserName);
      }

      public bool FullExamination()
      {
         if (!blockHashIsCorrect())
            return false;

         if (!tranactionCountIsCorrect())
            return false;
         
         if (!transactionsAreTamperFree())
            return false;
         
         
         
         return false;
      }

      private bool blockHashIsCorrect()
      {
         string prevHash = BlockObj["PreviousHash"].ToString();
         var prepArray = (JArray)BlockObj["TXs"];
         var txArray = prepArray.ToList().Select(x => x.ToString()).ToArray();
         string merkleRoot = getMerkleFrom(txArray);
         string difficulty = BlockObj["Difficulty"].ToString();
         string time = BlockObj["Time"].ToString();
         var nonce = Convert.ToInt64(BlockObj["Nonce"]);
         string hashFromData = Helpers.GetSHAStringFromString($"{prevHash}{merkleRoot}{time}{difficulty}{nonce}");
         string hashFromBlock = BlockObj["Hash"].ToString();
         bool result = String.Equals(hashFromData, hashFromBlock);

         string test = "9a78e60daec784f382d9f88b37f01fe939abd3b3cd514cc0c4c3526a7fdec14f162276601200000314857";
         var s = Helpers.GetSHAStringFromString(test);

         return result;
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

      private bool tranactionCountIsCorrect()
      {
         int metaHeight = Convert.ToInt32(BlockObj["TransactionCount"]);
         JArray txIdArray = (JArray)BlockObj["TXs"];
         if (txIdArray.Count - 1 == metaHeight)
            return true;

         return false;
      }

      private bool transactionsAreTamperFree()
      {
         JObject txs = (JObject)BlockObj["Transactions"];
         foreach (var tx in txs)
         {
            var transaction = tx.Value;
            TransactionModel txModel = new TransactionModel
            {
               TransactionId = tx.Key,
               Signature = transaction["Signature"].ToString(),
               Input = new Input()
               {
                  FromAddress = transaction["Input"]["FromAddress"].ToString(),
                  Amount = transaction["Input"]["Amount"].ToString()
               },
               Output = new Output
               {
                  ToAddress = transaction["Output"]["ToAddress"].ToString(),
                  Amount = transaction["Output"]["Amount"].ToString()
               },
               Locktime = Convert.ToInt64(transaction["Locktime"]),
               Fee = transaction["Fee"].ToString()
            };
            var txIsGood = DactylKey.VerifyTransaction(txModel);
            var txCopy = DactylKey.CreateTransactionId(txModel);
            bool txIdIsCorrect = String.Equals(txModel.TransactionId, txCopy.TransactionId);

            if (!txIsGood || !txIdIsCorrect)
               return false;
         }
         return true;
      }
   }
}
