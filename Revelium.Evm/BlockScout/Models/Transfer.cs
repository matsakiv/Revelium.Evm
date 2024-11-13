using System.Text.Json.Serialization;

namespace Revelium.Evm.BlockScout.Models
{
    public class Transfer
    {
        [JsonPropertyName("block_hash")]
        public string BlockHash { get; set; } = default!;
        [JsonPropertyName("from")]
        public Address From { get; set; } = default!;
        [JsonPropertyName("log_index")]
        public string LogIndex { get; set; } = default!;
        [JsonPropertyName("method")]
        public string Method { get; set; } = default!;
        [JsonPropertyName("timestamp")]
        public string TimeStamp { get; set; } = default!;
        [JsonPropertyName("to")]
        public Address To { get; set; } = default!;
        [JsonPropertyName("token")]
        public Token Token { get; set; } = default!;
        [JsonPropertyName("total")]
        public Amount Total { get; set; } = default!;
        [JsonPropertyName("tx_hash")]
        public string TxHash { get; set; } = default!;
        [JsonPropertyName("type")]
        public string Type { get; set; } = default!;
    }
}
