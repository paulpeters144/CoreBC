using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace CoreBC.BlockModels
{
   public class BlockModel
   {
      public string Hash { get; set; }
      public string PreviousHash { get; set; }
      public Int64 Confirmations { get; set; }
      public Int64 TransactionCount { get; set; }
      public Int64 Height { get; set; }
      public string MerkleRoot { get; set; }
      public string[] TXs { get; set; }
      public Int64 Time { get; set; }
      public Int64 Nonce { get; set; }
      public string Difficulty { get; set; }
      public CoinbaseModel Coinbase { get; set; }
      public TransactionModel[] Transactions { get; set; }
   }

   public class BlockChainModel 
   {
      BlockModel[] Blocks { get; set; }
      public string AddBlockToChain(BlockModel block)
      {
         BlockModel[] blockArray = new BlockModel[] { block };
         string result = JsonConvert.SerializeObject(blockArray, Formatting.Indented);
         return result;
      }
   }

   public class AccountModel
   {
      public string Address { get; set; }
      public string Amount { get; set; }
   }
}
