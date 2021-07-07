using CoreBC.BlockModels;
using CoreBC.DataAccess;
using CoreBC.P2PLib;
using CoreBC.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoreBC
{
    class CommandListener
    {
        public P2PNetwork P2PNetwork;
        public Miner Miner;
        public Thread MinerThread;
        private DBAccess DB;
        public CommandListener()
        {
            int bufferSize = 2058;
            P2PNetwork = new P2PNetwork(bufferSize);
            DB = new DBAccess();
        }
        public void ProcessCommand(string mainCmd)
        {
            string subCmd = string.Empty;
            if (mainCmd.Contains(" "))
            {
                string[] cmdArr = mainCmd.Split(' ');
                mainCmd = cmdArr[0];
                subCmd = string.Join(" ", cmdArr.Where((c, i) => i != 0));
            }
            switch (mainCmd)
            {
                case "help": showAllCommands(); break;
                case "mine": mine(); break;
                case "clr": Console.Clear(); break;
                case "l": listenForConnections(subCmd); break;
                case "cto": connectTo(subCmd); break;
                case "sign-in": signinAs(subCmd); break;
                case "send-to": sendCurrencyTo(subCmd); break;
                case "genesis": getGBlock(); break;
                case "balance": getBalance(); break;
                case "address": getAddress(); break;
                case "set-diff": setDifficulty(subCmd); break;
                default: Console.WriteLine("Not a valid command"); break;
            }
        }

        private void setDifficulty(string subCmd)
        {
            Helpers.MiningDifficulty = subCmd;
            Console.WriteLine("Diffulty set to " + subCmd);
        }

        private void getAddress()
        {
            string pubKey = new ChainKeys(Program.UserName).GetPubKeyString();
            Console.WriteLine("Address: " + pubKey);
        }

        private void getBalance()
        {
            ChainKeys chainKeys = new ChainKeys(Program.UserName);
            string pubKey = chainKeys.GetPubKeyString();
            var bal = DB.GetWalletBalanceFor(pubKey);
            Console.WriteLine($"{Program.UserName} has {bal}");
        }

        private void getGBlock()
        {
            GenesisBlock genesisBlock = new GenesisBlock(P2PNetwork);
            genesisBlock.Generate();
        }

        private void mine()
        {
            if (Miner == null)
            {
                Console.WriteLine("Mining started...");
                Miner = new Miner(P2PNetwork);
                MinerThread = new Thread(Miner.Mining);
                MinerThread.Start();
            }
            else
            {
                Miner.IsMining = false;
                Miner = null;
                MinerThread = null;
                mine();
            }
        }

        private void sendCurrencyTo(string subCmd)
        {
            try
            {
                string address = subCmd.Split(' ')[0];
                string amount = subCmd.Split(' ')[1];
                ChainKeys chainKeys = new ChainKeys(Program.UserName);
                TransactionModel tx = chainKeys.SendMoneyTo(address, Convert.ToDecimal(amount));
                if (tx != null)
                {
                    DB.SaveToMempool(tx);
                    string json = JsonConvert.SerializeObject(tx, Formatting.None);
                    string message = P2PHelpers.PrepMessage(
                            P2PNetwork.ID, MessageHeader.NewTransaction, json
                        );
                    P2PNetwork.BroadCast(message);
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Error processing command");
            }
        }

        private void signinAs(string subCmd)
        {
            Program.UserName = subCmd;
            Console.WriteLine("Hello " + subCmd);
        }

        private void connectTo(string subCmd)
        {
            try
            {
                string ip = "127.0.0.1";
                int port = Convert.ToInt32(subCmd);
                P2PNetwork.ConnectTo(ip, port);
            }
            catch (Exception ex)
            {
                string error = $"{ex.Message}{Environment.NewLine}{ex.StackTrace}";
                Console.WriteLine("Error connecting: " + error);
            }
        }

        private void listenForConnections(string portString)
        {
            int port = Convert.ToInt32(portString);
            P2PNetwork.ListenOn(port);
        }

        private void showAllCommands()
        {
            List<string> availableCommands = new List<string>
         {
            "'clr' to clear console",
            "'l' to start listtening for connections",
            "'cto <ipaddress>' to connect to a another node",
            "'sign-in <username>' to connect to your Blockchain Wallet",
            "'balances' to see your currency balance",
            "send-to <wallet-address> amount"
         };

            foreach (var cmd in availableCommands)
                Console.WriteLine(cmd);
        }
    }
}
