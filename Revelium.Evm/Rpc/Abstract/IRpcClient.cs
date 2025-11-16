using Incendium;
using Revelium.Evm.Rpc.Models;
using Revelium.Evm.Rpc.Parameters;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Revelium.Evm.Rpc.Abstract;

public interface IRpcClient
{
    long? ChainId { get; }

    Task<NullableResult<TResult>> CallAsync<TResult>(
        string to,
        string? from = null,
        BigInteger? gas = null,
        BigInteger? gasPrice = null,
        BigInteger? value = null,
        string? input = null,
        BlockNumber? block = null,
        CancellationToken cancellationToken = default);
    Task<Result<BigInteger>> EstimateGasAsync(
        string to,
        string? from = null,
        BigInteger? gas = null,
        BigInteger? gasPrice = null,
        BigInteger? maxPriorityFeePerGas = null,
        BigInteger? maxFeePerGas = null,
        BigInteger? value = null,
        string? data = null,
        BlockNumber? block = null,
        CancellationToken cancellationToken = default);
    Task<Result<BigInteger>> GetBalanceAsync(
        string address,
        BlockNumber? block = null,
        CancellationToken cancellationToken = default);
    Task<Result<Block>> GetBlockByNumberAsync(
        BlockNumber? block = null,
        CancellationToken cancellationToken = default);
    Task<Result<BigInteger>> GetBlockNumberAsync(
        CancellationToken cancellationToken = default);
    Task<Result<BigInteger>> GetGasPriceAsync(
        CancellationToken cancellationToken = default);
    Task<Result<LightBlock>> GetLightBlockByNumberAsync(
        BlockNumber? block = null,
        CancellationToken cancellationToken = default);
    Task<Result<List<Log>>> GetLogsAsync(
        BlockNumber? fromBlock = null,
        BlockNumber? toBlock = null,
        string? address = null,
        string[]? topics = null,
        string? blockHash = null,
        CancellationToken cancellationToken = default);
    Task<Result<List<Log>>> GetLogsWithTopicsFilterAsync(
        BlockNumber? fromBlock = null,
        BlockNumber? toBlock = null,
        string? address = null,
        string[][]? topics = null,
        string? blockHash = null,
        CancellationToken cancellationToken = default);
    Task<Result<BigInteger>> GetMaxPriorityFeePerGasAsync(
        CancellationToken cancellationToken = default);
    Task<Result<BigInteger>> GetTransactionCountAsync(
        string address,
        BlockNumber? block = null,
        CancellationToken cancellationToken = default);
    Task<NullableResult<TransactionReceipt>> GetTransactionReceiptAsync(
        string txId,
        CancellationToken cancellationToken = default);
    Task<Result<string>> SendAsync(
        string content,
        CancellationToken cancellationToken = default);
    Task<NullableResult<TResult>> SendAsync<TResult>(
        string content,
        CancellationToken cancellationToken = default);
    Task<Result<NullableResult<JsonDocument>[]>> SendBatchAsync(
        IReadOnlyList<RpcRequest> requests,
        CancellationToken cancellationToken = default);
    Task<Result<string>> SendRawTransactionAsync(
        string signedTransactionData,
        CancellationToken cancellationToken = default);
}
