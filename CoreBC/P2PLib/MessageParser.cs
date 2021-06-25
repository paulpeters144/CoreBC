using System;
using System.Collections.Generic;
using System.Text;

namespace CoreBC.P2PLib
{
   public class MessageParser
   {
      public bool EndOfFile = false;
      public string SenderId { get; set; }
      public string Message = "";
      public void ConsumeBites(byte[] consumedBytes)
      {
         string stringedData = Encoding.ASCII.GetString(consumedBytes);
         if (stringedData.Contains("<ID>"))
         {
            SenderId = stringedData.Split("<ID>")[0];
            stringedData = stringedData.Split("<ID>")[1];
            Message = "";
            EndOfFile = false;
         }
         if (stringedData.Contains("<EOF>"))
         {
            stringedData = stringedData.Split("<EOF>")[0];
            EndOfFile = true;
         }
         Message += stringedData;
      }
   }
}
