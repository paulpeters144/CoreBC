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
      public static bool WeHaveReceivedNewBlock = false;
      public static string MiningDifficulty = "00000";
      public static string FormatDigits(decimal amount)
      {
         return String.Format("{0:0.0000000000}", amount);
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

      public static string GetDifficulty() =>
         MiningDifficulty;

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

      public static string GetCoinbaseAmount() =>
         "300";

      public static decimal GetFeePercent() =>
         0.00025M;

      public static string GetBlockDir() =>
      $"{Program.FilePath}\\";

      public static string GetBlockchainFilePath() =>
         GetBlockDir() + $"DotNetBlockChain.json";

      public static string GetAcctSetFile() =>
         GetBlockDir() + "ACCTSet.json";

      public static string GetMempooFile() =>
         GetBlockDir() + "mempool.json";

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

      public static void ReadException(Exception ex)
      {
         string error = $"{ex.Message}{Environment.NewLine}{ex.StackTrace}";
         Console.WriteLine("Error connecting: " + error);
      }
   }
}
