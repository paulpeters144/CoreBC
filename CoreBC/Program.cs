using System;
using System.Linq;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.Threading;

namespace CoreBC
{
   class Program
   {
      static void Main(string[] args)
      {
         int solvedCount = 0;
         float totalSeconds = 0;
         while (true)
         {
            string challenge = randWord(15) + " ";
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

               if (hashAttemp.StartsWith("000000"))
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
         
         Console.ReadLine();
      }

      private static string getHashString(byte[] hashBytes)
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
   }
}
