using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using CoreBC.BlockModels;
using CoreBC.DataAccess;
using CoreBC.Utils;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CoreBC
{
   class Program
   {
      public static IConfiguration Configuration { get; set; }
      public static string FilePath { get; set; }
      public static string UserName;

      static void Main(string[] args)
      {
         startup();
         //IDataAccess DB = new BlockChainFiles();
         //GenesisBlock genesisBlock = new GenesisBlock();
         //genesisBlock.Generate();
         //DB.UpdateAccountBalances();
         //for (int i = 0; i < 10; i++)
         //{
         //   PlayScenario playScenario = new PlayScenario();
         //   playScenario.CreateTx();
         //   playScenario.MineMempool();
         //   DB.UpdateAccountBalances();
         //}
         CommandListener cmdListener = new CommandListener();
         cmdListener.ProcessCommand("help");
         cmdListener.ProcessCommand("sign-in paulp");
         while (true)
         {
            string mainCmd = Console.ReadLine();
            cmdListener.ProcessCommand(mainCmd);
         }
      }

      private static void startup()
      {
         Configuration = new ConfigurationBuilder()
                         .AddJsonFile("appsettings.json", true, true)
                         .Build();
         var codeBase = System.Reflection.Assembly.GetExecutingAssembly().Location;
         var pathList = codeBase.Split("\\").ToList();
         pathList.RemoveAt(pathList.Count - 1);
         FilePath = String.Join("\\", pathList.ToArray());
      }
   }
}
