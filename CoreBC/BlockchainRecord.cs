﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using CoreBC.BlockModels;
using CoreBC.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CoreBC
{
   public class BlockchainRecord
   {
      public void SaveToMempool(TransactionModel tx)
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

      public void SaveNewBlock(BlockModel block)
      {
         string blockJson = getMinedTransactions(block);
         string filePath = Helpers.GetBlockDir() + Helpers.GetNexBlockFileName();
         File.WriteAllText(filePath, blockJson);
      }

      private string getMinedTransactions(BlockModel block)
      {
         string mempoolPath = $"{Program.FilePath}\\Blockchain\\Mempool\\mempool.json";

         if (!File.Exists(mempoolPath))
            return "";

         string mempoolJson = File.ReadAllText(mempoolPath);
         JObject mempoolObj = JObject.Parse(mempoolJson);

         JObject txObj = new JObject();
         foreach (var tx in mempoolObj)
            if (block.TXs.Contains(tx.Key))
               txObj.Add(new JProperty(tx.Key, tx.Value));

         JObject result = new JObject(
                  new JProperty(block.Hash, new JObject(
                     new JProperty("PreviousHash", block.PreviousHash),
                     new JProperty("Confirmations", block.Confirmations),
                     new JProperty("TransactionCount", block.TransactionCount),
                     new JProperty("Height", block.Height),
                     new JProperty("MerkleRoot", block.MerkleRoot),
                     new JProperty("Time", block.Time),
                     new JProperty("Nonce", block.Nonce),
                     new JProperty("Difficulty", block.Difficulty),
                     new JProperty("TXs", new JArray(block.TXs)),
                     new JProperty("Coinbase", new JObject(
                        new JProperty("TransactionId", block.Coinbase.TransactionId),
                        new JProperty("BlockHash", block.Coinbase.BlockHash),
                        new JProperty("Output", new JObject(
                                 new JProperty("ToAddress", block.Coinbase.Output.ToAddress),
                                 new JProperty("Amount", block.Coinbase.Output.Amount)
                              )
                           )
                        )
                     ),
                     new JProperty("Transactions", txObj)
                  )
              )
            );

         string prevBlocksPath = Helpers.GetBlockDir() + Helpers.GetNexBlockFileName();
         if (File.Exists(prevBlocksPath))
         {
            string allText = File.ReadAllText(prevBlocksPath);
            JObject prevBlocks = JObject.Parse(allText);
            foreach (var b in prevBlocks)
               result.Add(new JProperty(b.Key, b.Value));
         }

         return result.ToString(Formatting.Indented);
      }

      public void SaveNewBlock(GenesisBlockModel block)
      {
         string fileName = Helpers.GetNexBlockFileName();
         string dirPath = Helpers.GetBlockDir();
         string filePath = dirPath + fileName;

         if (!Directory.Exists(dirPath))
            Directory.CreateDirectory(dirPath);

         if (!File.Exists(filePath))
            File.Create(filePath).Dispose();

         string blockJson = CreateJsonBlock(block);
         File.WriteAllText(filePath, blockJson);
      }

      public void UpdateFileMetaData(string filePath)
      {
         string json = File.ReadAllText(filePath);
         JObject fileBlocks = JObject.Parse(json);
         Int64 lowestBlock = Int64.MaxValue;
         Int64 heighestBlock = Int64.MinValue;

         foreach (var block in fileBlocks)
         {
            Int64 height = Convert.ToInt64(block.Value["Height"]);

            if (lowestBlock > height)
               lowestBlock = height;

            if (heighestBlock < height)
               heighestBlock = height;
         }

         JObject newObj = new JObject(new JProperty("HeightRange", $"{lowestBlock}:{heighestBlock}"));
         
         foreach (var block in fileBlocks)
            newObj.Add(new JProperty(block.Key, block.Value));

         string fileWithNewMetaData = newObj.ToString(Formatting.Indented);
         File.WriteAllText(filePath, fileWithNewMetaData);
      }

      public string CreateJsonBlock(GenesisBlockModel genesisBlock)
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

      private DateTime UnixTimeStampToDateTime(long unixTimeStamp)
      {
         // Unix timestamp is seconds past epoch
         System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
         dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
         return dtDateTime;
      }
   }
}
