using CoreBC.BlockModels;
using CoreBC.Utils;
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

      public BlockModel MineGBlock(BlockModel genesisBlock)
      {
         string mRoot = genesisBlock.MerkleRoot;
         string difficulty = genesisBlock.Difficulty;
         string time = genesisBlock.Time.ToString();
         Int64 nonce = 0;
         for ( ; ; )
         {
            string attempt = $"{mRoot}{time}{difficulty}{nonce}";
            string hashAttemp = Helpers.GetSHAStringFromString(attempt);

            if (hashAttemp.StartsWith(genesisBlock.Difficulty))
            {
               genesisBlock.Nonce = nonce;
               break;
            }
            nonce++;
         }

         return genesisBlock;
      }

      public BlockModel Mine(BlockModel block)
      {
         string result = string.Empty;
         string prevHash = block.PreviousHash;
         string mRoot = block.MerkleRoot;
         string difficulty = block.Difficulty;
         Int64 nonce = 0;
         for ( ; ; )
         {

            string attempt = $"{prevHash}{mRoot}{block.Time}{difficulty}{nonce}";
            byte[] hashBytes = SHA256.Create().ComputeHash(Encoding.ASCII.GetBytes(attempt));
            string hashAttemp = getHashString(hashBytes);

            if (hashAttemp.StartsWith(block.Difficulty))
            {
               result = hashAttemp;
               break;
            }
            else
            {
               nonce++;
            }

         }
         block.Hash = result;
         block.Nonce = nonce;
         return block;
      }
   }
}
