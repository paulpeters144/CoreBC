using System;
using System.Collections.Generic;
using System.Text;

namespace CoreBC.BlockModels
{
   class BlockModel
   {
      public string Hash { get; set; }
      public string PreviousHash { get; set; }
      public double Confirmations { get; set; }
      public int TransactionCount { get; set; }
      public double Height { get; set; }
      public string MerkleRoot { get; set; }
      public string[] TXs { get; set; }
      public DateTime Time { get; set; }
      public DateTime MedianTime { get; set; }
      public double Nonce { get; set; }
      public double Bits { get; set; }
      public double Difficulty { get; set; }
      public string Chainwork { get; set; }
   }
}
