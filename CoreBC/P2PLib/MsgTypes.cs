using System;
using System.Collections.Generic;
using System.Text;

namespace CoreBC.P2PLib
{
   public static class MessageHeader
   {
      public static readonly string NewTransaction = "<newtransaction>";
      public static readonly string BlockMined = "<blockmined>";
      public static readonly string NeedBoostrap = "<bootstrap>";
      public static readonly string NeedHeightRange = "<needheightrange>";
      public static readonly string HeresMyBlockHeight = "<myblockheight>";
      public static readonly string HeresHeightRange = "<heresheightrange>";
   }
}