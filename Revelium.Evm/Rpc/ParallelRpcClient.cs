using Incendium;
using Revelium.Evm.Rpc.Abstract;
using Revelium.Evm.Rpc.Models;
using Revelium.Evm.Rpc.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Revelium.Evm.Rpc;

public class ParallelRpcClient(
    IEnumerable<RpcClient> rpcClients,
    IEnumerable<RpcClient> broadcastRpcClients) : IRpcClient
{
    private readonly IEnumerable<RpcClient> _rpcClients = rpcClients;
    private readonly IEnumerable<RpcClient> _broadcastRpcClients = broadcastRpcClients;

    /// <summary>
    /// For ParallelRpcClient returns the Url of the first node from broadcastRpcClients list, if it is not empty.
    /// Otherwise returns the first node from rpcClients list
    /// </summary>
    public string Url => _broadcastRpcClients?.FirstOrDefault()?.Url ?? _rpcClients.First().Url;
    public long? ChainId => _rpcClients.First().ChainId;

    /// <summary>
    /// Executes a function in parallel across multiple RPC clients and returns the first successful result.
    /// Continues waiting for other clients if the first response is an error.
    /// </summary>
    private async Task<Result<TValue>> GetFirstSuccessAsync<TValue>(
        Func<IRpcClient, CancellationToken, Task<Result<TValue>>> requestFunc,
        IEnumerable<IRpcClient> clients,
        CancellationToken cancellationToken = default)
    {
        var clientsList = clients.ToList();
        if (clientsList.Count == 0)
            throw new InvalidOperationException("No RPC clients available");

        var tasks = clientsList
            .Select(client => requestFunc(client, cancellationToken))
            .ToList();

        var completedTasks = new HashSet<Task<Result<TValue>>>();
        Error? lastError = null;

        while (completedTasks.Count < tasks.Count)
        {
            var remainingTasks = tasks.Where(t => !completedTasks.Contains(t)).ToList();
            if (remainingTasks.Count == 0)
                break;

            var completedTask = await Task.WhenAny(remainingTasks);

            var (result, error) = await completedTask;
            completedTasks.Add(completedTask);

            if (error == null)
                return result;

            lastError = error;
        }

        // All tasks completed with errors, return the last error
        if (lastError != null)
            return lastError;

        throw new InvalidOperationException("All RPC requests failed without returning an error");
    }

    /// <summary>
    /// Executes a function in parallel across multiple RPC clients and returns the first successful result.
    /// Continues waiting for other clients if the first response is an error.
    /// </summary>
    private async Task<NullableResult<TValue>> GetFirstSuccessAsync<TValue>(
        Func<IRpcClient, CancellationToken, Task<NullableResult<TValue>>> requestFunc,
        IEnumerable<IRpcClient> clients,
        CancellationToken cancellationToken = default)
    {
        var clientsList = clients.ToList();
        if (clientsList.Count == 0)
            throw new InvalidOperationException("No RPC clients available");

        var tasks = clientsList
            .Select(client => requestFunc(client, cancellationToken))
            .ToList();

        var completedTasks = new HashSet<Task<NullableResult<TValue>>>();
        Error? lastError = null;

        while (completedTasks.Count < tasks.Count)
        {
            var remainingTasks = tasks.Where(t => !completedTasks.Contains(t)).ToList();
            if (remainingTasks.Count == 0)
                break;

            var completedTask = await Task.WhenAny(remainingTasks);

            var (result, error) = await completedTask;
            completedTasks.Add(completedTask);

            if (error == null)
                return result;

            lastError = error;
        }

        // All tasks completed with errors, return the last error
        if (lastError != null)
            return lastError;

        throw new InvalidOperationException("All RPC requests failed without returning an error");
    }

    public Task<NullableResult<TResult>> CallAsync<TResult>(
        string to,
        string? from = null,
        BigInteger? gas = null,
        BigInteger? gasPrice = null,
        BigInteger? value = null,
        string? input = null,
        BlockNumber? block = null,
        CancellationToken cancellationToken = default)
    {
        return GetFirstSuccessAsync(
            (client, ct) => client.CallAsync<TResult>(to, from, gas, gasPrice, value, input, block, ct),
            _rpcClients,
            cancellationToken);
    }

    public Task<Result<BigInteger>> EstimateGasAsync(
        string to,
        string? from = null,
        BigInteger? gas = null,
        BigInteger? gasPrice = null,
        BigInteger? maxPriorityFeePerGas = null,
        BigInteger? maxFeePerGas = null,
        BigInteger? value = null,
        string? data = null,
        BlockNumber? block = null,
        CancellationToken cancellationToken = default)
    {
        return GetFirstSuccessAsync(
            (client, ct) => client.EstimateGasAsync(
                to,
                from,
                gas,
                gasPrice,
                maxPriorityFeePerGas,
                maxFeePerGas,
                value,
                data,
                block,
                ct),
            _rpcClients,
            cancellationToken);
    }

    public Task<Result<BigInteger>> GetBalanceAsync(
        string address,
        BlockNumber? block = null,
        CancellationToken cancellationToken = default)
    {
        return GetFirstSuccessAsync(
            (client, ct) => client.GetBalanceAsync(address, block, ct),
            _rpcClients,
            cancellationToken);
    }

    public Task<Result<Block>> GetBlockByNumberAsync(
        BlockNumber? block = null,
        CancellationToken cancellationToken = default)
    {
        return GetFirstSuccessAsync(
            (client, ct) => client.GetBlockByNumberAsync(block, ct),
            _rpcClients,
            cancellationToken);
    }

    public Task<Result<BigInteger>> GetBlockNumberAsync(
        CancellationToken cancellationToken = default)
    {
        return GetFirstSuccessAsync(
            (client, ct) => client.GetBlockNumberAsync(ct),
            _rpcClients,
            cancellationToken);
    }

    public Task<Result<BigInteger>> GetGasPriceAsync(
        CancellationToken cancellationToken = default)
    {
        return GetFirstSuccessAsync(
            (client, ct) => client.GetGasPriceAsync(ct),
            _rpcClients,
            cancellationToken);
    }

    public Task<Result<LightBlock>> GetLightBlockByNumberAsync(
        BlockNumber? block = null,
        CancellationToken cancellationToken = default)
    {
        return GetFirstSuccessAsync(
            (client, ct) => client.GetLightBlockByNumberAsync(block, ct),
            _rpcClients,
            cancellationToken);
    }

    public Task<Result<List<Log>>> GetLogsAsync(
        BlockNumber? fromBlock = null,
        BlockNumber? toBlock = null,
        string? address = null,
        string[]? topics = null,
        string? blockHash = null,
        CancellationToken cancellationToken = default)
    {
        return GetFirstSuccessAsync(
            (client, ct) => client.GetLogsAsync(fromBlock, toBlock, address, topics, blockHash, ct),
            _rpcClients,
            cancellationToken);
    }

    public Task<Result<List<Log>>> GetLogsWithTopicsFilterAsync(
        BlockNumber? fromBlock = null,
        BlockNumber? toBlock = null,
        string? address = null,
        string[][]? topics = null,
        string? blockHash = null,
        CancellationToken cancellationToken = default)
    {
        return GetFirstSuccessAsync(
            (client, ct) => client.GetLogsWithTopicsFilterAsync(fromBlock, toBlock, address, topics, blockHash, ct),
            _rpcClients,
            cancellationToken);
    }

    public Task<Result<BigInteger>> GetMaxPriorityFeePerGasAsync(
        CancellationToken cancellationToken = default)
    {
        return GetFirstSuccessAsync(
            (client, ct) => client.GetMaxPriorityFeePerGasAsync(ct),
            _rpcClients,
            cancellationToken);
    }

    public Task<Result<BigInteger>> GetTransactionCountAsync(
        string address,
        BlockNumber? block = null,
        CancellationToken cancellationToken = default)
    {
        return GetFirstSuccessAsync(
            (client, ct) => client.GetTransactionCountAsync(address, block, ct),
            _rpcClients,
            cancellationToken);
    }

    public Task<NullableResult<TransactionReceipt>> GetTransactionReceiptAsync(
        string txId,
        CancellationToken cancellationToken = default)
    {
        return GetFirstSuccessAsync(
            (client, ct) => client.GetTransactionReceiptAsync(txId, ct),
            _rpcClients,
            cancellationToken);
    }

    public Task<Result<string>> SendAsync(
        string content,
        CancellationToken cancellationToken = default)
    {
        return GetFirstSuccessAsync(
            (client, ct) => client.SendAsync(content, ct),
            _rpcClients,
            cancellationToken);
    }

    public Task<NullableResult<TResult>> SendAsync<TResult>(
        string content,
        CancellationToken cancellationToken = default)
    {
        return GetFirstSuccessAsync(
            (client, ct) => client.SendAsync<TResult>(content, ct),
            _rpcClients,
            cancellationToken);
    }

    public Task<Result<NullableResult<JsonDocument>[]>> SendBatchAsync(
        IReadOnlyList<RpcRequest> requests,
        CancellationToken cancellationToken = default)
    {
        return GetFirstSuccessAsync(
            (client, ct) => client.SendBatchAsync(requests, ct),
            _rpcClients,
            cancellationToken);
    }

    public async Task<Result<string>> SendRawTransactionAsync(
        string signedTransactionData,
        CancellationToken cancellationToken = default)
    {
        // For broadcasting transactions, send to all broadcast clients in parallel
        // but return the first successful response
        var broadcastClients = _broadcastRpcClients.ToList();
        if (broadcastClients.Count > 0)
        {
            var result = await GetFirstSuccessAsync(
                (client, ct) => client.SendRawTransactionAsync(signedTransactionData, ct),
                broadcastClients,
                cancellationToken);

            // If broadcast succeeded, return it
            var (_, error) = result;
            if (error == null)
                return result;
        }

        // Fallback to regular RPC clients if no broadcast clients or all failed
        return await GetFirstSuccessAsync(
            (client, ct) => client.SendRawTransactionAsync(signedTransactionData, ct),
            _rpcClients,
            cancellationToken);
    }
}
