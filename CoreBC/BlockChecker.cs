using CoreBC.BlockModels;
using CoreBC.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CoreBC
{

   class BlockChecker
   {
      private DactylKey DactylKey { get; set; }
      public bool FullChainConfirmed { get; internal set; }
      public string MainFileDestination { get; set; }

      public BlockChecker()
      {
         DactylKey = new DactylKey(Program.UserName);
      }

      public void ConfirmPriorBlocks()
      {
         string[] files = Directory.GetFiles(Helpers.GetBlockDir(), "*.json");
         foreach (var file in files)
         {
            string allText = File.ReadAllText(file);
            string confirmedFile = confirmBlocksInFile(allText);
            File.WriteAllText(file, confirmedFile);
         }
      }

      private string confirmBlocksInFile(string allText)
      {
         JObject fileObj = JObject.Parse(allText);
         foreach (var block in fileObj)
         {
            if (block.Key.ToLower() == "heightrange")
               continue;
            fileObj[block.Key]["Confirmations"] =
               Convert.ToInt32(fileObj[block.Key]["Confirmations"]) + 1;
         }
            
         return fileObj.ToString(Newtonsoft.Json.Formatting.Indented);
      }

      public bool SaysHeaderIsGood(BlockModel block)
      {
         string prevHash = block.PreviousHash;
         var txArray = block.TXs;
         string merkleRoot = getMerkleFrom(txArray);
         string difficulty = block.Difficulty;
         string time = block.Time.ToString();
         var nonce = block.Nonce;
         string answer = $"{prevHash}{merkleRoot}{time}{difficulty}{nonce}";
         string hashFromData = Helpers.GetSHAStringFromString(answer);
         string hashFromBlock = block.Hash;

         if (!String.Equals(hashFromData, hashFromBlock))
            return false;

         if (block.TXs.Length - 1 != block.TransactionCount)
            return false;

         if (!txAreTamperFree(block))
            return false;

         return true;
      }

      public bool TransactionAreTamperFree(string blockJson, string hash)
      {
         var txs = JObject.Parse(blockJson)[hash];
         int txCount = Convert.ToInt32(txs["TransactionCount"]);
         var txArray = txs["TXs"].ToArray();

         if (txCount != txArray.Length - 1)
            return false;

         if (!coinbaseIsGood(txs))
            return false;

         foreach (var txHash in txArray)
         {
            var txObj = txs["Transactions"];
            string txId = txHash.ToString();

            string cbId = txs["Coinbase"]["TransactionId"].ToString();
            if (txId == cbId)
               continue;

            var transaction = txObj[txId];
            TransactionModel txModel = new TransactionModel
            {
               TransactionId = txId,
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

      private bool coinbaseIsGood(JToken txs)
      {
         int blockHeight = Convert.ToInt32(txs["Height"]);
         string toAddress = txs["Coinbase"]["Output"]["ToAddress"].ToString();
         string coinbaseSig = $"{blockHeight}_belongs_to_{toAddress}";
         string checkId = Helpers.GetSHAStringFromString(coinbaseSig);
         string cbTxId = txs["Coinbase"]["TransactionId"].ToString();
         return cbTxId == checkId;
      }

      private bool txAreTamperFree(BlockModel block)
      {
         foreach (var item in block.TXs)
         {

         }
         // JObject txs = (JObject)BlockObj["Transactions"];
         


         return true;
      }

      public bool SaysIsGood(GenesisBlockModel block)
      {
         var txArray = block.TXs;
         string merkleRoot = getMerkleFrom(txArray);
         string difficulty = block.Difficulty;
         string time = block.Time.ToString();
         var nonce = block.Nonce;
         string answer = $"{merkleRoot}{time}{difficulty}{nonce}";
         string hashFromData = Helpers.GetSHAStringFromString(answer);
         string hashFromBlock = block.Hash;

         if (!String.Equals(hashFromData, hashFromBlock))
            return false;

         if (block.TXs.Length != block.TransactionCount)
            return false;

         return true;
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

      //private bool tranactionCountIsCorrect()
      //{
      //   int metaHeight = Convert.ToInt32(BlockObj["TransactionCount"]);
      //   JArray txIdArray = (JArray)BlockObj["TXs"];
      //   if (txIdArray.Count - 1 == metaHeight)
      //      return true;

      //   return false;
      //}

      //private bool transactionsAreTamperFree()
      //{
      //   JObject txs = (JObject)BlockObj["Transactions"];
      //   foreach (var tx in txs)
      //   {
      //      var transaction = tx.Value;
      //      TransactionModel txModel = new TransactionModel
      //      {
      //         TransactionId = tx.Key,
      //         Signature = transaction["Signature"].ToString(),
      //         Input = new Input()
      //         {
      //            FromAddress = transaction["Input"]["FromAddress"].ToString(),
      //            Amount = transaction["Input"]["Amount"].ToString()
      //         },
      //         Output = new Output
      //         {
      //            ToAddress = transaction["Output"]["ToAddress"].ToString(),
      //            Amount = transaction["Output"]["Amount"].ToString()
      //         },
      //         Locktime = Convert.ToInt64(transaction["Locktime"]),
      //         Fee = transaction["Fee"].ToString()
      //      };
      //      var txIsGood = DactylKey.VerifyTransaction(txModel);
      //      var txCopy = DactylKey.CreateTransactionId(txModel);
      //      bool txIdIsCorrect = String.Equals(txModel.TransactionId, txCopy.TransactionId);

      //      if (!txIsGood || !txIdIsCorrect)
      //         return false;
      //   }
      //   return true;
      //}
   }
}
