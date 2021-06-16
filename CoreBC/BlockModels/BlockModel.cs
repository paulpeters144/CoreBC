using System;
using System.Collections.Generic;
using System.Text;

namespace CoreBC.BlockModels
{
   public class BlockModel
   {
      public string Hash { get; set; } // shawd from 1) PreviousHash 2) merkel 3) timestamp 4) difficulty 5) nonce
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
   }

   public class GenesisBlockModel 
   {
      public string Hash { get; set; } // shawd from 1) merkel 2) timestamp 3) difficulty 4) nonce
      public Int64 Confirmations { get; set; }
      public Int64 TransactionCount { get; set; }
      public Int64 Height { get; set; }
      public string MerkleRoot { get; set; }
      public string[] TXs { get; set; }
      public Int64 Time { get; set; }
      public Int64 Nonce { get; set; }
      public string Difficulty { get; set; }
      public CoinbaseModel Coinbase { get; set; }
   }
}
