using Revelium.Evm.Common;
using System.Numerics;
using System.Text.Json.Serialization;

namespace Revelium.Evm.BlockScout.Models
{
    public class NextPageParams
    {
        [JsonPropertyName("block_number")]
        public long? BlockNumber { get; set; }
        [JsonPropertyName("index")]
        public long? Index { get; set; }
        [JsonPropertyName("items_count")]
        public long? ItemsCount { get; set; }
        [JsonPropertyName("address_hash")]
        public string? AddressHash { get; set; }
        [JsonPropertyName("value")]
        [JsonConverter(typeof(BigIntegerConverter))]
        public BigInteger? Value { get; set; }
        [JsonPropertyName("fee")]
        public string? Fee { get; set; }
        [JsonPropertyName("hash")]
        public string? Hash { get; set; }
        [JsonPropertyName("inserted_at")]
        public string? InsertedAt { get; set; }
    }
}
