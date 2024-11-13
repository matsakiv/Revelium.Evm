using System.Text.Json.Serialization;

namespace Revelium.Evm.Rpc.Models
{
    public class Transaction
    {
        [JsonPropertyName("blockHash")]
        public string BlockHash { get; init; } = default!;
        [JsonPropertyName("blockNumber")]
        public string BlockNumber { get; init; } = default!;
        [JsonPropertyName("from")]
        public string From { get; init; } = default!;
        [JsonPropertyName("gas")]
        public string Gas { get; init; } = default!;
        [JsonPropertyName("gasPrice")]
        public string GasPrice { get; init; } = default!;
        [JsonPropertyName("hash")]
        public string Hash { get; init; } = default!;
        [JsonPropertyName("input")]
        public string Input { get; init; } = default!;
        [JsonPropertyName("nonce")]
        public string Nonce { get; init; } = default!;
        [JsonPropertyName("to")]
        public string To { get; init; } = default!;
        [JsonPropertyName("transactionIndex")]
        public string TransactionIndex { get; init; } = default!;
        [JsonPropertyName("value")]
        public string Value { get; init; } = default!;
        [JsonPropertyName("v")]
        public string V { get; init; } = default!;
        [JsonPropertyName("r")]
        public string R { get; init; } = default!;
        [JsonPropertyName("s")]
        public string S { get; init; } = default!;
    }
}
