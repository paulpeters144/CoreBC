using CoreBC.BlockModels;
using CoreBC.DataAccess;
using CoreBC.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CoreBC.Utils
{

   class BlockChecker
   {
      private ChainKeys ChainKeys { get; set; }
      private IDataAccess DB;
      public bool FullChainConfirmed { get; internal set; }
      public string MainFileDestination { get; set; }

      public BlockChecker()
      {
         ChainKeys = new ChainKeys(Program.UserName);
         DB = new BlockChainFiles();
      }

      public void ConfirmPriorBlocks()
      {
         BlockModel[] fullBlockChain = DB.GetAllBlocks();
         for (int i = 0; i < fullBlockChain.Length; i++)
         {
            var nextBlock = fullBlockChain[i];
            if (HeaderIsGood(nextBlock) && 
               TransactionAreTamperFree(nextBlock))
               nextBlock.Confirmations += 1;
         }
         DB.Save(fullBlockChain);
      }

      public bool ConfirmEntireBlock(BlockModel block)
      {
         if (!HeaderIsGood(block))
            return false;

         if (!TransactionAreTamperFree(block))
            return false;

         return true;
      }

      public bool HeaderIsGood(BlockModel block)
      {
         string prevHash = block.PreviousHash;
         var txArray = block.TXs;
         string difficulty = block.Difficulty;
         string time = block.Time.ToString();
         var nonce = block.Nonce;
         string answer = string.Empty;

         if (block.PreviousHash != null)
         {
            string merkleRoot = string.Empty;
            if (txArray.Length > 1)
               merkleRoot = getMerkleFrom(txArray);
            else
               merkleRoot = Helpers.GetSHAStringFromString(block.Coinbase.TransactionId);
            answer = $"{prevHash}{merkleRoot}{time}{difficulty}{nonce}";
         }
         else
         {

            string merkleRoot = Helpers.GetSHAStringFromString(block.Coinbase.TransactionId);
            answer = $"{merkleRoot}{time}{difficulty}{nonce}";
         }
         
         string hashFromData = Helpers.GetSHAStringFromString(answer);
         string hashFromBlock = block.Hash;

         if (!String.Equals(hashFromData, hashFromBlock))
            return false;

         if (block.TXs.Length != block.TransactionCount)
            return false;

         return true;
      }

      public bool TransactionAreTamperFree(BlockModel block)
      {
         if (block.Transactions != null)
         {
            if (block.Transactions.Length + 1 != block.TXs.Length)
               return false;
         }

         if (!coinbaseIsGood(block))
            return false;

         if (block.Transactions == null)
            return true;

         foreach (var tx in block.Transactions)
         {
            var txIsGood = ChainKeys.VerifyTransaction(tx);
            var txCopy = ChainKeys.CreateTransactionId(tx);
            bool txIdIsCorrect = String.Equals(tx.TransactionId, txCopy.TransactionId);

            if (!txIsGood || !txIdIsCorrect)
               return false;
         }

         return true;
      }

      private bool coinbaseIsGood(BlockModel block)
      {
         long blockHeight = block.Height;
         string toAddress = block.Coinbase.Output.ToAddress;
         string coinbaseSig = $"{blockHeight}_belongs_to_{toAddress}";
         string checkId = Helpers.GetSHAStringFromString(coinbaseSig);
         string cbTxId = block.Coinbase.TransactionId;
         return checkId == cbTxId;
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
