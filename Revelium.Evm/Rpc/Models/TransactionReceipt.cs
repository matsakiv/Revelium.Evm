using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Revelium.Evm.Rpc.Models;

public class TransactionReceipt
{
    [JsonPropertyName("transactionHash")]
    public string TransactionHash { get; init; } = default!;
    [JsonPropertyName("transactionIndex")]
    public string TransactionIndex { get; init; } = default!;
    [JsonPropertyName("blockHash")]
    public string BlockHash { get; init; } = default!;
    [JsonPropertyName("blockNumber")]
    public string BlockNumber { get; init; } = default!;
    [JsonPropertyName("from")]
    public string From { get; init; } = default!;
    [JsonPropertyName("to")]
    public string To { get; init; } = default!;
    [JsonPropertyName("cumulativeGasUsed")]
    public string CumulativeGasUsed { get; init; } = default!;
    [JsonPropertyName("effectiveGasPrice")]
    public string EffectiveGasPrice { get; init; } = default!;
    [JsonPropertyName("gasUsed")]
    public string GasUsed { get; init; } = default!;
    [JsonPropertyName("contractAddress")]
    public string ContractAddress { get; init; } = default!;
    [JsonPropertyName("logs")]
    public List<Log> Logs { get; init; } = default!;
    [JsonPropertyName("logsBloom")]
    public string LogsBloom { get; init; } = default!;
    [JsonPropertyName("type")]
    public string Type { get; init; } = default!;
    [JsonPropertyName("root")]
    public string Root { get; init; } = default!;
    [JsonPropertyName("status")]
    public string Status { get; init; } = default!;
}
