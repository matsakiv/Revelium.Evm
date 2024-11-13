using System.Text.Json.Serialization;

namespace Revelium.Evm.BlockScout.Models
{
    public class GasPrices
    {
        [JsonPropertyName("average")]
        public decimal Average { get; set; }
        [JsonPropertyName("fast")]
        public decimal Fast { get; set; }
        [JsonPropertyName("slow")]
        public decimal Slow { get; set; }
    }

    public class Stats
    {
        [JsonPropertyName("gas_prices")]
        public GasPrices GasPrices { get; set; } = default!;
        [JsonPropertyName("average_block_time")]
        public decimal AverageBlockTime { get; set; }
        [JsonPropertyName("total_addresses")]
        public string TotalAddresses { get; set; } = default!;
        [JsonPropertyName("total_blocks")]
        public string TotalBlocks { get; set; } = default!;
        [JsonPropertyName("total_transactions")]
        public string TotalTransactions { get; set; } = default!;
        [JsonPropertyName("transactions_today")]
        public string TransactionsToday { get; set; } = default!;
    }
}
