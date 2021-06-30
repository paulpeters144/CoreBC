using CoreBC.BlockModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace CoreBC.Utils
{
   public static class Helpers
   {
      public static string FormatDigits(decimal amount)
      {
         decimal rounded = Math.Round(amount, 10, MidpointRounding.ToEven);
         return String.Format("{0:0.0000000000}", rounded);
      }

      public static string GetSHAStringFromBytes(byte[] txIdInBytes)
      {
         StringBuilder sb = new StringBuilder();

         foreach (byte b in txIdInBytes)
            sb.Append(b.ToString("x2"));

         return sb.ToString();
      }

      public static string GetSHAStringFromString(string text)
      {
         byte[] textToBytes = Encoding.ASCII.GetBytes(text);
         byte[] textByesShawed = SHA256.Create().ComputeHash(textToBytes);
         return GetSHAStringFromBytes(textByesShawed);
      }

      public static long GetCurrentUTC() =>
         new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();

      public static string GetDifficulty()
      {
         return "000";
      }

      public static string GetDifficulty(BlockModel[] blocks)
      {
         int maxBlockCount = 2016;
         int maxBlockTimeSeconds = 600;
         List<BlockModel> blockList = blocks.OrderByDescending(b => b.Time).ToList();
         string lastDifficulty = blockList[0].Difficulty;
         
         if (blocks.Length < maxBlockCount)
            return lastDifficulty;

         List<long> blockGenTimes = new List<long>();
         blockList.Reverse();
         for (int i = 0; i < blockList.Count - 1; i++)
         {
            long secondDiff = blockList[i].Time - blockList[i + 1].Time;
            blockGenTimes.Add(secondDiff);
            if (blockGenTimes.Count > maxBlockCount)
               break;
         }

         long averageBlockTime =
               (long)Math.Floor(
                     Convert.ToDecimal(blockGenTimes.Sum() / blockGenTimes.Count)
                  );

         string result = string.Empty;

         if (averageBlockTime > maxBlockTimeSeconds)
            result = lastDifficulty + "0";
         else if (!String.IsNullOrEmpty(lastDifficulty))
            result = lastDifficulty.Substring(0, lastDifficulty.Length - 1);

         return result;
      }

      public static string GetCoinbaseAmount()
      {
         return "300";
      }

      public static decimal GetFeePercent()
      {
         return 0.00025M;
      }
      public static string GetBlockchainFilePath()
      {
         return GetBlockDir() + $"DotNetBlockChain.json";
      }

      public static string GetAcctSetFile()
      {
         return $"{Program.FilePath}\\Blockchain\\ACCTSet\\ACCTSet.json";
      }

      public static string GetBlockDir()
      {
         return $"{Program.FilePath}\\Blockchain\\Blocks\\";
      }

      public static string GetMempooFile()
      {
         return $"{Program.FilePath}\\Blockchain\\Mempool\\mempool.json";
      }

      public static decimal GetMineReward(long height)
      {
         decimal baseReward = 50;
         int halving = 210000;
         while (height > halving)
         {
            baseReward *= 0.5M;
            height -= halving;
         }
         return baseReward;
      }
   }
}
