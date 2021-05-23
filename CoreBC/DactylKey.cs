using CoreBC.BlockModels;
using CoreBC.CryptoApi;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml;

namespace CoreBC
{
   internal class DactylKey
   {
      public int KeySize = 1024;
      public RSAParameters PublicKey;

      private RSAParameters PrivateKey;

      public DactylKey(string keyName)
      {
         findKey(keyName);
      }

      public TransactionModel SignTransaction(TransactionModel tx)
      {
         string message = createMessageFrom(tx);
         var document = Encoding.UTF8.GetBytes(message);
         byte[] hashedDocument = SHA256.Create().ComputeHash(document);
         byte[] messageSigned = SignData(hashedDocument);
         string signature = Convert.ToBase64String(messageSigned);
         tx.Signature = signature;
         return tx;
      }

      public bool VerifyTransaction(string transaction)
      {
         TransactionModel transactionModel = JsonConvert.DeserializeObject<TransactionModel>(transaction);
         string message = createMessageFrom(transactionModel);
         var document = Encoding.UTF8.GetBytes(message);
         byte[] hashedDocument = SHA256.Create().ComputeHash(document);
         byte[] txSig = Convert.FromBase64String(transactionModel.Signature);
         bool result = VerifySignature(hashedDocument, txSig);
         return result;
      }

      public bool VerifyTransaction(TransactionModel tx)
      {
         string message = createMessageFrom(tx);
         var document = Encoding.UTF8.GetBytes(message);
         byte[] hashedDocument = SHA256.Create().ComputeHash(document);
         byte[] txSig = Convert.FromBase64String(tx.Signature);
         bool result = VerifySignature(hashedDocument, txSig);
         return result;
      }

      public TransactionModel CreateTransactionId(TransactionModel tx)
      {
         StringBuilder sb = new StringBuilder();

         foreach (var input in tx.Inputs)
            sb.Append($"{input.FromAddress}{input.Amount}");

         foreach (var output in tx.Outputs)
            sb.Append($"{output.ToAddress}{output.Amount}");

         sb.Append(tx.LockTime);
         sb.Append(tx.Signature);
         string txFullMessage = sb.ToString();
         byte[] txFullMessageInBytes = Encoding.UTF8.GetBytes(txFullMessage);
         byte[] txIdInBytes = SHA256.Create().ComputeHash(txFullMessageInBytes);
         StringBuilder txIdSb = new StringBuilder();
         
         foreach (byte b in txIdInBytes)
            txIdSb.Append(b.ToString("x2"));

         tx.TransactionId = $"{DateTime.Now.ToString("yyyyMMdd")}_{txIdSb}";
         return tx;
      }

      private string createMessageFrom(TransactionModel tx)
      {
         StringBuilder sb = new StringBuilder();
         
         foreach (var input in tx.Inputs)
            sb.Append($"{input.FromAddress}{input.Amount}");
         
         foreach (var output in tx.Outputs)
            sb.Append($"{output.ToAddress}{output.Amount}");

         sb.Append(tx.LockTime);
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

      private void findKey(string keyName)
      {
         var codeBase = System.Reflection.Assembly.GetExecutingAssembly().Location;
         var pathList = codeBase.Split("\\").ToList();
         pathList.RemoveAt(pathList.Count - 1);
         var path = String.Join("\\" , pathList.ToArray());

         if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

         string pathToKey = $"{path}\\{keyName}.pem";

         if (!File.Exists(pathToKey))
         {
            File.Create(pathToKey).Dispose();
            createNewKeySet(pathToKey);
         }
         else
         {
            loadKeySetFrom($"{path}\\{keyName}.pem");
         }
      }

      public byte[] SignData(byte[] hashOfDataToSign)
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

      public bool VerifySignature(byte[] hashOfDataToSign, byte[] signature)
      {
         using (var rsa = new RSACryptoServiceProvider(KeySize))
         {
            rsa.ImportParameters(PublicKey);
            var rsaDeformatter = new RSAPKCS1SignatureDeformatter(rsa);
            rsaDeformatter.SetHashAlgorithm("SHA256");
            return rsaDeformatter.VerifySignature(hashOfDataToSign, signature);
         }
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
            string newKey = CryptoApi.RSAKeys.ExportPrivateKey(rsa);
            File.WriteAllText(keyPath, newKey);
         }
      }
   }
}