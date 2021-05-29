using System;
using Microsoft.Extensions.Configuration;

namespace CoreBC
{
   class Program
   {
      public static IConfiguration Configuration { get; set; }
      static void Main(string[] args)
      {
         //startup();
         //CommandListener commandListener = new CommandListener();
         //Console.WriteLine("Type 'help' to see availible commands.");
         //while (true)
         //{
         //   string cmd = Console.ReadLine();
         //   commandListener.ProcessCommand(cmd);
         //}

         Miner miner = new Miner();
         miner.ProofOfWork();
      }

      private static void startup()
      {
         Configuration = new ConfigurationBuilder()
                         .AddJsonFile("appsettings.json", true, true)
                         .Build();
         //var playerSection = Configuration.GetSection("Player:Name").Value;
      }
   }
}
