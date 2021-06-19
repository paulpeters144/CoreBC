using CoreBC.BlockModels;
using CoreBC.CryptoApi;
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
   internal class DactylKey
   {
      public int KeySize = 512;
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

      public TransactionModel SendMoneyTo(string recPubKey, decimal amount)
      {
         Input input = new Input
         {
            FromAddress = GetPubKeyString(),
            Amount = Helpers.FormatDactylDigits(amount)
         };

         Output output = new Output
         {
            ToAddress = recPubKey,
            Amount = Helpers.FormatDactylDigits(amount)
         };

         TransactionModel tx = new TransactionModel
         {
            Input = input,
            Output = output,
            Locktime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
         };

         tx = SignTransaction(tx);
         tx = CreateTransactionId(tx);
         tx.Fee = Helpers.FormatDactylDigits(Convert.ToDecimal(input.Amount) * tx.FeePercent);

         bool txVerified = VerifyTransaction(tx);
         decimal totalTradeAmount = (Convert.ToDecimal(input.Amount) + Convert.ToDecimal(tx.Fee));
         bool hasEnoughDYL = GetWalletBalance() > totalTradeAmount;

         if (txVerified && hasEnoughDYL)
            return tx;
         else  return null;
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

         sb.Append($"{tx.Input.FromAddress}{tx.Input.Amount}");
         sb.Append($"{tx.Output.ToAddress}{tx.Output.Amount}");
         sb.Append(tx.Locktime);
         sb.Append(tx.Signature);
         
         string txFullMessage = sb.ToString();
         byte[] txFullMessageInBytes = Encoding.UTF8.GetBytes(txFullMessage);
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

      private void findKey(string keyName)
      {
         var path = Program.FilePath;

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
      private decimal GetWalletBalance()
      {
         string acctPath = Program.FilePath + "\\Blockchain\\ACCTSet\\ACCTSet.json";
         string acctSet = File.ReadAllText(acctPath);
         JObject acctObj = JObject.Parse(acctSet);
         string publicKeyString = GetPubKeyString();
         decimal currentBalance = Convert.ToDecimal(acctObj[publicKeyString]);

         string mempoolPath = Program.FilePath + "\\Blockchain\\Mempool\\mempool.json";
         if (!File.Exists(mempoolPath))
            File.Create(mempoolPath).Dispose();

         string mempoolFile = File.ReadAllText(mempoolPath);

         JObject mempoolObj;
         if (String.IsNullOrEmpty(mempoolFile))
            mempoolObj = new JObject();
         else
            mempoolObj = JObject.Parse(mempoolFile);

         foreach (var tx in mempoolObj)
         {
            string txPubKey = tx.Value["Input"]["FromAddress"].ToString();
            if (String.Equals(txPubKey, publicKeyString))
               currentBalance -= Convert.ToDecimal(tx.Value["Input"]["Amount"]);
         }

         return currentBalance;
      }

      private bool VerifySignature(byte[] hashOfDataToSign, byte[] signature)
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