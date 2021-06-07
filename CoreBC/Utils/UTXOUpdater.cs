using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CoreBC.Utils
{
   class UTXOUpdater
   {
      public void RunUpdate()
      {
         var utxoDictionary = new Dictionary<string, decimal>();
         // get a list of all block txs
         var blockDates = Directory.GetDirectories($"{Program.FilePath}\\Blockchain\\Blocks");
         foreach (var blockFilePath in blockDates)
         {
            JObject dayObj = JObject.Parse(blockFilePath);
                  
            foreach (var block in dayObj)
            {
               var txIds = (JArray)block.Value["TXs"];
               List<string> blockTXs = txIds.Select(txs => (string)txs).ToList();
               utxoDictionary = sumTransactions(utxoDictionary, blockTXs, block.Value);
            }
         }

         saveToUTXOSet(utxoDictionary);
      }

      private void saveToUTXOSet(Dictionary<string, decimal> utxoDictionary)
      {
         JObject utxoSetObj = new JObject();

         foreach (var utxoSet in utxoDictionary)
            utxoSetObj.Add(utxoSet.Key, utxoSet.Value);
         
         string dirPath = $"{Program.FilePath}\\Blockchain\\UTXOSet";

         if (!Directory.Exists(dirPath))
            Directory.CreateDirectory(dirPath);

         string newFile = utxoSetObj.ToString(Formatting.Indented);
         string filePath = dirPath + "\\UTXOSet.json";
         File.WriteAllText(filePath, newFile);
      }

      private Dictionary<string, decimal> sumTransactions(Dictionary<string, decimal> utxoDictionary, List<string> blockTXs, JToken value)
      {
         string coinbaseAddress = value["Coinbase"]["Output"]["ToAddress"].ToString();
         decimal coinbaseAmount = Convert.ToDecimal(value["Coinbase"]["Output"]["Amount"]);
         string coinbaseTxId = value["Coinbase"]["TransactionId"].ToString();
         blockTXs.Remove(coinbaseTxId);

         if (!utxoDictionary.ContainsKey(coinbaseAddress))
            utxoDictionary.Add(coinbaseAddress, coinbaseAmount);

         foreach (var tx in blockTXs)
         {

         }

         return utxoDictionary;
      }
   }
}
