using CoreBC.BlockModels;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace CoreBC
{
   class PlayScenario
   {
      public string Path { get; set; }
      public void Play()
      {
         var codeBase = System.Reflection.Assembly.GetExecutingAssembly().Location;
         var pathList = codeBase.Split("\\").ToList();
         pathList.RemoveAt(pathList.Count - 1);
         Path = String.Join("\\", pathList.ToArray());
         CreateTx();
      }

      public static void CreateTx()
      {
         var senderKey = new DactylKey("paulp2");
         string senderPubKey = senderKey.GetPubKeyString();
         var recKey = new DactylKey("paulp1");
         string recPubKey = recKey.GetPubKeyString();

         Input input1 = new Input
         {
            FromAddress = senderPubKey,
            Amount = 10
         };

         Output output1 = new Output
         {
            ToAddress = recPubKey,
            Amount = 10
         };

         DateTime foo = DateTime.Now;
         long unixTime = ((DateTimeOffset)foo).ToUnixTimeSeconds();

         TransactionModel tx = new TransactionModel
         {
            Inputs = new Input[] { input1 },
            Outputs = new Output[] { output1 },
            LockTime = unixTime,
         };

         tx = senderKey.SignTransaction(tx);
         tx = senderKey.CreateTransactionId(tx);
         //BlockchainRecord.SaveToMempool(tx);
      }

      public static void SignScenario()
      {
         DactylKey key = new DactylKey("paulp");
         string originalMessage = "this is a message";
         byte[] hashedMessage = Encoding.UTF8.GetBytes(originalMessage);
         byte[] messageSha = SHA256.Create().ComputeHash(hashedMessage);

         byte[] messageSigned = key.SignData(messageSha);
         var test = Convert.ToBase64String(messageSigned);
         var test2 = Convert.FromBase64String(test);

         if (messageSigned.SequenceEqual(test2))
         {

         }

         if (key.VerifySignature(messageSha, messageSigned))
         {

         }
      }
   }
}
