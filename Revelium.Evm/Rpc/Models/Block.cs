using System;
using System.Text.Json.Serialization;

namespace Revelium.Evm.Rpc.Models;

public class Block<T>
{
    [JsonPropertyName("number")]
    public string Number { get; init; } = default!;
    [JsonPropertyName("hash")]
    public string Hash { get; init; } = default!;
    [JsonPropertyName("size")]
    public string Size { get; init; } = default!;
    [JsonPropertyName("gasLimit")]
    public string GasLimit { get; init; } = default!;
    [JsonPropertyName("gasUsed")]
    public string GasUsed { get; init; } = default!;
    [JsonPropertyName("baseFeePerGas")]
    public string BaseFeePerGas { get; init; } = default!;
    [JsonPropertyName("timestamp")]
    public string TimeStamp { get; init; } = default!;
    [JsonPropertyName("transactions")]
    public T[] Transactions { get; init; } = default!;

    public long GetBlockNumber() => Convert.ToInt64(Number[2..], 16);
}

public class Block : Block<Transaction>;
public class LightBlock : Block<string>;
