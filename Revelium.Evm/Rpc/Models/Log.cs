using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Revelium.Evm.Rpc.Models;

public class Log
{
    [JsonPropertyName("logIndex")]
    public string LogIndex { get; init; } = default!;
    [JsonPropertyName("blockNumber")]
    public string BlockNumber { get; init; } = default!;
    [JsonPropertyName("blockHash")]
    public string BlockHash { get; init; } = default!;
    [JsonPropertyName("transactionHash")]
    public string TransactionHash { get; init; } = default!;
    [JsonPropertyName("transactionIndex")]
    public string TransactionIndex { get; init; } = default!;
    [JsonPropertyName("address")]
    public string Address { get; init; } = default!;
    [JsonPropertyName("data")]
    public string Data { get; init; } = default!;
    [JsonPropertyName("topics")]
    public List<string> Topics { get; init; } = default!;
    [JsonPropertyName("removed")]
    public bool? Removed { get; init; }

    public long GetBlockNumber() => Convert.ToInt64(BlockNumber[2..], 16);

    public FilterLog ToFilterLog() => new()
    {
        Address = Address,
        Data = Data,
        BlockHash = BlockHash,
        TransactionHash = TransactionHash,
        BlockNumber = new HexBigInteger(BlockNumber),
        LogIndex = new HexBigInteger(LogIndex),
        Removed = Removed != null && Removed.Value,
        TransactionIndex = new HexBigInteger(TransactionIndex),
        Topics = [.. Topics]
    };
}
