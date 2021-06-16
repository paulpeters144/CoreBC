using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CoreBC.Utils
{
   class AccountUpdater
   {
      public void RunUpdate()
      {
         var acctDictionary = new Dictionary<string, decimal>();
         // get a list of all block txs
         var blockDates = Directory.GetDirectories($"{Program.FilePath}\\Blockchain\\Blocks");
         foreach (var blockFilePath in blockDates)
         {
            JObject dayObj = JObject.Parse(blockFilePath);
                  
            foreach (var block in dayObj)
            {
               var txIds = (JArray)block.Value["TXs"];
               List<string> blockTXs = txIds.Select(txs => (string)txs).ToList();
               acctDictionary = sumTransactions(acctDictionary, blockTXs, block.Value);
            }
         }

         saveToACCTSet(acctDictionary);
      }

      private void saveToACCTSet(Dictionary<string, decimal> acctDictionary)
      {
         JObject acctSetObj = new JObject();

         foreach (var acctSet in acctDictionary)
            acctSetObj.Add(acctSet.Key, acctSet.Value);
         
         string dirPath = $"{Program.FilePath}\\Blockchain\\ACCTSet";

         if (!Directory.Exists(dirPath))
            Directory.CreateDirectory(dirPath);

         string newFile = acctSetObj.ToString(Formatting.Indented);
         string filePath = dirPath + "\\ACCTSet.json";
         File.WriteAllText(filePath, newFile);
      }

      private Dictionary<string, decimal> sumTransactions(Dictionary<string, decimal> acctDictionary, List<string> blockTXs, JToken value)
      {
         string coinbaseAddress = value["Coinbase"]["Output"]["ToAddress"].ToString();
         decimal coinbaseAmount = Convert.ToDecimal(value["Coinbase"]["Output"]["Amount"]);
         string coinbaseTxId = value["Coinbase"]["TransactionId"].ToString();
         blockTXs.Remove(coinbaseTxId);

         if (!acctDictionary.ContainsKey(coinbaseAddress))
            acctDictionary.Add(coinbaseAddress, coinbaseAmount);

         foreach (var tx in blockTXs)
         {

         }

         return acctDictionary;
      }
   }
}
