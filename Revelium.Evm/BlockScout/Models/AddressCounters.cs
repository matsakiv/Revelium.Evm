using System.Text.Json.Serialization;

namespace Revelium.Evm.BlockScout.Models;

public class AddressCounters
{
    [JsonPropertyName("gas_usage_count")]
    public string GasUsageCount { get; set; } = default!;
    [JsonPropertyName("token_transfers_count")]
    public string TokenTransfersCount { get; set; } = default!;
    [JsonPropertyName("transactions_count")]
    public string TransactionsCount { get; set; } = default!;
    [JsonPropertyName("validations_count")]
    public string ValidationsCount { get; set; } = default!;
}
