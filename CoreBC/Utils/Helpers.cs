﻿using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace CoreBC.Utils
{
   public static class Helpers
   {
      public static string FormatDactylDigits(decimal amount)
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

      public static string GetCoinbaseAmount()
      {
         return "300";
      }

      public static string GetNexBlockFileName()
      {
         return $"DactylBlocks_{DateTime.UtcNow.ToString("yyyyMMdd")}.json";
      }

      public static string GetBlockDir()
      {
         return $"{Program.FilePath}\\Blockchain\\Blocks\\";
      }

      public static string GetMempooFile()
      {
         return $"{Program.FilePath}\\Blockchain\\Mempool\\mempool.json";
      }
   }
}
