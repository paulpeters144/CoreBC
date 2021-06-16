using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CoreBC.BlockModels
{
   public class TransactionModel
   {
      public string TransactionId { get; set; }
      public string Signature { get; set; }
      public long Locktime { get; set; }
      public Input Input { get; set; }
      public Output Output { get; set; }
      public string Fee { get; set; }
      public decimal FeePercent = 0.00025M;
   }

   public class Input 
   {
      public string FromAddress { get; set; }
      public string Amount { get; set; }
   }
   public class Output 
   {
      public string ToAddress { get; set; }
      public string Amount { get; set; }
   }

   public class CoinbaseModel
   {
      public string TransactionId { get; set; }
      public string BlockHash { get; set; }
      public Output Output { get; set; }
   }
}
