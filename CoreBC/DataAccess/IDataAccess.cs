using CoreBC.BlockModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace CoreBC.DataAccess
{
   public interface IDataAccess
   {
      public BlockModel[] GetLastBlocks(int count);
      public BlockModel GetBlockHeight(int height);
      public BlockModel GetBlock(string hash);
      public bool SaveMinedBlock(BlockModel block);
      public bool SaveRecievedBlock(BlockModel block);
      public bool SaveToMempool(TransactionModel tx);
      public bool UpdateAccountBalances();
      BlockModel[] GetAllBlocks();
      void Save(BlockModel[] fullBlockChain);
      decimal GetWalletBalanceFor(string publicKey);
   }
}