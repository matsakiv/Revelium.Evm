using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Revelium.Evm.BlockScout.Models
{
    public class Fee
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = default!;
        [JsonPropertyName("value")]
        public string Value { get; set; } = default!;
    }

    public class Transaction
    {
        [JsonPropertyName("timestamp")]
        public string TimeStamp { get; set; } = default!;
        [JsonPropertyName("fee")]
        public Fee Fee { get; set; } = default!;
        [JsonPropertyName("gas_limit")]
        public string GasLimit { get; set; } = default!;
        [JsonPropertyName("block")]
        public long Block { get; set; }
        [JsonPropertyName("status")]
        public string Status { get; set; } = default!;
        [JsonPropertyName("method")]
        public string Method { get; set; } = default!;
        [JsonPropertyName("confirmations")]
        public long Confirmations { get; set; }
        [JsonPropertyName("to")]
        public Address To { get; set; } = default!;
        [JsonPropertyName("result")]
        public string Result { get; set; } = default!;
        [JsonPropertyName("hash")]
        public string Hash { get; set; } = default!;
        [JsonPropertyName("gas_price")]
        public string GasPrice { get; set; } = default!;
        [JsonPropertyName("from")]
        public Address From { get; set; } = default!;
        [JsonPropertyName("token_transfers")]
        public List<Transfer> TokenTransfers { get; set; } = default!;
        [JsonPropertyName("tx_types")]
        public List<string> TxTypes { get; set; } = default!;
        [JsonPropertyName("gas_used")]
        public string GasUsed { get; set; } = default!;
        [JsonPropertyName("position")]
        public long Position { get; set; }
        [JsonPropertyName("nonce")]
        public long Nonce { get; set; }
        [JsonPropertyName("decoded_input")]
        public Decoded DecodedInput { get; set; } = default!;
        [JsonPropertyName("raw_input")]
        public string RawInput { get; set; } = default!;
        [JsonPropertyName("value")]
        public string Value { get; set; } = default!;
    }
}
