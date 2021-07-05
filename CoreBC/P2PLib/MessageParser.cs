using System;
using System.Collections.Generic;
using System.Text;

namespace CoreBC.P2PLib
{
   public class MessageParser
   {
      public bool EndOfFile = false;
      public string SenderId;
      public string Message = "";
      public MsgFromServer FromServer = MsgFromServer.PretendIsNull;
      public MsgFromClient FromClient  = MsgFromClient.PretendIsNull;
      public void ConsumeBites(byte[] consumedBytes)
      {
         string stringedData = Encoding.ASCII.GetString(consumedBytes);
         if (stringedData.Contains("<ID>"))
         {
            SenderId = stringedData.Split("<ID>")[0];
            stringedData = stringedData.Split("<ID>")[1];
            Message = "";
            Message += stringedData;
            FromClient = MsgFromClient.PretendIsNull;
            FromServer = MsgFromServer.PretendIsNull;
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
         }
         catch (Exception)
         {
            Message = string.Empty;
         }


         switch (header) // from client switch
         {
            case "<newtransaction>":
               FromClient = MsgFromClient.NewTransaction; 
               break;
            case "<blockmined>":
               FromClient = MsgFromClient.MinedBlockFound;
               break;
            case "<needblockhash>":
               FromClient = MsgFromClient.NeedBlockHash;
               break;
            case "<bootstrap>":
               FromClient = MsgFromClient.NeedBoostrap;
               break;
            case "<needconnections>":
               FromClient = MsgFromClient.NeedConnections;
               break;
            case "<needheightrange>":
               FromClient = MsgFromClient.NeedHeightRange;
               break;
         }

         switch (header) // from server switch
         {
            case "<blockmined>":
               FromServer = MsgFromServer.ABlockWasMined;
               break;
            case "<newtransaction>":
               FromServer = MsgFromServer.NewTransaction;
               break;
            case "<myblockheight>":
               FromServer = MsgFromServer.HeresMyBlockHeight;
               break;
            case "<gotconnections>":
               FromServer = MsgFromServer.HeresSomeConnections;
               break;
            case "<heresheightrange>":
               FromServer = MsgFromServer.HeresHeightRange;
               break;
         }
      }
   }
}
