using System;
using System.Collections.Generic;
using System.Text;

namespace CoreBC.P2PLib
{
   public class MessageParser
   {
      public bool EndOfFile = false;
      public string SenderId = string.Empty;
      public string Message = string.Empty;
      public string Header = string.Empty;
      public void ConsumeBites(byte[] consumedBytes)
      {
         string stringedData = Encoding.ASCII.GetString(consumedBytes);
         if (stringedData.Contains("<ID>"))
         {
            SenderId = stringedData.Split("<ID>")[0];
            stringedData = stringedData.Split("<ID>")[1];
            Message = string.Empty;
            Header = string.Empty;
            Message += stringedData;
            EndOfFile = false;
         }
         else
         {
            Message += stringedData;
         }
       
         if (Message.Contains("<EOF>"))
         {
            Message = Message.Split("<EOF>")[0];
            EndOfFile = true;
            
            parseHeader();
         }
      }

      private void parseHeader()
      {
         string header = string.Empty;
         char[] msgArr = Message.ToCharArray();

         if (!Message.Contains('<') && !Message.Contains('>'))
            return;

         foreach (var c in msgArr)
         {
            header += c;
            if (c == '>')
               break;
         }

         try
         {
            Message = Message.Substring(
                  header.Length,
                  Message.Length - header.Length
               ).Trim();
            Header = header;
         }
         catch (Exception)
         {
            Message = string.Empty;
            Header = string.Empty;
         }
      }
   }
}
