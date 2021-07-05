using CoreBC.BlockModels;
using CoreBC.CryptoApi;
using CoreBC.DataAccess;
using CoreBC.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml;

namespace CoreBC
{
   internal class ChainKeys
   {
      public int KeySize = 512;
      public RSAParameters PublicKey;
      public string KeyName { get; set; }
      private RSAParameters PrivateKey;
      private IDataAccess DB;
      public ChainKeys(string keyName)
      {
         KeyName = keyName;
         findKey();
         DB = new BlockChainFiles();
      }
      public TransactionModel SignTransaction(TransactionModel tx)
      {
         string message = createMessageFrom(tx);
         var document = Encoding.ASCII.GetBytes(message);
         byte[] hashedDocument = SHA256.Create().ComputeHash(document);
         byte[] messageSigned = SignData(hashedDocument);
         string signature = Convert.ToBase64String(messageSigned);
         tx.Signature = signature;
         return tx;
      }

      public TransactionModel SendMoneyTo(string recPubKey, decimal amount)
      {
         Input input = new Input
         {
            FromAddress = GetPubKeyString(),
            Amount = Helpers.FormatDigits(amount)
         };

         Output output = new Output
         {
            ToAddress = recPubKey,
            Amount = Helpers.FormatDigits(amount)
         };

         TransactionModel tx = new TransactionModel
         {
            Input = input,
            Output = output,
            Locktime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
         };

         tx = SignTransaction(tx);
         tx = CreateTransactionId(tx);
         tx.Fee = Helpers.FormatDigits(Convert.ToDecimal(input.Amount) * Helpers.GetFeePercent());

         bool txVerified = VerifyTransaction(tx);
         decimal totalTradeAmount = (Convert.ToDecimal(input.Amount) + Convert.ToDecimal(tx.Fee));
         string pubKey = GetPubKeyString();
         bool hasEnoughDYL = DB.GetWalletBalanceFor(pubKey) > totalTradeAmount;

         if (txVerified && hasEnoughDYL)
            return tx;
         else return null;
      }

      public bool VerifyTransaction(TransactionModel tx)
      {
         string message = createMessageFrom(tx);
         var document = Encoding.ASCII.GetBytes(message);
         byte[] hashedDocument = SHA256.Create().ComputeHash(document);
         byte[] txSig = Convert.FromBase64String(tx.Signature);
         bool result = VerifySignature(hashedDocument, txSig, tx);
         return result;
      }

      public TransactionModel CreateTransactionId(TransactionModel tx)
      {
         StringBuilder sb = new StringBuilder();

         sb.Append($"{tx.Input.FromAddress}{tx.Input.Amount}");
         sb.Append($"{tx.Output.ToAddress}{tx.Output.Amount}");
         sb.Append(tx.Locktime);
         sb.Append(tx.Signature);

         string txFullMessage = sb.ToString();
         byte[] txFullMessageInBytes = Encoding.ASCII.GetBytes(txFullMessage);
         byte[] txIdInBytes = SHA256.Create().ComputeHash(txFullMessageInBytes);
         string txId = Helpers.GetSHAStringFromBytes(txIdInBytes);
         tx.TransactionId = txId;
         return tx;
      }

      private string createMessageFrom(TransactionModel tx)
      {
         StringBuilder sb = new StringBuilder();

         sb.Append($"{tx.Input.FromAddress}{tx.Input.Amount}");
         sb.Append($"{tx.Output.ToAddress}{tx.Output.Amount}");
         sb.Append(tx.Locktime);

         return sb.ToString();
      }

      public string GetPubKeyString()
      {
         string result = RSAKeys.ExportPublicKey(PublicKey);
         result = result.Replace("-----END PUBLIC KEY-----", "");
         result = result.Replace("-----BEGIN PUBLIC KEY-----", "");
         result = result.Replace("\n", "");
         return result;
      }

      private void findKey()
      {
         var path = Program.FilePath;

         if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

         string pathToKey = $"{path}\\{KeyName}.pem";

         if (!File.Exists(pathToKey))
         {
            File.Create(pathToKey).Dispose();
            createNewKeySet(pathToKey);
         }
         else
         {
            loadKeySetFrom($"{path}\\{KeyName}.pem");
         }
      }

      private byte[] SignData(byte[] hashOfDataToSign)
      {
         using (var rsa = new RSACryptoServiceProvider(KeySize))
         {
            rsa.PersistKeyInCsp = false;
            rsa.ImportParameters(PrivateKey);
            var rsaFormatter = new RSAPKCS1SignatureFormatter(rsa);
            rsaFormatter.SetHashAlgorithm("SHA256");
            return rsaFormatter.CreateSignature(hashOfDataToSign);
         }
      }

      private bool VerifySignature(byte[] hashOfDataToSign, byte[] signature, TransactionModel tx)
      {
         string pubKey = pubKeyFormatter(tx.Input.FromAddress);
         var rsa = RSAKeys.ImportPublicKey(pubKey);
         var rsaDeformatter = new RSAPKCS1SignatureDeformatter(rsa);
         rsaDeformatter.SetHashAlgorithm("SHA256");
         bool result = rsaDeformatter.VerifySignature(hashOfDataToSign, signature);
         return result;
      }

      private string pubKeyFormatter(string fromAddress)
      {
         string result = "-----BEGIN PUBLIC KEY-----";
         char[] charArr = fromAddress.ToCharArray();
         for (int i = 0; i < charArr.Length; i++)
         {
            if (i % 64 == 0)
               result += "\n";
            result += charArr[i];
         }
         result += "\n-----END PUBLIC KEY-----";
         return $"{result}";
      }

      private void loadKeySetFrom(string keyPath)
      {
         string key = File.ReadAllText(keyPath);
         using (var rsa = RSAKeys.ImportPrivateKey(key))
         {
            PublicKey = rsa.ExportParameters(false);
            PrivateKey = rsa.ExportParameters(true);
         }
      }

      private void createNewKeySet(string keyPath)
      {
         using (var rsa = new RSACryptoServiceProvider(KeySize))
         {
            rsa.PersistKeyInCsp = false;
            PublicKey = rsa.ExportParameters(false);
            PrivateKey = rsa.ExportParameters(true);
            string newKey = RSAKeys.ExportPrivateKey(rsa);
            File.WriteAllText(keyPath, newKey);
         }
      }
   }
}