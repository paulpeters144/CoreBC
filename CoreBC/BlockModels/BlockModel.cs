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
}
