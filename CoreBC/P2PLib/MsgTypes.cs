using System;
using System.Collections.Generic;
using System.Text;

namespace CoreBC.P2PLib
{
   public enum MsgFromClient
   {
      NewTransaction, // if tx is new, send tx to all clients except the one that send msg
      MinedBlockFound, // if block is new, broadcast to clients except the one that sent the msg
      NeedBlockHash, // if you have has, send client entire block
      NeedBoostrap, // send client back all ipaddress of clients connected to. Send client all blocks that are missing
      NeedConnections, // send client back all ipaddress of clients connected to.
      NeedHeightRange, // after recieving a range of block heights, send client all data for each block in range.
      PretendIsNull,
   }

   public enum MsgFromServer
   {
      ABlockWasMined, // if block is new, broadcast to clients except the one that sent the msg
      NewTransaction, // if tx is new, send tx to all clients except the one that send msg
      HeresMyBlockHeight, // if block height is higher than yours, ask for range of block heights
      HeresSomeConnections, // if you are not connected to clients, connect to clients
      HeresHeightRange,
      PretendIsNull,
   }
}