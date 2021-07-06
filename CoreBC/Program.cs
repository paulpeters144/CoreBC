﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
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
         CreateMainFiles();
      }
      
      public static void CreateMainFiles()
      {
         Console.WriteLine("write folder suffix");
         string s = Console.ReadLine();
         FilePath = Directory.GetCurrentDirectory() + $"\\BlockchainFiles{s}";
         string[] arr = FilePath.Split('\\');
         string node = arr[arr.Length - 1];
         Console.WriteLine("Node folder: " + node);

         string mainDir = Helpers.GetBlockDir();
         if (!Directory.Exists(mainDir))
            Directory.CreateDirectory(mainDir);

         string blockchainPath = Helpers.GetBlockchainFilePath();
         if (!File.Exists(blockchainPath))
            File.Create(blockchainPath).Dispose();

         string acctSetPath = Helpers.GetAcctSetFile();
         if (!File.Exists(acctSetPath))
            File.Create(acctSetPath).Dispose();

         string mempoolPath = Helpers.GetMempooFile();
         if (!File.Exists(mempoolPath))
            File.Create(mempoolPath).Dispose();
      }
   }
}
