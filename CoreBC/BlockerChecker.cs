using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace CoreBC
{

   class BlockerChecker
   {
      public string BlockReport { get; set; }
      private JObject BlockObj { get; set; }
      public BlockerChecker(JObject blockObj)
      {
         BlockObj = blockObj;
      }

      public void ExamineBlock()
      {

      }
   }
}
