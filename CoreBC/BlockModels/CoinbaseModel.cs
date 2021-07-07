namespace CoreBC.BlockModels
{
    public class CoinbaseModel
    {
        public string TransactionId { get; set; }
        public string BlockHash { get; set; }
        public Output Output { get; set; }
        public string FeeReward { get; set; }
    }
}
