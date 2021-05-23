using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CoreBC.BlockModels
{
   class TransactionModel
   {
      public string TransactionId { get; set; }
      public string Signature { get; set; }
      public long LockTime { get; set; }
      public Input[] Inputs { get; set; }
      public Output[] Outputs { get; set; }
   }

   class Input 
   {
      public string FromAddress { get; set; }
      public double Amount { get; set; }
   }
   class Output 
   {
      public string ToAddress { get; set; }
      public double Amount { get; set; }
   }
}
