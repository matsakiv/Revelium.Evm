using Incendium;
using Microsoft.Extensions.Logging;
using Revelium.Evm.Crypto.Abstract;
using Revelium.Evm.Rpc;
using Revelium.Evm.Rpc.Abstract;
using Revelium.Evm.Rpc.Models;
using Revelium.Evm.Rpc.Parameters;
using Revelium.Evm.Services;
using Revelium.Evm.Transactions;
using Revelium.Evm.Transactions.Abstract;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Revelium.Evm.Common;

public static class IRpcClientExtensions
{
    public const int TX_SEND_ERROR = 1;
    public const int TX_VERIFY_ERROR = 2;
    public const int INVALID_RESPONSE = 3;
    public const int NONCE_IS_NULL = 4;

    private static readonly Lazy<JsonSerializerOptions> RpcJsonOptions = new(() =>
    {
        var options = new JsonSerializerOptions();

        options.Converters.Add(new HexBigIntegerConverter());

        return options;
    });

    /// <summary>
    /// Sends a raw signed transaction.
    /// </summary>
    /// <param name="rpc">The RPC client instance.</param>
    /// <param name="transaction">The transaction request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The transaction ID.</returns>
    public static Task<Result<string>> SendTransactionAsync(
        this IRpcClient rpc,
        TransactionRequestBase transaction,
        CancellationToken cancellationToken = default)
    {
        return rpc.SendRawTransactionAsync(
            transaction.GetRlpEncoded(),
            cancellationToken);
    }

    /// <summary>
    /// Signs and sends a transaction.
    /// </summary>
    /// <param name="rpc">The RPC client instance.</param>
    /// <param name="tx">The transaction request.</param>
    /// <param name="signer">The transaction signer.</param>
    /// <param name="nonceManager">Nonce manager (optional). Used if not null and tx.Nonce is null.</param>
    /// <param name="verifyTx">Whether to verify the signed transaction before sending.</param>
    /// <param name="logger">The logger (optional).</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The transaction ID (hash).</returns>
    public static Task<Result<string>> SignAndSendTransactionAsync(
        this IRpcClient rpc,
        TransactionRequestBase tx,
        ISigner signer,
        NonceManager? nonceManager = null,
        bool verifyTx = true,
        ILogger? logger = null,
        CancellationToken ct = default)
    {
        return FillSignAndSendAsync(
            rpc, tx, signer,
            sendFunc: signedTx => rpc.SendTransactionAsync(signedTx, ct),
            nonceManager, verifyTx, logger, ct);
    }

    /// <summary>
    /// Signs and sends a transaction, also retrieving the current block in a single batch RPC request.
    /// This allows precise timing measurement of which block was current at the moment of sending.
    /// </summary>
    /// <param name="rpc">The RPC client instance.</param>
    /// <param name="tx">The transaction request.</param>
    /// <param name="signer">The transaction signer.</param>
    /// <param name="nonceManager">Nonce manager (optional). Used if not null and tx.Nonce is null.</param>
    /// <param name="verifyTx">Whether to verify the signed transaction before sending.</param>
    /// <param name="logger">The logger (optional).</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A tuple containing the transaction ID (hash) and the block at the time of sending.</returns>
    public static Task<Result<(string TxId, LightBlock Block)>> SignAndSendTransactionWithBlockAsync(
        this IRpcClient rpc,
        TransactionRequestBase tx,
        ISigner signer,
        NonceManager? nonceManager = null,
        bool verifyTx = true,
        ILogger? logger = null,
        CancellationToken ct = default)
    {
        return FillSignAndSendAsync(
            rpc, tx, signer,
            sendFunc: async signedTx =>
            {
                var sendReq = RpcClient.CreateSendRawTransactionRequest(signedTx.GetRlpEncoded());
                var blockReq = RpcClient.CreateBlockByNumberRequest(BlockNumber.Latest, includeTransactions: false);

                var (batchResult, batchError) = await rpc.SendBatchAsync<string, LightBlock>(sendReq, blockReq, ct);

                if (batchError != null)
                    return batchError;

                var (sendResult, blockResult) = batchResult;

                var (txId, txError) = sendResult;

                if (txError != null)
                    return new Error(TX_SEND_ERROR, "Transaction sending error", txError);

                if (txId == null)
                    return new Error(TX_SEND_ERROR, "Transaction ID is null");

                var (block, blockError) = blockResult;

                if (blockError != null)
                    return new Error(TX_SEND_ERROR, "Failed to get block at send time", blockError);

                if (block == null)
                    return new Error(TX_SEND_ERROR, "Block is null");

                return new Result<(string, LightBlock)>((txId, block));
            },
            nonceManager, verifyTx, logger, ct);
    }

    private static async Task<Result<T>> FillSignAndSendAsync<T>(
        IRpcClient rpc,
        TransactionRequestBase tx,
        ISigner signer,
        Func<TransactionRequestBase, Task<Result<T>>> sendFunc,
        NonceManager? nonceManager = null,
        bool verifyTx = true,
        ILogger? logger = null,
        CancellationToken ct = default)
    {
        var useNonceManager = nonceManager != null && tx.Nonce == null;

        using var nonceLock = useNonceManager
            ? await nonceManager!.LockAsync(tx.From, ct)
            : null;

        if (useNonceManager)
        {
            var (nonce, nonceError) = await nonceLock!.GetNonceAsync(ct);

            if (nonceError != null)
                return nonceError;

            tx.Nonce = nonce;

            logger?.LogDebug("Transaction nonce from NonceManager is {@nonce}", tx.Nonce.ToString());
        }
        else
        {
            if (tx.Nonce == null)
                return new Error(NONCE_IS_NULL, "Nonce is null");

            logger?.LogDebug("Transaction nonce is {@nonce}", tx.Nonce.ToString());
        }

        if (tx.EstimateGas)
        {
            var (estimatedGas, estimateGasError) = tx switch
            {
                Transaction1559Request eip1559Tx => await rpc.EstimateGasAsync(eip1559Tx),
                TransactionLegacyRequest legacyTx => await rpc.EstimateGasAsync(legacyTx),
                _ => throw new NotImplementedException(),
            };

            if (estimateGasError != null)
            {
                nonceLock?.Reset(tx.Nonce!.Value);
                return estimateGasError;
            }

            tx.GasLimit = estimatedGas;

            if (tx.EstimateGasReserveInPercent != null && tx.EstimateGasReserveInPercent >= 0)
                tx.GasLimit += tx.GasLimit * tx.EstimateGasReserveInPercent.Value / 100;
        }

        signer.Sign(tx);

        if (verifyTx && !tx.Verify())
        {
            nonceLock?.Reset(tx.Nonce!.Value);
            return new Error(TX_VERIFY_ERROR, "Can't verify transaction");
        }

        var (result, sendError) = await sendFunc(tx);

        if (sendError != null)
        {
            nonceLock?.Reset(tx.Nonce!.Value);
            return sendError;
        }

        return result;
    }

    /// <summary>
    /// Estimates the gas required for a legacy transaction.
    /// </summary>
    /// <param name="rpc">The RPC client instance.</param>
    /// <param name="tx">The transaction request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public static Task<Result<BigInteger>> EstimateGasAsync(
        this IRpcClient rpc,
        TransactionLegacyRequest tx,
        CancellationToken cancellationToken = default)
    {
        return rpc.EstimateGasAsync(
            to: tx.To,
            from: tx.From,
            gasPrice: tx.GasPrice,
            value: tx.Value,
            data: tx.Data,
            block: BlockNumber.Latest,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Estimates the gas required for a transaction with EIP-1559 parameters.
    /// </summary>
    /// <param name="rpc">The RPC client instance.</param>
    /// <param name="tx">The transaction request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public static Task<Result<BigInteger>> EstimateGasAsync(
        this IRpcClient rpc,
        Transaction1559Request tx,
        CancellationToken cancellationToken = default)
    {
        return rpc.EstimateGasAsync(
            to: tx.To,
            from: tx.From,
            maxPriorityFeePerGas: tx.MaxPriorityFeePerGas,
            maxFeePerGas: tx.MaxFeePerGas,
            value: tx.Value,
            data: tx.Data,
            block: BlockNumber.Latest,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Sends multiple RPC requests in a single batch and returns strongly-typed results.
    /// </summary>
    /// <typeparam name="T">The type of results to return.</typeparam>
    /// <param name="rpc">The RPC client instance.</param>
    /// <param name="requests">The collection of requests to send.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Array of nullable results, maintaining the order of requests.</returns>
    public static async Task<Result<NullableResult<T>[]>> SendBatchAsync<T>(
        this IRpcClient rpc,
        IReadOnlyList<RpcRequest> requests,
        CancellationToken cancellationToken = default)
    {
        var (responses, error) = await rpc.SendBatchAsync(requests, cancellationToken);

        if (error != null)
            return error;

        if (responses.Length != requests.Count)
            return new Error(INVALID_RESPONSE, "Invalid response count");

        var results = new NullableResult<T>[responses.Length];

        for (int i = 0; i < responses.Length; i++)
        {
            results[i] = responses[i].Error != null
                ? NullableResult<T>.Failure(responses[i].Error!)
                : NullableResult<T>.Success(responses[i].Value!.RootElement.Deserialize<T>());
        }

        return results;
    }

    /// <summary>
    /// Sends multiple RPC requests in a single batch and returns strongly-typed results.
    /// </summary>
    /// <typeparam name="T1">The type of the first result.</typeparam>
    /// <typeparam name="T2">The type of the second result.</typeparam>
    /// <param name="rpc">The RPC client instance.</param>
    /// <param name="request1">The first request.</param>
    /// <param name="request2">The second request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A tuple of nullable results.</returns>
    public static async Task<Result<(NullableResult<T1>, NullableResult<T2>)>> SendBatchAsync<T1, T2>(
        this IRpcClient rpc,
        RpcRequest request1,
        RpcRequest request2,
        CancellationToken cancellationToken = default)
    {
        var (responses, error) = await rpc.SendBatchAsync(
            [request1, request2],
            cancellationToken);

        if (error != null)
            return error;

        var options = RpcJsonOptions.Value;

        return (
            responses[0].Map(r => r!.RootElement.Deserialize<T1>(options)),
            responses[1].Map(r => r!.RootElement.Deserialize<T2>(options))
        );
    }

    /// <summary>
    /// Sends multiple RPC requests in a single batch and returns strongly-typed results.
    /// </summary>
    /// <typeparam name="T1">The type of the first result.</typeparam>
    /// <typeparam name="T2">The type of the second result.</typeparam>
    /// <typeparam name="T3">The type of the third result.</typeparam>
    /// <param name="rpc">The RPC client instance.</param>
    /// <param name="request1">The first request.</param>
    /// <param name="request2">The second request.</param>
    /// <param name="request3">The third request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public static async Task<Result<(NullableResult<T1>, NullableResult<T2>, NullableResult<T3>)>> SendBatchAsync<T1, T2, T3>(
        this IRpcClient rpc,
        RpcRequest request1,
        RpcRequest request2,
        RpcRequest request3,
        CancellationToken cancellationToken = default)
    {
        var (responses, error) = await rpc.SendBatchAsync(
            [request1, request2, request3],
            cancellationToken);

        if (error != null)
            return error;

        var options = RpcJsonOptions.Value;

        return (
            responses[0].Map(r => r!.RootElement.Deserialize<T1>(options)),
            responses[1].Map(r => r!.RootElement.Deserialize<T2>(options)),
            responses[2].Map(r => r!.RootElement.Deserialize<T3>(options))
        );
    }
}
