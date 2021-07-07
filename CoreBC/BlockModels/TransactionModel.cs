namespace CoreBC.BlockModels
{
    public class TransactionModel
    {
        public string TransactionId { get; set; }
        public string Signature { get; set; }
        public long Locktime { get; set; }
        public Input Input { get; set; }
        public Output Output { get; set; }
        public string Fee { get; set; }
    }
}
