using Incendium;
using Microsoft.Extensions.Logging;
using Revelium.Evm.Crypto.Abstract;
using Revelium.Evm.Rpc;
using Revelium.Evm.Rpc.Parameters;
using Revelium.Evm.Transactions;
using Revelium.Evm.Transactions.Abstract;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Revelium.Evm.Common
{
    public static class RpcClientExtensions
    {
        public const int TX_SEND_ERROR = 1;
        public const int TX_VERIFY_ERROR = 2;
        public const int INVALID_RESPONSE = 2;

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
            this RpcClient rpc,
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
        /// <param name="estimateGas">Whether to estimate the gas required for the transaction.</param>
        /// <param name="estimateGasReserveInPercent">The percentage of gas to reserve for the transaction.</param>
        /// <param name="networkId">The network ID.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The transaction ID.</returns>
        public static async Task<Result<string>> SignAndSendTransactionAsync(
            this RpcClient rpc,
            TransactionRequestBase tx,
            ISigner signer,
            bool estimateGas = true,
            uint? estimateGasReserveInPercent = 0,
            string? networkId = null,
            ILogger? logger = null,
            CancellationToken cancellationToken = default)
        {
            var nonceManager = NonceManager.GetOrAddInstance(tx.From, networkId);

            var (nonceLock, nonceError) = await nonceManager.GetNonceAsync(
                rpc,
                pending: true,
                logger: logger,
                cancellationToken);

            if (nonceError != null)
                return nonceError;

            try
            {
                tx.Nonce = nonceLock.Nonce;

                logger?.LogDebug("Transaction nonce is {@nonce}", tx.Nonce.ToString());

                if (estimateGas)
                {
                    var (estimatedGas, estimateGasError) = tx switch
                    {
                        Transaction1559Request eip1559Tx => await rpc.EstimateGasAsync(eip1559Tx),
                        TransactionLegacyRequest legacyTx => await rpc.EstimateGasAsync(legacyTx),
                        _ => throw new NotImplementedException(),
                    };

                    if (estimateGasError != null)
                    {
                        nonceManager.Reset(tx.Nonce, logger);
                        return estimateGasError;
                    }

                    tx.GasLimit = estimatedGas;

                    if (estimateGasReserveInPercent != null && estimateGasReserveInPercent >= 0)
                        tx.GasLimit += tx.GasLimit / 100 * estimateGasReserveInPercent.Value;
                }

                signer.Sign(tx);

                if (!tx.Verify())
                {
                    nonceManager.Reset(tx.Nonce, logger);
                    return new Error(TX_VERIFY_ERROR, "Can't verify transaction");
                }

                var (txId, txSendError) = await rpc.SendTransactionAsync(tx, cancellationToken);

                if (txSendError != null)
                {
                    nonceManager.Reset(tx.Nonce, logger);
                    return new Error(TX_SEND_ERROR, "Transaction sending error", txSendError);
                }

                return txId;
            }
            finally
            {
                nonceLock?.Dispose();
            }
        }

        /// <summary>
        /// Estimates the gas required for a legacy transaction.
        /// </summary>
        /// <param name="rpc">The RPC client instance.</param>
        /// <param name="tx">The transaction request.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        public static Task<Result<BigInteger>> EstimateGasAsync(
            this RpcClient rpc,
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
            this RpcClient rpc,
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
            this RpcClient rpc,
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
            this RpcClient rpc,
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
            this RpcClient rpc,
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
}
