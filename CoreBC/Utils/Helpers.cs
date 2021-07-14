using CoreBC.BlockModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace CoreBC.Utils
{
    public static class Helpers
    {
        public static bool WeHaveReceivedNewBlock = false;
        public static string MiningDifficulty = "00000";
        public static string FormatDigits(decimal amount)
        {
            decimal rounded = Math.Round(amount, 14, MidpointRounding.ToEven);
            return String.Format("{0:0.0000000000}", rounded);
        }

        public static string GetSHAStringFromBytes(byte[] txIdInBytes)
        {
            StringBuilder sb = new StringBuilder();

            foreach (byte b in txIdInBytes)
                sb.Append(b.ToString("x2"));

            return sb.ToString();
        }

        public static string GetSHAStringFromString(string text)
        {
            byte[] textToBytes = Encoding.ASCII.GetBytes(text);
            byte[] textByesShawed = SHA256.Create().ComputeHash(textToBytes);
            return GetSHAStringFromBytes(textByesShawed);
        }

        public static long GetCurrentUTC() =>
           new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();

        public static string GetDifficulty() =>
           MiningDifficulty;

        public static string GetDifficulty(BlockModel[] blocks)
        {
            int maxBlockTimeSeconds = 600;
            int adjustEvery = 7;
            List<BlockModel> blockList = blocks.OrderByDescending(b => b.Time).ToList();
            string lastDifficulty = blockList[0].Difficulty;

            if (blockList[0].Height % adjustEvery != 0 || blockList[0].Height == 0)
                return lastDifficulty;

            long endRange = blockList[0].Height;
            long startRange = blockList[adjustEvery].Height;

            List<BlockModel> listRange = blockList
                .Where(b => b.Height >= startRange && b.Height <= endRange)
                .Reverse()
                .ToList();

            List<long> blockTimeList = new List<long>();
            long lastTime = -1;
            foreach (var block in listRange)
            {
                if (lastTime == -1)
                {
                    lastTime = block.Time;
                }
                else
                {
                    long miningTime = block.Time - lastTime;
                    blockTimeList.Add(miningTime);
                    lastTime = block.Time;
                }
            }

            var averageTime = Math.Floor(Convert.ToDecimal(blockTimeList.Sum() / blockTimeList.Count));
            string result = lastDifficulty;

            if (averageTime > maxBlockTimeSeconds)
                result = lastDifficulty.Substring(0, lastDifficulty.Length - 1);
            else if (averageTime < maxBlockTimeSeconds)
                result = result + "0";

            return result;
        }

        public static decimal GetFeePercent() =>
           0.00025M;

        public static string GetBlockDir() =>
        $"{Program.FilePath}\\";

        public static string GetBlockchainFilePath() =>
           GetBlockDir() + $"DotNetBlockChain.json";

        public static string GetAcctSetFile() =>
           GetBlockDir() + "ACCTSet.json";

        public static string GetMempooFile() =>
           GetBlockDir() + "mempool.json";

        public static decimal GetMineReward(long height)
        {
            decimal baseReward = 50;
            int halving = 210000;
            while (height > halving)
            {
                baseReward *= 0.5M;
                height -= halving;
            }
            return baseReward;
        }

        public static void ReadException(Exception ex)
        {
            string error = $"{ex.Message}{Environment.NewLine}{ex.StackTrace}";
            Console.WriteLine("Error connecting: " + error);
        }
    }
}
