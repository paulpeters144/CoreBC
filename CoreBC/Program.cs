using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using CoreBC.BlockModels;
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
      public static string UserName = "paulp";
      static void Main(string[] args)
      {
         startup();
         //PlayScenario playScenario = new PlayScenario();
         //playScenario.MineMempool();






         PlayScenario playScenario = new PlayScenario();
         bool result = playScenario.CheckBlock(1);
         if (result)
         {
            Console.WriteLine("Block is good");
         }
         else
         {
            Console.WriteLine("Block is bad");
         }








         CommandListener commandListener = new CommandListener();
         Console.WriteLine("Type 'help' to see availible commands.");
         while (true)
         {
            string cmd = Console.ReadLine();
            commandListener.ProcessCommand(cmd);
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
