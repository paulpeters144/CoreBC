using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using CoreBC.BlockModels;
using CoreBC.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CoreBC
{
   static class BlockchainRecord
   {
      public static void SaveToMempool(TransactionModel tx)
      {
         string path = Program.FilePath + "\\Blockchain\\Mempool";
         string mempoolFile = $"{path}\\mempool.json";

         if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

         if (!File.Exists(mempoolFile))
            File.Create(mempoolFile).Dispose();

         string oldMempoolFile = File.ReadAllText(mempoolFile);
         JObject mempoolObj;

         if (!String.IsNullOrEmpty(oldMempoolFile))
            mempoolObj = JObject.Parse(oldMempoolFile);
         else mempoolObj = new JObject();

         JProperty newTx = new JProperty(tx.TransactionId, new JObject(
            new JProperty("Signature", tx.Signature),
            new JProperty("Locktime", tx.Locktime),
            new JProperty("Input", new JObject(
                  new JProperty("FromAddress", tx.Input.FromAddress),
                  new JProperty("Amount", tx.Input.Amount)
               )),
            new JProperty("Output", new JObject(
                  new JProperty("ToAddress", tx.Output.ToAddress),
                  new JProperty("Amount", tx.Output.Amount)
               )),
            new JProperty("Fee", tx.Fee)
            ));

         mempoolObj.Add(newTx);
         string newMempool = mempoolObj.ToString(Formatting.Indented);
         File.WriteAllText(mempoolFile, newMempool);
      }

      public static void SaveNewBlock(BlockModel block)
      {

      }
      public static void SaveNewBlock(GenesisBlockModel block)
      {
         string path = Program.FilePath + $"\\Blockchain\\Blocks\\DactylBlocks_{DateTime.UtcNow.ToString("yyyyMMdd")}";
         string miningStartTime = String.Format("{0:yyyy MMM dd}", UnixTimeStampToDateTime(block.Time));
         string year = miningStartTime.Split(' ')[0];
         string month = miningStartTime.Split(' ')[1];
         string day = miningStartTime.Split(' ')[2];
         string yearPath = $"{path}\\{year}";
         string monthPath = $"{yearPath}\\{month}";
         string filePath = $"{monthPath}\\{year}-{month}-{day}.json";

         if (!Directory.Exists(monthPath))
            Directory.CreateDirectory(monthPath);

         if (!File.Exists(filePath))
            File.Create(filePath).Dispose();

         string blockJson = CreateJsonBlock(block);
         File.WriteAllText(filePath, blockJson);
      }

      public static string CreateJsonBlock(GenesisBlockModel genesisBlock)
      {
         JObject genesisObj = new JObject(
               new JProperty(genesisBlock.Hash, new JObject(
                     new JProperty("Confirmations", genesisBlock.Confirmations),
                     new JProperty("TransactionCount", genesisBlock.TransactionCount),
                     new JProperty("Height", genesisBlock.Height),
                     new JProperty("MerkleRoot", genesisBlock.MerkleRoot),
                     new JProperty("TXs", new JArray(new string[] { genesisBlock.TXs[0] })),
                     new JProperty("Time", genesisBlock.Time),
                     new JProperty("Nonce", genesisBlock.Nonce),
                     new JProperty("Difficulty", genesisBlock.Difficulty),
                     new JProperty("Coinbase", new JObject(
                        new JProperty("TransactionId", genesisBlock.Coinbase.TransactionId),
                        new JProperty("BlockHash", genesisBlock.Hash),
                        new JProperty("Output", new JObject(
                           new JProperty("ToAddress", genesisBlock.Coinbase.Output.ToAddress),
                           new JProperty("Amount", genesisBlock.Coinbase.Output.Amount)
                        ))
                  )))
            ));
         return genesisObj.ToString(Formatting.Indented);
      }



      private static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
      {
         // Unix timestamp is seconds past epoch
         System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
         dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
         return dtDateTime;
      }
   }
}
