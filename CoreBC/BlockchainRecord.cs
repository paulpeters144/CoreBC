using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CoreBC.BlockModels;
using Newtonsoft.Json;

namespace CoreBC
{
   static class BlockchainRecord
   {
      public static void SaveToMempool(TransactionModel tx, string path)
      {
         path = path + "\\Blockchain\\Mempool";
         if (Directory.Exists(path))
         {
            string mempoolFile = $"{path}\\mempool.json";
         
            if (File.Exists(mempoolFile))
            {
               string oldMempoolFile = File.ReadAllText(mempoolFile);
               MempoolModel oldMempool = JsonConvert.DeserializeObject<MempoolModel>(oldMempoolFile);
               MempoolModel mempool = new MempoolModel();
               mempool.Transactions = new TransactionModel[oldMempool.Transactions.Length + 1];
               
               for (int i = 0; i < oldMempool.Transactions.Length; i++)
                  mempool.Transactions[i] = oldMempool.Transactions[i];

               mempool.Transactions[mempool.Transactions.Length - 1] = tx;
               string newMempool = JsonConvert.SerializeObject(mempool, Formatting.Indented);
               File.WriteAllText(mempoolFile, newMempool);
            }
         }
      }
   }
}
