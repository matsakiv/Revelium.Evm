using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Revelium.Evm.BlockScout.Models;

public class Log
{
    [JsonPropertyName("address")]
    public Address Address { get; set; } = default!;
    [JsonPropertyName("block_hash")]
    public string BlockHash { get; set; } = default!;
    [JsonPropertyName("block_number")]
    public long BlockNumber { get; set; }
    [JsonPropertyName("data")]
    public string Data { get; set; } = default!;
    [JsonPropertyName("index")]
    public int Index { get; set; }
    [JsonPropertyName("smart_contract")]
    public Address SmartContract { get; set; } = default!;
    [JsonPropertyName("topics")]
    public List<string> Topics { get; set; } = default!;
    [JsonPropertyName("tx_hash")]
    public string TxHash { get; set; } = default!;
    [JsonPropertyName("decoded")]
    public Decoded Decoded { get; set; } = default!;
}
