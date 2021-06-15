﻿using CoreBC.BlockModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace CoreBC
{
   class Miner
   {
      public void ProofOfWork()
      {
         int solvedCount = 0;
         float totalSeconds = 0;
         while (true)
         {
            string challenge = randWord(15);
            string answer = string.Empty;
            string attempted = string.Empty;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            int count = 0;
            while (true)
            {
               count++;
               string attempt = challenge + count;
               byte[] hashBytes = SHA256.Create().ComputeHash(Encoding.ASCII.GetBytes(attempt));
               string hashAttemp = getHashString(hashBytes);

               if (hashAttemp.StartsWith("0000"))
               {
                  answer = hashAttemp;
                  attempted = attempt;
                  break;
               }
            }

            stopwatch.Stop();
            Thread.Sleep(1500);
            string seconds = "seconds: " + stopwatch.ElapsedMilliseconds * .001f;
            totalSeconds += stopwatch.ElapsedMilliseconds;
            solvedCount++;
            Console.WriteLine($"average seconds: {(totalSeconds / solvedCount) * .001f}\n{count}\n{seconds}\nguess: {attempted}\nanswer: {answer}\n");
         }
      }

      public static string getHashString(byte[] hashBytes)
      {
         StringBuilder result = new StringBuilder();

         for (int i = 0; i < hashBytes.Length; i++)
            result.Append(hashBytes[i].ToString("x2"));

         return result.ToString();
      }

      private static string randWord(int wordLength)
      {
         var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
         var stringChars = new char[wordLength];
         var random = new Random();

         for (int i = 0; i < stringChars.Length; i++)
            stringChars[i] = chars[random.Next(chars.Length)];

         return new String(stringChars);
      }

      public Dictionary<string, string> Mine(GenesisBlockModel genesisBlock)
      {
         var result = new Dictionary<string, string>();
         string mRoot = genesisBlock.MerkleRoot;
         string difficulty = genesisBlock.Difficulty;
         string time = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString();
         Int64 nonce = 0;
         for ( ; ; )
         {
            
            string attempt = $"{mRoot}{time}{difficulty}{nonce}";
            byte[] hashBytes = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(attempt));
            string hashAttemp = getHashString(hashBytes);

            if (hashAttemp.StartsWith(genesisBlock.Difficulty))
            {
               result.Add("MerkleRoot", mRoot);
               result.Add("Time", time);
               result.Add("Difficulty", difficulty);
               result.Add("Nonce", nonce.ToString());
               break;
            }
            nonce++;
         }

         return result;
      }

      public BlockModel Mine(BlockModel block)
      {
         string result = string.Empty;
         string mRoot = block.MerkleRoot;
         string difficulty = block.Difficulty;
         string time = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString();
         Int64 nonce = 204857;
         for ( ; ; )
         {

            string attempt = $"{mRoot}{time}{difficulty}{nonce}";
            byte[] hashBytes = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(attempt));
            string hashAttemp = getHashString(hashBytes);

            if (hashAttemp.StartsWith(block.Difficulty))
            {
               result = hashAttemp;
               break;
            }
            nonce++;
         }
         block.Hash = result;
         block.Nonce = nonce;
         return block;
      }
   }
}
