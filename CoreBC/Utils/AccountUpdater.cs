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
         var acctDictionary = new Dictionary<string, string>();
         string path = $"{Program.FilePath}\\Blockchain\\Blocks";
         var blockDates = Directory.GetFiles(path, "*.json");
         foreach (var blockFilePath in blockDates)
         {
            string fileText = File.ReadAllText(blockFilePath);
            JObject dayObj = JObject.Parse(fileText);
                  
            foreach (var block in dayObj)
            {
               if (block.Key.ToLower() == "heightrange")
                  continue;
               
               var txIds = (JArray)block.Value["TXs"];
               List<string> blockTXs = txIds.Select(txs => (string)txs).ToList();
               acctDictionary = sumBlockActivity(acctDictionary, blockTXs, block.Value);
            }
         }

         saveToACCTSet(acctDictionary);
      }

      private void saveToACCTSet(Dictionary<string, string> acctDictionary)
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

      private Dictionary<string, string> sumBlockActivity
         (
            Dictionary<string, string> acctDictionary, 
            List<string> blockTXs, 
            JToken value
         )
      {
         string coinbaseAddress = value["Coinbase"]["Output"]["ToAddress"].ToString();
         string blockReward = Helpers.FormatDactylDigits(
               Convert.ToDecimal(value["Coinbase"]["Output"]["Amount"]) +
               Convert.ToDecimal(value["Coinbase"]["TotalFees"])
            );

         string coinbaseTxId = value["Coinbase"]["TransactionId"].ToString();
         blockTXs.Remove(coinbaseTxId);

         if (!acctDictionary.ContainsKey(coinbaseAddress))
            acctDictionary.Add(coinbaseAddress, blockReward);
         else acctDictionary[coinbaseAddress] = 
               Helpers.FormatDactylDigits(Convert.ToDecimal(blockReward) + 
               Convert.ToDecimal(acctDictionary[coinbaseAddress]));

         foreach (var tx in blockTXs)
         {
            var transaction = value["Transactions"][tx];
            if (transaction == null)
               continue;

            string fromAddress = transaction["Input"]["FromAddress"].ToString();
            decimal inputAmount = Convert.ToDecimal(transaction["Input"]["Amount"]);
            decimal fee = Convert.ToDecimal(transaction["Fee"]);
            decimal totalSpend = inputAmount + fee;
            acctDictionary = addressSpend(acctDictionary, fromAddress, totalSpend);

            string toAddress = transaction["Output"]["ToAddress"].ToString();
            decimal outputAmount = Convert.ToDecimal(transaction["Output"]["Amount"]);
            acctDictionary = addressRecieve(acctDictionary, toAddress, outputAmount);
         }

         return acctDictionary;
      }

      private Dictionary<string, string> addressSpend(
            Dictionary<string, string> acctDictionary, 
            string fromAddress, 
            decimal totalSpend
         )
      {
         if (acctDictionary.ContainsKey(fromAddress))
         {
            decimal newBalance = Convert.ToDecimal(acctDictionary[fromAddress]) - totalSpend;
            acctDictionary[fromAddress] = Helpers.FormatDactylDigits(newBalance);
         }
         return acctDictionary;
      }

      private Dictionary<string, string> addressRecieve(
            Dictionary<string, string> acctDictionary,
            string toAddress,
            decimal outputAmount
         )
      {
         if (acctDictionary.ContainsKey(toAddress))
         {
            decimal newBalance = Convert.ToDecimal(acctDictionary[toAddress]) + outputAmount;
            acctDictionary[toAddress] = Helpers.FormatDactylDigits(newBalance);
         }
         else
         {
            acctDictionary[toAddress] = Helpers.FormatDactylDigits(outputAmount);
         }
         return acctDictionary;
      }
   }
}
